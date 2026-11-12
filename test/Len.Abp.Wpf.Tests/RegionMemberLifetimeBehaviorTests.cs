using Len.Abp.Wpf.Navigation.Regions;
using Len.Abp.Wpf.Navigation.Regions.Behaviors;
using Xunit;

// 附加属性 Len.Abp.Wpf.Navigation.Region 与区域实现 Len.Abp.Wpf.Navigation.Regions.Region 同名。
using RegionImpl = Len.Abp.Wpf.Navigation.Regions.Region;

namespace Len.Abp.Wpf.Tests;

/// <summary>
/// 覆盖 <see cref="RegionMemberLifetimeBehavior"/> 的视图保活策略。
/// </summary>
public class RegionMemberLifetimeBehaviorTests
{
    /// <summary>
    /// 视图模型声明 KeepAlive 为 false 时，失活会从区域中移除该视图，
    /// 并按视图名把该视图所属的区域从提供程序中移除。
    /// </summary>
    [Fact]
    public void Deactivate_KeepAliveFalse_RemovesView()
    {
        var provider = new RegionProvider();
        // 区域所有者与视图名一致，才能验证 RemoveByViewName 的联动。
        var region = new RegionImpl("MainRegion", "demo");
        provider.Add(region);

        var view = new FakeView("demo", new LifetimeViewModel(keepAlive: false));
        region.Add(view);
        region.Activate(view);

        region.Behaviors.Add(nameof(RegionMemberLifetimeBehavior), new RegionMemberLifetimeBehavior(provider));
        region.Deactivate(view);

        Assert.Empty(region.Views);
        Assert.Empty(provider.Regions);
    }

    /// <summary>
    /// 视图模型声明 KeepAlive 为 true 时，失活保留视图。
    /// </summary>
    [Fact]
    public void Deactivate_KeepAliveTrue_KeepsView()
    {
        var provider = new RegionProvider();
        var region = new RegionImpl("MainRegion", "MainView");
        provider.Add(region);

        var view = new FakeView("demo", new LifetimeViewModel(keepAlive: true));
        region.Add(view);
        region.Activate(view);

        region.Behaviors.Add(nameof(RegionMemberLifetimeBehavior), new RegionMemberLifetimeBehavior(provider));
        region.Deactivate(view);

        Assert.Single(region.Views);
        Assert.Single(provider.Regions);
    }

    /// <summary>
    /// 视图模型未实现 IRegionMemberLifetime 时默认保活。
    /// </summary>
    [Fact]
    public void Deactivate_NoLifetimeContract_KeepsView()
    {
        var provider = new RegionProvider();
        var region = new RegionImpl("MainRegion", "MainView");
        provider.Add(region);

        var view = new FakeView("demo", new object());
        region.Add(view);
        region.Activate(view);

        region.Behaviors.Add(nameof(RegionMemberLifetimeBehavior), new RegionMemberLifetimeBehavior(provider));
        region.Deactivate(view);

        Assert.Single(region.Views);
    }

    /// <summary>
    /// 添加视图（非移除动作）不会触发保活逻辑。
    /// </summary>
    [Fact]
    public void AddView_DoesNotRemoveAnything()
    {
        var provider = new RegionProvider();
        var region = new RegionImpl("MainRegion", "MainView");
        provider.Add(region);
        region.Behaviors.Add(nameof(RegionMemberLifetimeBehavior), new RegionMemberLifetimeBehavior(provider));

        region.Add(new FakeView("demo", new LifetimeViewModel(keepAlive: false)));

        Assert.Single(region.Views);
    }

    private sealed class LifetimeViewModel(bool keepAlive) : IRegionMemberLifetime
    {
        public bool KeepAlive { get; } = keepAlive;
    }
}
