using Len.Abp.Wpf.Navigation.Regions;
using Len.Abp.Wpf.Navigation.Regions.Behaviors;
using Xunit;


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
        var region = new Region("MainRegion", "MainView");
        var behavior = new RegionProviderRegistrationBehavior(provider) { Region = region };

        behavior.Attach();

        Assert.Same(region, Assert.Single(provider.Regions));
    }
}
