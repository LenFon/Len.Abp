using Len.Abp.Wpf;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Volo.Abp.DependencyInjection;
using Xunit;

namespace Len.Abp.Wpf.Tests;

/// <summary>
/// 覆盖 <see cref="SingleInstanceWpfBackgroundWorker"/> 的单实例检测。
/// </summary>
[Collection(GlobalServiceProviderCollection.Name)]
public class SingleInstanceWpfBackgroundWorkerTests
{
    /// <summary>
    /// 未启用单实例时不创建互斥量，也不停止应用。
    /// </summary>
    [Fact]
    public async Task StartAsync_WhenDisabled_DoesNotStopApplication()
    {
        var lifetime = new FakeHostLifetime();
        var worker = CreateWorker(new WpfOptions { SingleInstance = false }, lifetime, "disabled-app");

        await worker.StartAsync();

        Assert.False(lifetime.StopRequested);
    }

    /// <summary>
    /// 首次启动时创建标记互斥量，不停止应用。
    /// </summary>
    [Fact]
    public async Task StartAsync_FirstInstance_DoesNotStopApplication()
    {
        var lifetime = new FakeHostLifetime();
        var worker = CreateWorker(new WpfOptions { SingleInstance = true }, lifetime, $"first-{Guid.NewGuid():N}");

        await worker.StartAsync();

        Assert.False(lifetime.StopRequested);

        await worker.StopAsync();
    }

    /// <summary>
    /// 第二个实例检测到已有实例时停止应用。
    /// </summary>
    [Fact]
    public async Task StartAsync_SecondInstance_StopsApplication()
    {
        var name = $"second-{Guid.NewGuid():N}";
        var firstLifetime = new FakeHostLifetime();
        var first = CreateWorker(new WpfOptions { SingleInstance = true }, firstLifetime, name);
        await first.StartAsync();

        var secondLifetime = new FakeHostLifetime();
        var second = CreateWorker(new WpfOptions { SingleInstance = true }, secondLifetime, name);
        await second.StartAsync();

        Assert.False(firstLifetime.StopRequested);
        Assert.True(secondLifetime.StopRequested);

        await first.StopAsync();
        await second.StopAsync();
    }

    /// <summary>
    /// 取消令牌已取消时不执行单实例检测。
    /// </summary>
    [Fact]
    public async Task StartAsync_WithCancelledToken_DoesNotStopApplication()
    {
        var lifetime = new FakeHostLifetime();
        var worker = CreateWorker(new WpfOptions { SingleInstance = true }, lifetime, $"cancelled-{Guid.NewGuid():N}");
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await worker.StartAsync(cts.Token);

        Assert.False(lifetime.StopRequested);
    }

    private static SingleInstanceWpfBackgroundWorker CreateWorker(
        WpfOptions options,
        FakeHostLifetime lifetime,
        string applicationName
    )
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IHostApplicationLifetime>(lifetime);
        services.AddSingleton<IHostEnvironment>(new FakeHostEnvironment(applicationName));
        services.Configure<WpfOptions>(o => o.SingleInstance = options.SingleInstance);

        var provider = services.BuildServiceProvider();

        return new SingleInstanceWpfBackgroundWorker
        {
            LazyServiceProvider = new AbpLazyServiceProvider(provider),
        };
    }
}
