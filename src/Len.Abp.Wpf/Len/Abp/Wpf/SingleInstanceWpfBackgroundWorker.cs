using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundWorkers;

namespace Len.Abp.Wpf;

internal class SingleInstanceWpfBackgroundWorker : BackgroundWorkerBase
{
    private Mutex? _mutex;
    private WpfOptions WpfOptions => LazyServiceProvider.LazyGetRequiredService<IOptions<WpfOptions>>().Value;
    private IHostApplicationLifetime HostApplicationLifetime =>
        LazyServiceProvider.LazyGetRequiredService<IHostApplicationLifetime>();
    private IHostEnvironment Environment => LazyServiceProvider.LazyGetRequiredService<IHostEnvironment>();

    /// <summary>
    /// 启动单实例检测；若已有实例在运行，则停止当前应用。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    public override async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await base.StartAsync(cancellationToken);

        if (!WpfOptions.SingleInstance)
        {
            return;
        }

        // 该互斥量从不被持有：只用它的存在性作为“已在运行”的标记。
        // 这样检测不涉及线程亲和性，关闭时也无需调用 ReleaseMutex。
        var mutexName = $@"Local\Len.Abp.Wpf.{Environment.ApplicationName}";

        _mutex = new Mutex(initiallyOwned: false, mutexName, out var createdNew);

        if (createdNew)
        {
            HostApplicationLifetime.ApplicationStopping.Register(OnStopping);

            return;
        }

        // 另一个实例已持有标记；释放本实例的句柄，避免继续占用该名称。
        _mutex.Dispose();
        _mutex = null;

        Logger.LogWarning(
            "应用 {applicationName} 已在运行，正在停止当前应用。",
            Environment.ApplicationName
        );

        HostApplicationLifetime.StopApplication();
    }

    private void OnStopping()
    {
        Logger.LogInformation("已调用 OnStopping，正在关闭应用。");

        _mutex?.Dispose();
        _mutex = null;
    }
}
