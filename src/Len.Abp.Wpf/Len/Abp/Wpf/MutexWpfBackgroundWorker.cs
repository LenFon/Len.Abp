using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundWorkers;

namespace Len.Abp.Wpf;

internal class MutexWpfBackgroundWorker : BackgroundWorkerBase
{
    private Semaphore? _locker;
    private WpfOptions WpfOptions => LazyServiceProvider.LazyGetRequiredService<IOptions<WpfOptions>>().Value;
    private IHostApplicationLifetime HostApplicationLifetime =>
        LazyServiceProvider.LazyGetRequiredService<IHostApplicationLifetime>();
    private IHostEnvironment Environment => LazyServiceProvider.LazyGetRequiredService<IHostEnvironment>();

    public override async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await base.StartAsync(cancellationToken);
        if (WpfOptions.SingleInstance)
        {
            _locker = new(1, 1, Environment.ApplicationName);

            var isLocked = !_locker.WaitOne(TimeSpan.FromMicroseconds(500));

            HostApplicationLifetime.ApplicationStopping.Register(OnStopping);

            if (isLocked)
            {
                Logger.LogWarning(
                    "Application {applicationName} already running, stopping application.",
                    Environment.ApplicationName
                );

                HostApplicationLifetime.StopApplication();
            }
        }
    }

    private void OnStopping()
    {
        Logger.LogInformation("OnStopping has been called, closing application.");

        _locker?.Dispose();
    }
}
