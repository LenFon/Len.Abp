using Len.Abp.Wpf;
using Len.Abp.Wpf.Navigation;
using Len.Abp.Wpf.Mvvm;
using Len.Abp.Wpf.Navigation.Regions;
using Len.Abp.Wpf.Navigation.Regions.Behaviors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Windows;
using System.Windows.Threading;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Modularity;
using Xunit;

namespace Len.Abp.Wpf.Tests;

/// <summary>
/// 覆盖 <see cref="WpfThreadService"/> 的启动与退出流程。
/// </summary>
/// <remarks>
/// 一个 AppDomain 只能创建一个 <see cref="Application"/>，因此所有断言合并在同一个测试里，
/// 依次验证启动、资源合并、Shell 显示、调度器转发与退出联动。
/// </remarks>
[Collection(GlobalServiceProviderCollection.Name)]
public class WpfThreadServiceTests
{
    /// <summary>
    /// 启动后创建应用、合并模块资源、显示 Shell，并在应用退出时拆除区域、停止宿主。
    /// </summary>
    [Fact]
    public async Task StartAsync_RunsFullLifecycle()
    {
        var module = new RecordingWpfModule();
        var context = new WpfContext { IsLifetimeLinked = true };
        var lifetime = new FakeHostLifetime();
        var shellHolder = new ShellHolder();
        var regionProvider = new RegionProvider();
        var disposableView = new DisposableView("demo");
        var loggerProvider = new RecordingLoggerProvider();

        var (worker, service) = CreateWorker(
            context,
            lifetime,
            module,
            shellHolder,
            regionProvider,
            loggerProvider,
            handleUnhandledExceptions: true
        );

        await worker.StartAsync();

        Assert.NotNull(context.WpfApplication);
        Assert.True(context.IsRunning);
        Assert.Same(context.WpfApplication.Dispatcher, context.Dispatcher);

        // StartUiThread 在 StartAsync 返回后才在 UI 线程上执行，这里等待它完成。
        await WaitUntilAsync(
            () => module.ConfigureResourcesCalled && shellHolder.Shell?.IsVisible == true,
            TimeSpan.FromSeconds(5)
        );

        Assert.True(module.ConfigureResourcesCalled);
        Assert.NotNull(shellHolder.Shell);
        Assert.True(shellHolder.Shell!.IsVisible);
        Assert.False(lifetime.StopRequested);

        // pack URI 的解析依赖 Application 上下文，这里在应用已启动的前提下验证。
        var dictionary = new UriResourceDictionary(
            "pack://application:,,,/Len.Abp.Wpf.Tests;component/TestResource.xaml"
        );
        Assert.True(dictionary.Source.IsAbsoluteUri);

        // UI 线程上的未处理异常应被记录，且开关为 true 时进程继续运行。
        // 用 BeginInvoke 而不是 InvokeAsync：只有前者的异常会流向 Dispatcher.UnhandledException。
        // 这里不预先检查"处理器是否已挂接"——直接抛异常并断言被记录，
        // 既覆盖了挂接本身，也覆盖了处理逻辑；检查私有委托字段只是同一件事的间接证据。
        _ = context.WpfApplication.Dispatcher.BeginInvoke(
            new Action(() => throw new InvalidOperationException("UI 线程测试异常"))
        );

        await WaitUntilAsync(
            () => loggerProvider.Entries.Any(entry => entry.Exception is InvalidOperationException),
            TimeSpan.FromSeconds(5)
        );

        var logged = Assert.Single(
            loggerProvider.Entries,
            entry => entry.Exception is InvalidOperationException
        );
        Assert.Equal(LogLevel.Error, logged.Level);
        Assert.True(context.IsRunning);

        // 后台任务的未观察异常同样被兜底：记录并标记已观察。
        AssertUnobservedTaskExceptionHandlerAttached(service, loggerProvider);

        // 退出前放入一个区域，验证退出时会被拆除并释放其中的瞬态视图。
        var region = new Region("MainRegion", "MainView");
        region.Behaviors.Add(
            nameof(RegionMemberLifetimeBehavior),
            new RegionMemberLifetimeBehavior(regionProvider, new ViewScopeRegistry())
        );
        region.Add(disposableView);
        regionProvider.Add(region);

        // 通过后台工作器停止，覆盖 StopAsync 的关闭路径。
        await worker.StopAsync();

        // IsRunning 在退出流程一开始就被置为 false，停止宿主发生在拆除之后，
        // 因此必须等 StopRequested，否则断言会与拆除过程竞争。
        await WaitUntilAsync(() => lifetime.StopRequested, TimeSpan.FromSeconds(5));

        Assert.False(context.IsRunning);
        Assert.True(lifetime.StopRequested);
        Assert.Empty(regionProvider.Regions);
        Assert.True(disposableView.IsDisposed);
    }

    /// <summary>
    /// 启动成功信号发出之后的异常必须记录日志并停止宿主，不能静默丢弃。
    /// </summary>
    /// <remarks>
    /// 回归：<c>StartAsync</c> 原先在 <c>StartUiThread</c> 之前就发出成功信号，
    /// 之后抛的异常由 <c>TrySetException</c> 静默吞掉——任务已完成，返回值被忽略。
    /// 表现为"启动成功但界面不出现，日志里也没有线索"。
    /// 这条路径无法在测试里真正触发（一个 AppDomain 只能创建一个
    /// <see cref="Application"/>，且 <c>Run</c> 会阻塞），因此直接调用处理逻辑验证。
    /// </remarks>
    [Fact]
    public void HandlePostStartupFailure_LogsAndStopsApplication()
    {
        var context = new WpfContext();
        var lifetime = new FakeHostLifetime();
        var loggerProvider = new RecordingLoggerProvider();
        var service = CreateService(context, lifetime, loggerProvider);
        var exception = new InvalidOperationException("消息循环启动失败");

        service.HandlePostStartupFailure(exception);

        var entry = Assert.Single(loggerProvider.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Same(exception, entry.Exception);
        Assert.True(lifetime.StopRequested);
    }

    /// <summary>
    /// 宿主已在停止过程中时不再重复请求停止。
    /// </summary>
    [Fact]
    public void HandlePostStartupFailure_AlreadyStopping_DoesNotStopAgain()
    {
        var context = new WpfContext();
        var lifetime = new FakeHostLifetime();
        var loggerProvider = new RecordingLoggerProvider();
        var service = CreateService(context, lifetime, loggerProvider);

        lifetime.TriggerStopping();

        service.HandlePostStartupFailure(new InvalidOperationException("消息循环启动失败"));

        Assert.False(lifetime.StopRequested);
    }

    private static WpfThreadService CreateService(
        WpfContext context,
        FakeHostLifetime lifetime,
        RecordingLoggerProvider loggerProvider
    )
    {
        var provider = BuildProvider(context, lifetime, new RegionProvider(), loggerProvider);
        var lazyProvider = new AbpLazyServiceProvider(provider);

        return new WpfThreadService(
            lazyProvider,
            context,
            lifetime,
            new WpfExceptionHandler(context, lazyProvider),
            new RegionTeardownService(lazyProvider)
        );
    }

    /// <summary>
    /// 开关为 false 时记录异常但不标记为已处理，保持 WPF 原有的崩溃行为。
    /// </summary>
    [Fact]
    public void HandleDispatcherException_WhenNotEnabled_LogsAndReturnsFalse()
    {
        var context = new WpfContext { HandleUnhandledExceptions = false };
        var loggerProvider = new RecordingLoggerProvider();
        var handler = CreateExceptionHandler(context, loggerProvider);
        var exception = new InvalidOperationException("视图异常");

        var handled = handler.HandleDispatcherException(exception);

        Assert.False(handled);
        var entry = Assert.Single(loggerProvider.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Same(exception, entry.Exception);
    }

    /// <summary>
    /// 开关为 true 时把异常标记为已处理，进程继续运行。
    /// </summary>
    [Fact]
    public void HandleDispatcherException_WhenEnabled_ReturnsTrue()
    {
        var context = new WpfContext { HandleUnhandledExceptions = true };
        var loggerProvider = new RecordingLoggerProvider();
        var handler = CreateExceptionHandler(context, loggerProvider);

        var handled = handler.HandleDispatcherException(new InvalidOperationException("视图异常"));

        Assert.True(handled);
        Assert.Single(loggerProvider.Entries);
    }

    /// <summary>
    /// 后台任务的未观察异常被记录，且框架把它标记为已观察，避免进程被终结。
    /// </summary>
    [Fact]
    public void HandleUnobservedTaskException_LogsAndMarksObserved()
    {
        var context = new WpfContext();
        var loggerProvider = new RecordingLoggerProvider();
        var handler = CreateExceptionHandler(context, loggerProvider);
        var exception = new InvalidOperationException("后台任务异常");
        var args = new UnobservedTaskExceptionEventArgs(new AggregateException(exception));

        handler.HandleUnobservedTaskException(args.Exception);

        var entry = Assert.Single(loggerProvider.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Same(exception, entry.Exception?.InnerException);
    }

    /// <summary>
    /// 未观察异常被记录并标记为已观察。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 直接调用 <see cref="WpfThreadService.HandleUnobservedTaskException"/>，
    /// 不读取 <see cref="TaskScheduler"/> 的私有委托字段：后者依赖 BCL 内部实现，
    /// 且需要 <c>DynamicInvoke</c> 才能触发。处理器体本身是框架逻辑，
    /// 订阅动作（一行 <c>+=</c>）没有独立可测的语义。
    /// </para>
    /// <para>
    /// 一个 AppDomain 只能创建一个 <see cref="Application"/>，因此本断言并入
    /// <see cref="StartAsync_RunsFullLifecycle"/>，复用已启动的实例。
    /// </para>
    /// </remarks>
    private static void AssertUnobservedTaskExceptionHandlerAttached(
        WpfThreadService service,
        RecordingLoggerProvider loggerProvider
    )
    {
        var args = new UnobservedTaskExceptionEventArgs(
            new AggregateException(new InvalidOperationException("后台任务异常"))
        );

        service.HandleUnobservedTaskException(args);

        Assert.True(args.Observed);
        Assert.Contains(loggerProvider.Entries, entry => entry.Exception is AggregateException);
    }

    /// <summary>
    /// 链接宿主生命周期时，退出拆除区域后停止宿主。
    /// </summary>
    [Fact]
    public void ApplicationExit_WhenLifetimeLinked_StopsHostAfterTeardown()
    {
        var context = new WpfContext { IsLifetimeLinked = true };
        var lifetime = new FakeHostLifetime();
        var regionProvider = new RegionProvider();
        var disposableView = new DisposableView("demo");

        var region = new Region("MainRegion", "MainView");
        region.Behaviors.Add(
            nameof(RegionMemberLifetimeBehavior),
            new RegionMemberLifetimeBehavior(regionProvider, new ViewScopeRegistry())
        );
        region.Add(disposableView);
        regionProvider.Add(region);

        var service = CreateService(context, lifetime, regionProvider);

        service.OnApplicationExit();

        Assert.True(lifetime.StopRequested);
        Assert.Empty(regionProvider.Regions);
        Assert.True(disposableView.IsDisposed);
    }
    /// <summary>
    /// 退出时容器管理的视图不被释放，与区域行为的保活语义一致。
    /// </summary>
    [Fact]
    public void ApplicationExit_DoesNotDisposeContainerManagedViews()
    {
        var context = new WpfContext { IsLifetimeLinked = false };
        var lifetime = new FakeHostLifetime();
        var regionProvider = new RegionProvider();
        var singletonView = new SingletonDisposableView("demo");

        var region = new Region("MainRegion", "MainView");
        region.Behaviors.Add(
            nameof(RegionMemberLifetimeBehavior),
            new RegionMemberLifetimeBehavior(regionProvider, new ViewScopeRegistry())
        );
        region.Add(singletonView);
        regionProvider.Add(region);

        CreateService(context, lifetime, regionProvider).OnApplicationExit();

        Assert.Empty(regionProvider.Regions);
        Assert.False(singletonView.IsDisposed);
    }

    /// <summary>
    /// 没有注册区域提供程序时退出流程不抛异常。
    /// </summary>
    [Fact]
    public void ApplicationExit_WithoutRegionProvider_DoesNotThrow()
    {
        var context = new WpfContext { IsLifetimeLinked = false };
        var lifetime = new FakeHostLifetime();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(context);
        services.AddSingleton<IHostApplicationLifetime>(lifetime);
        var provider = services.BuildServiceProvider();
        var lazyProvider = new AbpLazyServiceProvider(provider);

        var service = new WpfThreadService(
            lazyProvider,
            context,
            lifetime,
            new WpfExceptionHandler(context, lazyProvider),
            new RegionTeardownService(lazyProvider)
        );

        service.OnApplicationExit();

        Assert.False(context.IsRunning);
    }

    /// <summary>
    /// 宿主已在停止过程中时不再重复请求停止，避免重入。
    /// </summary>
    [Fact]
    public void ApplicationExit_WhenHostAlreadyStopping_DoesNotRequestStopAgain()
    {
        var context = new WpfContext { IsLifetimeLinked = true };
        var lifetime = new FakeHostLifetime();
        var regionProvider = new RegionProvider();

        // 模拟宿主自己先发出了停止信号（例如外部 Ctrl+C）。
        lifetime.TriggerStopping();

        CreateService(context, lifetime, regionProvider).OnApplicationExit();

        Assert.False(lifetime.StopRequested);
        Assert.False(context.IsRunning);
    }

    /// <summary>
    /// 单个区域拆除抛异常时记录警告，其余区域仍被拆除。
    /// </summary>
    /// <remarks>
    /// 退出时容器可能已开始销毁，视图的 <c>Dispose</c> 里解析服务会抛异常。
    /// 一个区域失败不能阻止其他区域被拆除，也不能让退出流程崩溃。
    /// </remarks>
    [Fact]
    public void ApplicationExit_WhenRegionTeardownThrows_LogsAndContinues()
    {
        var context = new WpfContext { IsLifetimeLinked = false };
        var lifetime = new FakeHostLifetime();
        var regionProvider = new RegionProvider();
        var loggerProvider = new RecordingLoggerProvider();

        var brokenRegion = new ThrowingRegion("BrokenRegion");
        var healthyRegion = new Region("HealthyRegion", "MainView");
        var view = new DisposableView("demo");
        healthyRegion.Behaviors.Add(
            nameof(RegionMemberLifetimeBehavior),
            new RegionMemberLifetimeBehavior(regionProvider, new ViewScopeRegistry())
        );
        healthyRegion.Add(view);

        regionProvider.Add(brokenRegion);
        regionProvider.Add(healthyRegion);

        CreateService(context, lifetime, regionProvider, loggerProvider).OnApplicationExit();

        Assert.Contains(
            loggerProvider.Entries,
            entry => entry.Level == LogLevel.Warning && entry.Exception is InvalidOperationException
        );
        Assert.True(view.IsDisposed);
        Assert.Empty(regionProvider.Regions);
    }

    /// <summary>
    /// 拆除时抛异常的区域替身。
    /// </summary>
    private sealed class ThrowingRegion(string name) : IRegion
    {
        public IViewsCollection ActiveViews { get; } = new ViewsCollection([], _ => false);

        public IRegionBehaviorCollection Behaviors { get; } = new RegionBehaviorCollection(new Region(name, name));

        public string Name { get; } = name;

        public string Owner => Name;

        public IView? OwnerView => null;

        public IViewsCollection Views { get; } = new ViewsCollection([], _ => false);

        public void Activate(IView view) => throw new NotSupportedException();

        public void Add(IView view) => throw new NotSupportedException();

        public void Deactivate(IView view) => throw new NotSupportedException();

        public IView? GetView(string viewName) => null;

        public void Remove(IView view) => throw new NotSupportedException();

        public void RemoveAll() => throw new InvalidOperationException("拆除失败。");
    }

    private static WpfThreadService CreateService(
        WpfContext context,
        FakeHostLifetime lifetime,
        RegionProvider regionProvider,
        RecordingLoggerProvider? loggerProvider = null
    )
    {
        var provider = BuildProvider(context, lifetime, regionProvider, loggerProvider);
        var lazyProvider = new AbpLazyServiceProvider(provider);

        return new WpfThreadService(
            lazyProvider,
            context,
            lifetime,
            new WpfExceptionHandler(context, lazyProvider),
            new RegionTeardownService(lazyProvider)
        );
    }

    private static WpfExceptionHandler CreateExceptionHandler(
        WpfContext context,
        RecordingLoggerProvider loggerProvider
    )
    {
        var provider = BuildProvider(context, new FakeHostLifetime(), new RegionProvider(), loggerProvider);

        return new WpfExceptionHandler(context, new AbpLazyServiceProvider(provider));
    }

    private static ServiceProvider BuildProvider(
        WpfContext context,
        FakeHostLifetime lifetime,
        RegionProvider regionProvider,
        RecordingLoggerProvider? loggerProvider
    )
    {
        var services = new ServiceCollection();
        if (loggerProvider is null)
        {
            services.AddLogging();
        }
        else
        {
            services.AddLogging(builder => builder.AddProvider(loggerProvider));
        }

        services.AddSingleton(context);
        services.AddSingleton<IHostApplicationLifetime>(lifetime);
        services.AddSingleton<IRegionProvider>(regionProvider);

        return services.BuildServiceProvider();
    }

    private static (WpfBackgroundWorker Worker, WpfThreadService Service) CreateWorker(
        WpfContext context,
        FakeHostLifetime lifetime,
        RecordingWpfModule module,
        ShellHolder shellHolder,
        RegionProvider regionProvider,
        RecordingLoggerProvider loggerProvider,
        bool handleUnhandledExceptions
    )
    {
        var application = new FakeAbpApplication
        {
            Modules = [new FakeModuleDescriptor(module)],
        };

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddProvider(loggerProvider));
        services.AddSingleton<IAbpApplication>(application);
        services.AddSingleton(context);
        services.AddSingleton<IHostApplicationLifetime>(lifetime);
        services.AddSingleton<IRegionProvider>(regionProvider);
        services.Configure<WpfOptions>(options =>
        {
            options.HandleUnhandledExceptions = handleUnhandledExceptions;
        });

        // Window 必须在创建它的线程上使用，因此由容器在 WPF 应用线程上延迟构造。
        services.AddSingleton<IShell>(_ =>
        {
            var shell = new RecordingShell();
            shellHolder.Shell = shell;
            return shell;
        });

        var provider = services.BuildServiceProvider();
        var lazyProvider = new AbpLazyServiceProvider(provider);
        var exceptionHandler = new WpfExceptionHandler(context, lazyProvider);
        var regionTeardownService = new RegionTeardownService(lazyProvider);

        var service = new WpfThreadService(
            lazyProvider,
            context,
            lifetime,
            exceptionHandler,
            regionTeardownService
        );

        // 工作器通过延迟服务提供程序解析 WpfThreadService，必须注册同一实例。
        services.AddSingleton(service);
        provider = services.BuildServiceProvider();
        lazyProvider = new AbpLazyServiceProvider(provider);

        var worker = new WpfBackgroundWorker { LazyServiceProvider = lazyProvider };

        return (worker, service);
    }

    private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(20);
        }
    }

    /// <summary>
    /// 非 <see cref="Window"/> 的 <see cref="IShell"/> 实现被跳过并记录警告，不抛异常。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 回归：原实现是 <c>GetServices&lt;IShell&gt;().Cast&lt;Window&gt;()</c>，
    /// 遇到非窗口实现直接抛 <see cref="InvalidCastException"/>。异常发生在 UI 线程准备阶段，
    /// 界面永远不出现，而 <see cref="ShellActivator"/> 对同样的输入是跳过——
    /// 两处口径不一致。
    /// </para>
    /// <para>
    /// 本用例不创建 <see cref="Application"/>：<see cref="WpfThreadService.GetShellWindows"/>
    /// 只读容器，不需要应用上下文。这样避免与"一个 AppDomain 只能有一个 Application"冲突。
    /// </para>
    /// </remarks>
    [Fact]
    public void GetShellWindows_SkipsNonWindowShellAndLogsWarning()
    {
        // Window 有线程亲和性，构造与读取都必须在 STA 线程上。
        Sta.Run(() =>
        {
            var loggerProvider = new RecordingLoggerProvider();
            var windowShell = new NonRecordingShell();

            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddProvider(loggerProvider));
            services.AddSingleton<IShell>(new NotAWindowShell());
            services.AddSingleton<IShell>(windowShell);

            var provider = services.BuildServiceProvider();
            var lazyProvider = new AbpLazyServiceProvider(provider);
            var context = new WpfContext();

            var service = new WpfThreadService(
                lazyProvider,
                context,
                new FakeHostLifetime(),
                new WpfExceptionHandler(context, lazyProvider),
                new RegionTeardownService(lazyProvider)
            );

            var windows = service.GetShellWindows();

            // 只返回窗口实现，非窗口实现被跳过而不是抛异常。
            Assert.Single(windows);
            Assert.Same(windowShell, windows[0]);

            // 跳过必须可见：否则配置错误会变成"界面不出现但日志里什么都没有"。
            Assert.Contains(
                loggerProvider.Entries,
                entry => entry.Level == LogLevel.Warning
                    && entry.Message.Contains(nameof(NotAWindowShell), StringComparison.Ordinal)
            );
        });
    }

    /// <summary>
    /// 保存容器在 WPF 应用线程上创建的 Shell，供测试断言。
    /// </summary>
    private sealed class ShellHolder
    {
        public RecordingShell? Shell { get; set; }
    }

    /// <summary>
    /// 标记了 <see cref="IShell"/> 但不是 <see cref="Window"/> 的实现。
    /// </summary>
    private sealed class NotAWindowShell : IShell;

    /// <summary>
    /// 测试用窗口，不写入 <see cref="ShellHolder"/>。
    /// </summary>
    private sealed class NonRecordingShell : Window, IShell;

    private sealed class RecordingWpfModule : AbpWpfModule
    {
        public bool ConfigureResourcesCalled { get; private set; }

        public override void ConfigureResources(ICollection<ResourceDictionary> resources)
        {
            ConfigureResourcesCalled = true;
            resources.Add([]);
        }
    }

    private sealed class FakeModuleDescriptor(IAbpModule module) : IAbpModuleDescriptor
    {
        public Type Type => module.GetType();

        public System.Reflection.Assembly Assembly => module.GetType().Assembly;

        public System.Reflection.Assembly[] AllAssemblies => [Assembly];

        public IAbpModule Instance => module;

        public bool IsLoadedAsPlugIn => false;

        public IReadOnlyList<IAbpModuleDescriptor> Dependencies => [];
    }

    private sealed class RecordingShell : Window, IShell;

    /// <summary>
    /// 记录是否被释放的视图。
    /// </summary>
    private class DisposableView(string viewName) : IView, IDisposable
    {
        public bool IsDisposed { get; private set; }

        public object ViewModel { get; } = new object();

        public string ViewName { get; } = viewName;

        public void Dispose() => IsDisposed = true;
    }

    /// <summary>
    /// 容器管理的视图，退出时不应被释放。
    /// </summary>
    private sealed class SingletonDisposableView(string viewName)
        : DisposableView(viewName),
            ISingletonDependency;
}
