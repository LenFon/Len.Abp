using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundWorkers;

namespace Len.Abp.Wpf;

internal class WpfBackgroundWorker : BackgroundWorkerBase
{
    private WpfThreadService WpfThreadService => LazyServiceProvider.LazyGetRequiredService<WpfThreadService>();
    private WpfOptions WpfOptions => LazyServiceProvider.LazyGetRequiredService<IOptions<WpfOptions>>().Value;
    private WpfContext WpfContext => LazyServiceProvider.LazyGetRequiredService<WpfContext>();

    /// <summary>
    /// 启动 WPF UI 线程。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    public override async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await base.StartAsync(cancellationToken);
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        WpfContext.ShutdownMode = WpfOptions.ShutdownMode;
        WpfContext.IsLifetimeLinked = WpfOptions.IsLifetimeLinked;
        WpfContext.HandleUnhandledExceptions = WpfOptions.HandleUnhandledExceptions;

        await WpfThreadService.StartAsync();
    }

    /// <summary>
    /// 停止 WPF UI 线程。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <remarks>
    /// <para>
    /// <b>不能按 <paramref name="cancellationToken"/> 提前返回。</b>ABP 在
    /// <c>OnApplicationShutdownAsync</c> 里传入的是 <c>IHostApplicationLifetime.ApplicationStopping</c>，
    /// 调用到这里时它已经取消。按令牌守卫会让宿主主动停止（Ctrl+C、<c>StopApplication</c>、
    /// 嵌入式宿主）时永远不关 WPF：<c>Application.Exit</c> 不触发，
    /// <see cref="WpfThreadService.OnApplicationExit"/> 里的区域拆除与视图释放全部跳过。
    /// 是否还需要关闭只由 <see cref="WpfContext.IsRunning"/> 决定。
    /// </para>
    /// <para>
    /// 用户关掉最后一个窗口时走的是另一条路径：<c>Exit</c> 事件先把
    /// <see cref="WpfContext.IsRunning"/> 置为 <see langword="false"/>，这里因此直接返回。
    /// </para>
    /// </remarks>
    public override async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await base.StopAsync(cancellationToken);

        if (!WpfContext.IsRunning)
        {
            return;
        }

        Logger.LogDebug("应用退出，正在停止 WPF。");

        var dispatcher = WpfContext.Dispatcher;

        // 应用可能已经在关闭（例如用户刚关掉最后一个窗口），此时调度器不再接收新工作，
        // InvokeAsync 返回的操作会被中止，等待它会抛 TaskCanceledException。
        if (dispatcher.HasShutdownStarted)
        {
            return;
        }

        try
        {
            await dispatcher.InvokeAsync(WpfContext.Shutdown);
        }
        catch (TaskCanceledException)
        {
            // 关闭请求与调度器停止竞争：应用本来就在退出，无需处理。
        }
    }
}
