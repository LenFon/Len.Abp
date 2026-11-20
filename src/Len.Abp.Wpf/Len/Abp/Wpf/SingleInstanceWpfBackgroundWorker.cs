using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundWorkers;

namespace Len.Abp.Wpf;

internal class SingleInstanceWpfBackgroundWorker : BackgroundWorkerBase
{
    private IInstanceActivationChannel? _channel;
    private WpfOptions WpfOptions => LazyServiceProvider.LazyGetRequiredService<IOptions<WpfOptions>>().Value;
    private IHostApplicationLifetime HostApplicationLifetime =>
        LazyServiceProvider.LazyGetRequiredService<IHostApplicationLifetime>();

    /// <summary>
    /// 启动单实例检测；若已有实例在运行，则请求其激活窗口并停止当前应用。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    public override async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await base.StartAsync(cancellationToken);

        if (cancellationToken.IsCancellationRequested || !WpfOptions.SingleInstance)
        {
            return;
        }

        _channel = LazyServiceProvider.LazyGetRequiredService<IInstanceActivationChannel>();

        if (_channel.TryClaimPrimary())
        {
            HostApplicationLifetime.ApplicationStopping.Register(OnStopping);
            _channel.StartListening(ActivateExistingInstance);

            return;
        }

        Logger.LogWarning("应用已在运行，正在激活已有窗口。");

        // 先通知再退出：退出路径可能很快，晚一步就可能来不及发出信号。
        _channel.SignalPrimary();
        _channel.Dispose();
        _channel = null;

        HostApplicationLifetime.StopApplication();
    }

    /// <summary>
    /// 在 UI 线程上激活已有窗口。
    /// </summary>
    /// <remarks>
    /// 回调来自监听线程，<see cref="System.Windows.Window"/> 有线程亲和性，必须切到 UI 线程。
    /// 应用尚未启动完成时忽略请求：此时没有可激活的窗口。
    /// </remarks>
    private void ActivateExistingInstance()
    {
        var context = LazyServiceProvider.LazyGetService<WpfContext>();

        if (context is not { IsRunning: true })
        {
            return;
        }

        context.Dispatcher.InvokeAsync(() =>
        {
            try
            {
                LazyServiceProvider.LazyGetRequiredService<ShellActivator>().Activate();
            }
            catch (Exception exception)
            {
                // 激活失败不应影响主实例运行，记录后忽略。
                Logger.LogWarning(exception, "激活已有窗口失败。");
            }
        });
    }

    private void OnStopping()
    {
        Logger.LogInformation("已调用 OnStopping，正在关闭应用。");

        _channel?.Dispose();
        _channel = null;
    }
}
