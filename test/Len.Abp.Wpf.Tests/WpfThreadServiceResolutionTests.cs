using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Volo.Abp;
using Volo.Abp.Modularity;
using Xunit;

namespace Len.Abp.Wpf.Tests;

/// <summary>
/// 验证 <see cref="WpfThreadService"/> 及其依赖能被 ABP 容器按约定解析。
/// </summary>
/// <remarks>
/// 其余测试都手工 <c>new</c> 出 <see cref="WpfThreadService"/>，因此漏掉依赖标记接口时
/// 测试仍然通过，而真实宿主启动会抛 <c>DependencyResolutionException</c>。
/// 这里必须走容器解析，才能覆盖「约定注册」这条路径。
/// </remarks>
public class WpfThreadServiceResolutionTests
{
    /// <summary>
    /// 容器能解析 <see cref="WpfThreadService"/>，说明其构造函数参数都已注册。
    /// </summary>
    [Fact]
    public async Task WpfThreadService_IsResolvableFromContainer()
    {
        using var application = await AbpApplicationFactory.CreateAsync<ResolutionModule>();
        var provider = application.Services.BuildServiceProvider();

        var service = provider.GetRequiredService<WpfThreadService>();

        Assert.NotNull(service);
    }

    /// <summary>
    /// 两个依赖都是单例，与宿主服务的生命周期一致。
    /// </summary>
    /// <remarks>
    /// 断言接口而非具体类型：测试程序集里的替换实现会全局替换 <see cref="IWpfExceptionHandler"/>，
    /// 具体类型断言会随测试程序集内容变化而失效。
    /// </remarks>
    [Fact]
    public async Task Dependencies_AreSingletons()
    {
        using var application = await AbpApplicationFactory.CreateAsync<ResolutionModule>();
        var provider = application.Services.BuildServiceProvider();

        Assert.Same(
            provider.GetRequiredService<IWpfExceptionHandler>(),
            provider.GetRequiredService<IWpfExceptionHandler>()
        );
        Assert.Same(
            provider.GetRequiredService<RegionTeardownService>(),
            provider.GetRequiredService<RegionTeardownService>()
        );
    }

    [DependsOn(typeof(LenAbpWpfModule))]
    public sealed class ResolutionModule : AbpModule
    {
        /// <summary>
        /// 补上 Generic Host 才会注册的生命周期服务，使容器能完整解析宿主服务。
        /// </summary>
        /// <param name="context">服务配置上下文。</param>
        public override void ConfigureServices(ServiceConfigurationContext context) =>
            context.Services.AddSingleton<IHostApplicationLifetime, FakeHostApplicationLifetime>();
    }

    private sealed class FakeHostApplicationLifetime : IHostApplicationLifetime
    {
        public CancellationToken ApplicationStarted => CancellationToken.None;

        public CancellationToken ApplicationStopping => CancellationToken.None;

        public CancellationToken ApplicationStopped => CancellationToken.None;

        public void StopApplication()
        {
        }
    }
}
