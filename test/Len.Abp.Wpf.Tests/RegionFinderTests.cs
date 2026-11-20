using Len.Abp.Wpf.Mvvm;
using Len.Abp.Wpf.Navigation.Regions;
using Xunit;

namespace Len.Abp.Wpf.Tests;

/// <summary>
/// 覆盖 <see cref="RegionFinder"/> 在重名区域之间的可见性判定。
/// </summary>
/// <remarks>
/// 嵌套区域允许重名：同一视图的多个实例各自拥有同名子区域。
/// 查找时必须返回当前可见的那个，否则导航会命中已被切走的旧实例。
/// </remarks>
public class RegionFinderTests
{
    /// <summary>
    /// 只有一个同名区域时直接返回它。
    /// </summary>
    [Fact]
    public void FindRegion_SingleCandidate_ReturnsIt()
    {
        var provider = new RegionProvider();
        var region = new Region("MainRegion", "MainView");
        provider.Add(region);

        Assert.Same(region, new RegionFinder(provider).FindRegion("MainRegion"));
    }

    /// <summary>
    /// 没有同名区域时返回 null。
    /// </summary>
    [Fact]
    public void FindRegion_NoCandidate_ReturnsNull()
    {
        Assert.Null(new RegionFinder(new RegionProvider()).FindRegion("Missing"));
    }

    /// <summary>
    /// 区域名称为 null 时抛出异常。
    /// </summary>
    [Fact]
    public void FindRegion_NullName_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => new RegionFinder(new RegionProvider()).FindRegion(null!)
        );
    }

    /// <summary>
    /// 重名区域中优先返回所属视图处于活动链上的那个。
    /// </summary>
    [Fact]
    public void FindRegion_DuplicateNames_PrefersVisibleRegion()
    {
        var provider = new RegionProvider();
        var activeView = new FakeView("active");
        var inactiveView = new FakeView("inactive");

        var parent = new Region("ParentRegion", "Shell");
        parent.Add(activeView);
        parent.Add(inactiveView);
        parent.Activate(activeView);

        var visibleChild = new Region("ChildRegion", activeView.ViewName, activeView);
        var hiddenChild = new Region("ChildRegion", inactiveView.ViewName, inactiveView);

        provider.Add(parent);
        provider.Add(hiddenChild);
        provider.Add(visibleChild);

        Assert.Same(visibleChild, new RegionFinder(provider).FindRegion("ChildRegion"));
    }

    /// <summary>
    /// 所属视图不在任何区域中时视为根视图，其区域始终可见。
    /// </summary>
    [Fact]
    public void FindRegion_OwnerViewNotInAnyRegion_IsVisible()
    {
        var provider = new RegionProvider();
        var hiddenOwner = new FakeView("hidden");
        var parent = new Region("ParentRegion", "Shell");
        parent.Add(hiddenOwner);

        var hiddenRegion = new Region("ChildRegion", hiddenOwner.ViewName, hiddenOwner);
        var rootView = new FakeView("root");
        var rootRegion = new Region("ChildRegion", rootView.ViewName, rootView);

        provider.Add(parent);
        provider.Add(hiddenRegion);
        provider.Add(rootRegion);

        Assert.Same(rootRegion, new RegionFinder(provider).FindRegion("ChildRegion"));
    }

    /// <summary>
    /// 区域图存在环时不会无限递归。
    /// </summary>
    /// <remarks>
    /// 自引用区域（视图拥有包含它自己的区域）在错误配置下可能出现。
    /// 可见性判定用递归路径阻断环，因此该区域被判为不可见，
    /// 查找继续使用其他候选而不是栈溢出。
    /// </remarks>
    [Fact]
    public void FindRegion_CyclicOwnership_DoesNotRecurseForever()
    {
        var provider = new RegionProvider();
        var view = new FakeView("cyclic");
        var cyclicRegion = new Region("ChildRegion", view.ViewName, view);
        cyclicRegion.Add(view);
        cyclicRegion.Activate(view);

        var other = new Region("ChildRegion", "other");
        provider.Add(cyclicRegion);
        provider.Add(other);

        Assert.Same(other, new RegionFinder(provider).FindRegion("ChildRegion"));
    }

    /// <summary>
    /// 所有候选都不可见时回退到第一个注册的候选，而不是返回 null。
    /// </summary>
    [Fact]
    public void FindRegion_AllCandidatesHidden_FallsBackToFirst()
    {
        var provider = new RegionProvider();
        var view = new FakeView("inactive");
        var parent = new Region("ParentRegion", "Shell");
        parent.Add(view);

        var first = new Region("ChildRegion", view.ViewName, view);
        var second = new Region("ChildRegion", view.ViewName, view);

        provider.Add(parent);
        provider.Add(first);
        provider.Add(second);

        Assert.Same(first, new RegionFinder(provider).FindRegion("ChildRegion"));
    }
}
