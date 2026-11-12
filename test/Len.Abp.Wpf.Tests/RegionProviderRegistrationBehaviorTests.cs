using Len.Abp.Wpf.Navigation.Regions;
using Len.Abp.Wpf.Navigation.Regions.Behaviors;
using Xunit;

// 附加属性 Len.Abp.Wpf.Navigation.Region 与区域实现 Len.Abp.Wpf.Navigation.Regions.Region 同名。
using RegionImpl = Len.Abp.Wpf.Navigation.Regions.Region;

namespace Len.Abp.Wpf.Tests;

/// <summary>
/// 覆盖 <see cref="RegionProviderRegistrationBehavior"/>。
/// </summary>
public class RegionProviderRegistrationBehaviorTests
{
    /// <summary>
    /// 附加行为时把区域登记到区域提供程序。
    /// </summary>
    [Fact]
    public void Attach_RegistersRegion()
    {
        var provider = new RegionProvider();
        var region = new RegionImpl("MainRegion", "MainView");
        var behavior = new RegionProviderRegistrationBehavior(provider) { Region = region };

        behavior.Attach();

        Assert.Same(region, Assert.Single(provider.Regions));
    }
}
