using Len.Abp.Wpf;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Volo.Abp.DependencyInjection;
using Xunit;

namespace Len.Abp.Wpf.Tests;

/// <summary>
/// 覆盖 <see cref="WpfBackgroundWorker"/> 中不涉及创建 WPF 应用的分支。
/// </summary>
/// <remarks>
/// 一个 AppDomain 只能创建一个 <see cref="System.Windows.Application"/>，
/// 因此真正启动 UI 线程的路径由 <see cref="WpfThreadServiceTests"/> 统一覆盖。
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
    /// 取消令牌已取消时不关闭应用。
    /// </summary>
    [Fact]
    public async Task StopAsync_WithCancelledToken_DoesNotShutdown()
    {
        var context = new WpfContext { IsRunning = true };
        var worker = CreateWorker(context, new FakeHostLifetime(), new WpfOptions());
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await worker.StopAsync(cts.Token);

        Assert.True(context.IsRunning);
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
}
