using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundWorkers;

namespace Len.Abp.Wpf;

internal class WpfBackgroundWorker : BackgroundWorkerBase
{
    private WpfThreadService WpfThreadService => LazyServiceProvider.LazyGetRequiredService<WpfThreadService>();
    private WpfOptions WpfOptions => LazyServiceProvider.LazyGetRequiredService<IOptions<WpfOptions>>().Value;
    private WpfContext WpfContext => LazyServiceProvider.LazyGetRequiredService<WpfContext>();

    public override async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await base.StartAsync(cancellationToken);
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        WpfContext.ShutdownMode = WpfOptions.ShutdownMode;
        WpfContext.IsLifetimeLinked = WpfOptions.IsLifetimeLinked;

        await WpfThreadService.StartAsync();
    }

    public override async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await base.StopAsync(cancellationToken);
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        if (WpfContext.IsRunning)
        {
            Logger.LogDebug("Stopping WPF due to application exit.");

            await WpfContext.Dispatcher.InvokeAsync(WpfContext.WpfApplication.Shutdown);
        }
    }
}
