using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Windows;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Modularity;

namespace Len.Abp.Wpf;

/// <summary>
/// WPF UI 线程宿主：在独立 STA 线程上创建并运行 <see cref="Application"/>。
/// </summary>
/// <remarks>
/// 异常处理交给 <see cref="IWpfExceptionHandler"/>，退出时的区域拆除交给
/// <see cref="RegionTeardownService"/>，本类只负责线程与生命周期的编排。
/// </remarks>
internal class WpfThreadService(
    IAbpLazyServiceProvider abpLazyServiceProvider,
    WpfContext wpfContext,
    IHostApplicationLifetime hostApplicationLifetime,
    IWpfExceptionHandler exceptionHandler,
    RegionTeardownService regionTeardownService
) : ISingletonDependency
{
    private readonly IAbpLazyServiceProvider _abpLazyServiceProvider = abpLazyServiceProvider;
    private readonly IHostApplicationLifetime _hostApplicationLifetime = hostApplicationLifetime;
    private readonly WpfContext _uiContext = wpfContext;
    private readonly IWpfExceptionHandler _exceptionHandler = exceptionHandler;
    private readonly RegionTeardownService _regionTeardownService = regionTeardownService;

    /// <summary>
    /// 在独立的 STA 线程上创建并启动 WPF 应用。
    /// </summary>
    /// <returns>WPF 应用创建完成后完成的任务。</returns>
    /// <remarks>
    /// 同步准备（模块资源合并、Shell 解析）必须在宣告成功之前完成：
    /// 这些步骤失败意味着界面永远不会出现，调用方必须能感知。
    /// 只有进入消息循环（<see cref="System.Windows.Application.Run()"/>）是阻塞的，放在成功信号之后。
    /// </remarks>
    public Task StartAsync()
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                PreStartUiThread();
                _uiContext.IsRunning = true;

                // 同步准备先于成功信号：失败时调用方拿到的任务会带异常。
                PrepareUiThread();

                // 在 Application 创建完成且准备就绪后再发出信号，
                // 使调用方可以安全依赖 WpfContext.WpfApplication 与已显示的 Shell。
                tcs.TrySetResult();

                RunUiThread();
            }
            catch (Exception exception)
            {
                // 线程体抛出的异常无法传播给调用方：必须通过任务传递，
                // 否则 StartAsync 会永久挂起，同时异常在后台线程上终止进程。
                _uiContext.IsRunning = false;

                // 任务可能已完成（异常来自进入消息循环之后的阶段），此时 TrySetException 返回 false。
                // 必须记录日志并停止宿主，否则启动失败完全静默——界面不出现，日志里也没有线索。
                if (!tcs.TrySetException(exception))
                {
                    HandlePostStartupFailure(exception);
                }
            }
        })
        {
            IsBackground = true
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        return tcs.Task;
    }

    /// <summary>
    /// 处理 WPF 应用退出：拆除区域，并按配置联动停止宿主。
    /// </summary>
    /// <remarks>
    /// 不依赖 <see cref="Application"/> 实例，因此可以直接在测试中调用。
    /// </remarks>
    internal void OnApplicationExit()
    {
        _uiContext.IsRunning = false;

        // 先拆除区域再停宿主：视图的 Dispose 可能依赖容器仍然可用。
        _regionTeardownService.TearDownAll();

        if (!_uiContext.IsLifetimeLinked)
        {
            return;
        }

        if (
            _hostApplicationLifetime.ApplicationStopped.IsCancellationRequested
            || _hostApplicationLifetime.ApplicationStopping.IsCancellationRequested
        )
        {
            return;
        }

        _hostApplicationLifetime.StopApplication();
    }

    private void PreStartUiThread()
    {
        var wpfApplication = new Application() { ShutdownMode = _uiContext.ShutdownMode };
        wpfApplication.Dispatcher.Invoke(() =>
        {
            wpfApplication.Exit += (s, e) => OnApplicationExit();
            wpfApplication.DispatcherUnhandledException += (s, e) =>
                e.Handled = _exceptionHandler.HandleDispatcherException(e.Exception);
        });

        _uiContext.WpfApplication = wpfApplication;
    }

    /// <summary>
    /// 同步准备 UI 线程：合并模块资源、解析 Shell、挂接启动与异常处理。
    /// </summary>
    /// <remarks>
    /// 这些步骤失败意味着界面永远不会出现，必须在宣告启动成功之前完成，
    /// 让异常通过 <see cref="StartAsync"/> 返回的任务传播给调用方。
    /// </remarks>
    private void PrepareUiThread()
    {
        _uiContext.WpfApplication.Dispatcher.Invoke(() =>
        {
            var app = _abpLazyServiceProvider.LazyGetRequiredService<IAbpApplication>();

            foreach (var module in app.Modules)
            {
                if (module.Instance is AbpWpfModule abpModule)
                {
                    abpModule.ConfigureResources(_uiContext.WpfApplication.Resources.MergedDictionaries);
                }
            }

            var shellWindows = GetShellWindows();

            _uiContext.WpfApplication.Startup += (s, e) =>
            {
                foreach (var window in shellWindows)
                {
                    window.Show();
                }
            };

            AttachTaskExceptionHandler();
        });
    }

    /// <summary>
    /// 解析所有 Shell 窗口。
    /// </summary>
    /// <returns>可显示的窗口列表。</returns>
    /// <remarks>
    /// <para>
    /// <b>不能用 <c>Cast&lt;Window&gt;()</c>。</b><see cref="IShell"/> 是标记接口，
    /// 实现者不一定是 <see cref="Window"/>；遇到非窗口实现时 <c>Cast</c> 会抛
    /// <see cref="InvalidCastException"/>，界面永远不出现，异常还发生在 UI 线程准备阶段。
    /// 这里与 <see cref="ShellActivator"/> 保持一致：跳过并记录警告，让配置错误可见但不致命。
    /// </para>
    /// <para>
    /// 声明为 <c>internal</c> 而不是私有：测试需要直接验证跳过与警告行为，
    /// 而走 <see cref="StartAsync"/> 的完整路径会创建 <see cref="Application"/>，
    /// 一个 AppDomain 只能创建一个。用 <c>InternalsVisibleTo</c> 暴露比反射调用可靠——
    /// 方法改名时编译期就会失败。
    /// </para>
    /// </remarks>
    internal List<Window> GetShellWindows()
    {
        var windows = new List<Window>();

        foreach (var shell in _abpLazyServiceProvider.GetServices<IShell>())
        {
            if (shell is Window window)
            {
                windows.Add(window);
                continue;
            }

            _abpLazyServiceProvider
                .LazyGetService<ILogger<WpfThreadService>>()
                ?.LogWarning(
                    "类型 {ShellType} 标记了 IShell 但不是 Window，已跳过。Shell 必须是 Window 才能显示。",
                    shell?.GetType().FullName ?? "null"
                );
        }

        return windows;
    }

    /// <summary>
    /// 进入 WPF 消息循环；阻塞直到应用退出。
    /// </summary>
    private void RunUiThread() => _uiContext.WpfApplication.Dispatcher.Invoke(_uiContext.WpfApplication.Run);

    /// <summary>
    /// 处理"启动成功信号已发出之后"才发生的异常。
    /// </summary>
    /// <param name="exception">导致 UI 线程终止的异常。</param>
    /// <remarks>
    /// <para>
    /// 此时调用方已经拿到成功的任务，无法再通过任务传播异常。
    /// 必须记录日志并停止宿主：否则界面不出现，日志里也没有任何线索。
    /// </para>
    /// <para>
    /// 声明为 <c>internal</c> 而不是私有：测试需要直接调用它验证日志与停止行为，
    /// 而走 <see cref="StartAsync"/> 的失败路径会创建 <see cref="Application"/>，
    /// 一个 AppDomain 只能创建一个。用 <c>InternalsVisibleTo</c> 暴露比反射调用可靠——
    /// 方法改名时编译期就会失败。
    /// </para>
    /// </remarks>
    internal void HandlePostStartupFailure(Exception exception)
    {
        _abpLazyServiceProvider
            .LazyGetService<ILogger<WpfThreadService>>()
            ?.LogError(exception, "WPF UI 线程在启动完成后异常终止，正在停止宿主。");

        if (_hostApplicationLifetime.ApplicationStopping.IsCancellationRequested)
        {
            return;
        }

        _hostApplicationLifetime.StopApplication();
    }

    /// <summary>
    /// 挂接后台任务的未观察异常事件。
    /// </summary>
    /// <remarks>
    /// 订阅与 <c>SetObserved</c> 留在框架侧：未观察异常默认只在 GC 时静默丢弃，
    /// 既不崩溃也不留线索，这个兜底不该依赖用户实现记得调用。
    /// <see cref="TaskScheduler.UnobservedTaskException"/> 是进程级静态事件，
    /// 而一个 AppDomain 只能创建一个 <see cref="Application"/>，因此只会挂接一次。
    /// </remarks>
    private void AttachTaskExceptionHandler() =>
        TaskScheduler.UnobservedTaskException += (s, e) => HandleUnobservedTaskException(e);

    /// <summary>
    /// 处理后台任务的未观察异常：记录并标记为已观察。
    /// </summary>
    /// <param name="e">未观察异常的事件参数。</param>
    /// <remarks>
    /// 抽成独立方法而不是内联在 lambda 里：测试需要验证"记录并标记已观察"这段逻辑，
    /// 而 <see cref="TaskScheduler.UnobservedTaskException"/> 没有公开的触发方式，
    /// 只能靠 GC 回收未观察的 Task，时机不可控。直接调用本方法比读取静态事件的
    /// 私有委托字段可靠——后者依赖 BCL 内部实现，且需要 <c>DynamicInvoke</c>。
    /// </remarks>
    internal void HandleUnobservedTaskException(UnobservedTaskExceptionEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);

        _exceptionHandler.HandleUnobservedTaskException(e.Exception);
        e.SetObserved();
    }
}
