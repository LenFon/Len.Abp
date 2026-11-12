using Len.Abp.Wpf.Navigation.Regions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Modularity;
using Xunit;

namespace Len.Abp.Wpf.Tests;

/// <summary>
/// 端到端验证：不启用 Autofac 时，区域行为键仍被收集，区域仍能注册到提供程序。
/// </summary>
[Collection(GlobalServiceProviderCollection.Name)]
public class RegionRegistrationEndToEndTests
{
    /// <summary>
    /// 默认 DI 容器下行为键非空。
    /// </summary>
    [Fact]
    public async Task DefaultProvider_CollectsBehaviorKeys()
    {
        using var application = await AbpApplicationFactory.CreateAsync<HostModule>();

        var options = application.Services
            .BuildServiceProvider()
            .GetRequiredService<IOptions<RegionOptions>>()
            .Value;

        Assert.Contains(nameof(Navigation.Regions.Behaviors.RegionProviderRegistrationBehavior), options.BehaviorKeys);
        Assert.Contains(nameof(Navigation.Regions.Behaviors.RegionMemberLifetimeBehavior), options.BehaviorKeys);
    }

    /// <summary>
    /// 默认 DI 容器下创建的区域会通过行为自动注册到 <see cref="IRegionProvider"/>。
    /// </summary>
    [Fact]
    public async Task DefaultProvider_RegionIsRegisteredInProvider()
    {
        using var application = await AbpApplicationFactory.CreateAsync<HostModule>();
        var provider = application.Services.BuildServiceProvider();

        var regionProvider = provider.GetRequiredService<IRegionProvider>();
        var adapterFinder = provider.GetRequiredService<IRegionAdapterFinder>();

        Sta.Run(() =>
        {
            var adapter = adapterFinder.FindByControlType(typeof(System.Windows.Controls.ContentControl));
            adapter.Initialize(new System.Windows.Controls.ContentControl(), "MainRegion", "MainView");
        });

        Assert.Single(regionProvider.Regions);
        Assert.Equal("MainRegion", regionProvider.Regions[0].Name);
    }

    [DependsOn(typeof(LenAbpWpfModule))]
    public sealed class HostModule : AbpModule;
}
