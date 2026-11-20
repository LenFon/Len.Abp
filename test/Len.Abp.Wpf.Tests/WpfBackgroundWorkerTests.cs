using Len.Abp.Wpf;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Windows.Threading;
using Volo.Abp.DependencyInjection;
using Xunit;

namespace Len.Abp.Wpf.Tests;

/// <summary>
/// 覆盖 <see cref="WpfBackgroundWorker"/> 中不涉及创建 WPF 应用的分支。
/// </summary>
/// <remarks>
/// 一个 AppDomain 只能创建一个 <see cref="System.Windows.Application"/>，
/// 因此真正启动 UI 线程的路径由 <see cref="WpfThreadServiceTests"/> 统一覆盖。
/// 这里用独立 STA 线程上的真实调度器，配合重写 <see cref="WpfContext.Shutdown"/> 的替身，
/// 验证停止流程的判定与封送。
/// </remarks>
[Collection(GlobalServiceProviderCollection.Name)]
public class WpfBackgroundWorkerTests
{
    /// <summary>
    /// 取消令牌已取消时不启动 UI 线程。
    /// </summary>
    [Fact]
    public async Task StartAsync_WithCancelledToken_DoesNotStartUiThread()
    {
        var context = new WpfContext();
        var worker = CreateWorker(context, new FakeHostLifetime(), new WpfOptions());
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await worker.StartAsync(cts.Token);

        Assert.Null(context.WpfApplication);
    }

    /// <summary>
    /// UI 未运行时停止操作不访问调度器。
    /// </summary>
    [Fact]
    public async Task StopAsync_WhenNotRunning_DoesNothing()
    {
        var context = new WpfContext { IsRunning = false };
        var worker = CreateWorker(context, new FakeHostLifetime(), new WpfOptions());

        await worker.StopAsync();

        Assert.False(context.IsRunning);
    }

    /// <summary>
    /// 取消令牌已取消时仍然关闭 WPF 应用。
    /// </summary>
    /// <remarks>
    /// 回归：ABP 在 <c>OnApplicationShutdownAsync</c> 里传入的是
    /// <c>IHostApplicationLifetime.ApplicationStopping</c>，调用到这里时它已经取消。
    /// 原实现按令牌提前返回，导致宿主主动停止（Ctrl+C、<c>StopApplication</c>、
    /// 嵌入式宿主）时永远不关 WPF：<c>Application.Exit</c> 不触发，
    /// 区域拆除与视图释放全部跳过。
    /// </remarks>
    [Fact]
    public void StopAsync_WithCancelledToken_StillShutsDownApplication()
    {
        Sta.Run(() =>
        {
            var dispatcher = Dispatcher.CurrentDispatcher;
            var context = new RecordingWpfContext(dispatcher) { IsRunning = true };
            var worker = CreateWorker(context, new FakeHostLifetime(), new WpfOptions());
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // StopAsync 会 await 调度器上排队的关闭请求；在调度器线程上同步等待会死锁，
            // 因此另起线程调用，并在本线程泵送调度器。
            var stop = Task.Run(() => worker.StopAsync(cts.Token));

            PumpUntilCompleted(dispatcher, stop);

            stop.GetAwaiter().GetResult();

            Assert.Equal(1, context.ShutdownCount);
        });
    }

    /// <summary>
    /// 调度器已开始关闭时不再排队关闭请求，也不抛异常。
    /// </summary>
    /// <remarks>
    /// 用户刚关掉最后一个窗口时，<c>Exit</c> 事件与宿主停止可能同时发生。
    /// 此时 <see cref="Dispatcher.InvokeAsync(Action)"/> 返回的操作会被中止，
    /// 等待它会抛 <see cref="TaskCanceledException"/>。
    /// </remarks>
    [Fact]
    public void StopAsync_WhenDispatcherAlreadyShutDown_DoesNotThrow()
    {
        Sta.Run(() =>
        {
            var dispatcher = Dispatcher.CurrentDispatcher;
            dispatcher.InvokeShutdown();

            var context = new RecordingWpfContext(dispatcher) { IsRunning = true };
            var worker = CreateWorker(context, new FakeHostLifetime(), new WpfOptions());

            worker.StopAsync().GetAwaiter().GetResult();

            Assert.Equal(0, context.ShutdownCount);
        });
    }

    /// <summary>
    /// 泵送调度器，直到 <paramref name="task"/> 完成。
    /// </summary>
    /// <param name="dispatcher">当前线程的调度器。</param>
    /// <param name="task">在另一个线程上执行、需要调度器配合完成的任务。</param>
    /// <remarks>
    /// 用 Background 优先级结束帧：Normal 优先级上排队的关闭请求会先执行。
    /// 另加一个超时计时器兜底，避免任务永不完成时测试挂死。
    /// </remarks>
    private static void PumpUntilCompleted(Dispatcher dispatcher, Task task)
    {
        var frame = new DispatcherFrame();
        var timeout = new DispatcherTimer(DispatcherPriority.Send, dispatcher)
        {
            Interval = TimeSpan.FromSeconds(10),
        };

        timeout.Tick += (_, _) =>
        {
            timeout.Stop();
            frame.Continue = false;
        };
        timeout.Start();

        _ = task.ContinueWith(
            _ => dispatcher.BeginInvoke(
                DispatcherPriority.Background,
                new Action(() => frame.Continue = false)
            ),
            TaskScheduler.Default
        );

        Dispatcher.PushFrame(frame);
        timeout.Stop();
    }

    private static WpfBackgroundWorker CreateWorker(
        WpfContext context,
        FakeHostLifetime lifetime,
        WpfOptions options
    )
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(context);
        services.AddSingleton<IHostApplicationLifetime>(lifetime);
        services.Configure<WpfOptions>(o =>
        {
            o.ShutdownMode = options.ShutdownMode;
            o.IsLifetimeLinked = options.IsLifetimeLinked;
        });

        var provider = services.BuildServiceProvider();

        return new WpfBackgroundWorker
        {
            LazyServiceProvider = new AbpLazyServiceProvider(provider),
        };
    }

    /// <summary>
    /// 记录关闭请求的上下文替身：不依赖 <see cref="System.Windows.Application"/> 实例。
    /// </summary>
    /// <param name="dispatcher">当前 STA 线程的调度器。</param>
    private sealed class RecordingWpfContext(Dispatcher dispatcher) : WpfContext
    {
        /// <summary>
        /// 获取收到的关闭请求次数。
        /// </summary>
        public int ShutdownCount { get; private set; }

        /// <inheritdoc/>
        public override Dispatcher Dispatcher => dispatcher;

        /// <inheritdoc/>
        public override void Shutdown() => ShutdownCount++;
    }
}
