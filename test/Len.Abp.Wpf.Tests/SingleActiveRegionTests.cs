using Len.Abp.Wpf.Navigation.Regions;
using Xunit;

namespace Len.Abp.Wpf.Tests;

/// <summary>
/// 覆盖 <see cref="SingleActiveRegion"/> 的互斥激活语义。
/// </summary>
public class SingleActiveRegionTests
{
    /// <summary>
    /// 激活新视图时自动失活之前的活动视图。
    /// </summary>
    [Fact]
    public void Activate_SecondView_DeactivatesFirst()
    {
        var region = new SingleActiveRegion("MainRegion", "MainView");
        var first = new FakeView("first");
        var second = new FakeView("second");
        region.Add(first);
        region.Add(second);

        region.Activate(first);
        region.Activate(second);

        Assert.Equal(1, region.ActiveViews.Count);
        Assert.True(region.ActiveViews.Contains(second));
        Assert.False(region.ActiveViews.Contains(first));
    }

    /// <summary>
    /// 重复激活同一视图不会触发失活。
    /// </summary>
    [Fact]
    public void Activate_SameViewTwice_KeepsItActive()
    {
        var region = new SingleActiveRegion("MainRegion", "MainView");
        var view = new FakeView("demo");
        region.Add(view);

        region.Activate(view);
        region.Activate(view);

        Assert.Equal(1, region.ActiveViews.Count);
        Assert.True(region.ActiveViews.Contains(view));
    }

    /// <summary>
    /// 区域中没有任何活动视图时激活视图不会抛异常。
    /// </summary>
    [Fact]
    public void Activate_WhenNoActiveView_ActivatesDirectly()
    {
        var region = new SingleActiveRegion("MainRegion", "MainView");
        var view = new FakeView("demo");
        region.Add(view);

        region.Activate(view);

        Assert.True(region.ActiveViews.Contains(view));
    }

    /// <summary>
    /// 激活不属于本区域的视图时抛出异常，且不影响当前活动视图。
    /// </summary>
    [Fact]
    public void Activate_UnknownView_ThrowsAndKeepsCurrentActiveView()
    {
        var region = new SingleActiveRegion("MainRegion", "MainView");
        var active = new FakeView("active");
        region.Add(active);
        region.Activate(active);

        Assert.Throws<ArgumentException>(() => region.Activate(new FakeView("unknown")));

        Assert.Same(active, Assert.Single(region.ActiveViews));
    }
}
