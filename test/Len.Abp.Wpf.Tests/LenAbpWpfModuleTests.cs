using Len.Abp.Wpf.Navigation.Regions;
using Len.Abp.Wpf.Navigation.Regions.Behaviors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Volo.Abp.Modularity;
using Xunit;

namespace Len.Abp.Wpf.Tests;

/// <summary>
/// 覆盖 <see cref="LenAbpWpfModule"/> 的选项注册与行为键收集。
/// </summary>
/// <remarks>
/// 行为键在 <c>PostConfigureServices</c> 中通过扫描服务描述符收集，
/// 因此这里先模拟 ABP 的程序集扫描（<c>AddAssembly</c>），再调用 <c>PostConfigureServices</c>。
/// </remarks>
[Collection(GlobalServiceProviderCollection.Name)]
public class LenAbpWpfModuleTests
{
    /// <summary>
    /// 配置服务时把以键控方式注册的 <see cref="IRegionBehavior"/> 登记为默认行为键。
    /// </summary>
    [Fact]
    public void PostConfigureServices_CollectsRegionBehaviorKeys()
    {
        var services = new ServiceCollection();
        var context = new ServiceConfigurationContext(services);

        new LenAbpWpfModule().PreConfigureServices(context);

        // 复现 ABP 在 ConfigureServices 阶段对模块程序集的扫描。
        services.AddAssembly(typeof(LenAbpWpfModule).Assembly);

        new LenAbpWpfModule().PostConfigureServices(context);

        var options = services.BuildServiceProvider().GetRequiredService<IOptions<RegionOptions>>().Value;

        Assert.Contains(nameof(RegionProviderRegistrationBehavior), options.BehaviorKeys);
        Assert.Contains(nameof(RegionMemberLifetimeBehavior), options.BehaviorKeys);
    }

    /// <summary>
    /// 行为键收集不依赖 Autofac 的注册回调，默认 DI 容器下同样生效。
    /// </summary>
    [Fact]
    public void PostConfigureServices_WorksWithoutRegistrationCallbacks()
    {
        var services = new ServiceCollection();
        var context = new ServiceConfigurationContext(services);

        new LenAbpWpfModule().PreConfigureServices(context);
        services.AddAssembly(typeof(LenAbpWpfModule).Assembly);
        new LenAbpWpfModule().PostConfigureServices(context);

        // 默认容器不会触发 OnRegistered，这里断言键仍然被收集到。
        var registrationActions = Microsoft.Extensions.DependencyInjection
            .ServiceCollectionRegistrationActionExtensions.GetRegistrationActionList(services);

        Assert.Empty(registrationActions);

        var options = services.BuildServiceProvider().GetRequiredService<IOptions<RegionOptions>>().Value;
        Assert.Equal(2, options.BehaviorKeys.Count());
    }

    /// <summary>
    /// 没有区域行为注册时不产生任何行为键。
    /// </summary>
    [Fact]
    public void PostConfigureServices_WithoutBehaviors_AddsNoKeys()
    {
        var services = new ServiceCollection();
        var context = new ServiceConfigurationContext(services);

        new LenAbpWpfModule().PreConfigureServices(context);
        new LenAbpWpfModule().PostConfigureServices(context);

        var options = services.BuildServiceProvider().GetRequiredService<IOptions<RegionOptions>>().Value;

        Assert.Empty(options.BehaviorKeys);
    }

    /// <summary>
    /// 配置服务时注册 <see cref="WpfOptions"/>。
    /// </summary>
    [Fact]
    public void PreConfigureServices_RegistersWpfOptions()
    {
        var services = new ServiceCollection();
        var context = new ServiceConfigurationContext(services);

        new LenAbpWpfModule().PreConfigureServices(context);

        Assert.NotNull(services.BuildServiceProvider().GetService<IOptions<WpfOptions>>());
    }

    [DependsOn(typeof(LenAbpWpfModule))]
    public sealed class WpfModule : AbpModule;
}
