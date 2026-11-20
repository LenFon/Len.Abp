using Len.Abp.Wpf.Navigation.Regions;
using Xunit;

namespace Len.Abp.Wpf.Tests;

/// <summary>
/// 覆盖 <see cref="RegionProvider"/> 与 <see cref="RegionFinder"/>。
/// </summary>
public class RegionProviderTests
{
    /// <summary>
    /// 添加的区域按添加顺序保存在 Regions 中。
    /// </summary>
    [Fact]
    public void Add_AppendsRegion()
    {
        var provider = new RegionProvider();
        var first = new Region("first", "view1");
        var second = new Region("second", "view2");

        provider.Add(first);
        provider.Add(second);

        Assert.Equal([first, second], provider.Regions);
    }

    /// <summary>
    /// RemoveOwnedRegions 移除由该视图创建的区域，并返回其中的视图。
    /// </summary>
    [Fact]
    public void RemoveOwnedRegions_RemovesRegionsOwnedByView()
    {
        var provider = new RegionProvider();
        var view = new FakeView("view1");
        var other = new FakeView("view2");
        var nested = new FakeView("nested");
        var ownedRegion = new Region("a", "view1", view);
        ownedRegion.Add(nested);
        provider.Add(ownedRegion);
        provider.Add(new Region("b", "view1", view));
        provider.Add(new Region("c", "view2", other));

        var detached = provider.RemoveOwnedRegions(view);

        var remaining = Assert.Single(provider.Regions);
        Assert.Equal("c", remaining.Name);
        Assert.Same(nested, Assert.Single(detached));
    }

    /// <summary>
    /// <see cref="IRegionProvider.Remove"/> 摘除指定区域，不影响其它区域。
    /// </summary>
    /// <remarks>
    /// 元素卸载时用它摘除本轮创建的区域：区域不摘除会永久残留，
    /// 重新加载时再登记回去就会出现同名区域，导航可能命中早已失效的实例。
    /// </remarks>
    [Fact]
    public void Remove_DetachesOnlyTargetRegion()
    {
        var provider = new RegionProvider();
        var target = new Region("a", "view1");
        var other = new Region("b", "view2");
        provider.Add(target);
        provider.Add(other);

        var removed = provider.Remove(target);

        Assert.True(removed);
        Assert.Same(other, Assert.Single(provider.Regions));
    }

    /// <summary>
    /// 摘除不在提供程序中的区域返回 <see langword="false"/>，不抛异常。
    /// </summary>
    [Fact]
    public void Remove_UnknownRegion_ReturnsFalse()
    {
        var provider = new RegionProvider();

        Assert.False(provider.Remove(new Region("a", "view1")));
    }

    /// <summary>
    /// 同名视图的不同实例各自拥有区域时，只拆除目标实例的子树。
    /// </summary>
    [Fact]
    public void RemoveOwnedRegions_SameViewName_OnlyRemovesTargetInstance()
    {
        var provider = new RegionProvider();
        var first = new FakeView("demo");
        var second = new FakeView("demo");
        provider.Add(new Region("a", "demo", first));
        provider.Add(new Region("b", "demo", second));

        provider.RemoveOwnedRegions(first);

        var remaining = Assert.Single(provider.Regions);
        Assert.Same(second, remaining.OwnerView);
    }

    /// <summary>
    /// RemoveOwnedRegions 只处理一层，嵌套深度由调用方按生命周期决定。
    /// </summary>
    [Fact]
    public void RemoveOwnedRegions_DoesNotRecurse()
    {
        var provider = new RegionProvider();
        var root = new FakeView("root");
        var child = new FakeView("child");
        var grandchild = new FakeView("grandchild");

        var rootRegion = new Region("root-region", "root", root);
        var childRegion = new Region("child-region", "child", child);
        var grandchildRegion = new Region("grandchild-region", "grandchild", grandchild);

        rootRegion.Add(child);
        childRegion.Add(grandchild);

        provider.Add(rootRegion);
        provider.Add(childRegion);
        provider.Add(grandchildRegion);

        var detached = provider.RemoveOwnedRegions(root);

        Assert.Same(child, Assert.Single(detached));
        Assert.Equal(2, provider.Regions.Count);
        Assert.Contains(childRegion, provider.Regions);
        Assert.Contains(grandchildRegion, provider.Regions);
    }

    /// <summary>
    /// RemoveOwnedRegions 对不属于任何区域的视图不产生任何影响。
    /// </summary>
    [Fact]
    public void RemoveOwnedRegions_UnknownView_KeepsAllRegions()
    {
        var provider = new RegionProvider();
        provider.Add(new Region("a", "view1", new FakeView("view1")));

        var detached = provider.RemoveOwnedRegions(new FakeView("missing"));

        Assert.Single(provider.Regions);
        Assert.Empty(detached);
    }

    /// <summary>
    /// 区域没有所属视图实例时不会被任何 RemoveOwnedRegions 误删。
    /// </summary>
    [Fact]
    public void RemoveOwnedRegions_RegionWithoutOwnerView_IsKept()
    {
        var provider = new RegionProvider();
        provider.Add(new Region("a", "view1"));

        provider.RemoveOwnedRegions(new FakeView("view1"));

        Assert.Single(provider.Regions);
    }

    /// <summary>
    /// Clear 清空所有区域，并返回其中的视图。
    /// </summary>
    [Fact]
    public void Clear_RemovesAllRegionsAndReturnsViews()
    {
        var provider = new RegionProvider();
        var first = new FakeView("view1");
        var second = new FakeView("view2");
        var regionA = new Region("a", "view1", first);
        var regionB = new Region("b", "view2", second);
        regionA.Add(first);
        regionB.Add(second);
        provider.Add(regionA);
        provider.Add(regionB);

        var views = provider.Clear();

        Assert.Empty(provider.Regions);
        Assert.Equal([first, second], views);
    }

    /// <summary>
    /// 空提供程序上调用 Clear 返回空集合。
    /// </summary>
    [Fact]
    public void Clear_EmptyProvider_ReturnsEmpty()
    {
        var provider = new RegionProvider();

        Assert.Empty(provider.Clear());
        Assert.Empty(provider.Regions);
    }

    /// <summary>
    /// FindRegion 按名称查找区域。
    /// </summary>
    [Fact]
    public void FindRegion_ByName_ReturnsRegion()
    {
        var provider = new RegionProvider();
        var region = new Region("MainRegion", "MainView");
        provider.Add(region);
        var finder = new RegionFinder(provider);

        Assert.Same(region, finder.FindRegion("MainRegion"));
    }

    /// <summary>
    /// FindRegion 对不存在的名称返回 null。
    /// </summary>
    [Fact]
    public void FindRegion_MissingName_ReturnsNull()
    {
        var finder = new RegionFinder(new RegionProvider());

        Assert.Null(finder.FindRegion("missing"));
    }

    /// <summary>
    /// FindRegion 传入 null 时抛出异常。
    /// </summary>
    [Fact]
    public void FindRegion_NullName_Throws()
    {
        var finder = new RegionFinder(new RegionProvider());

        Assert.Throws<ArgumentNullException>(() => finder.FindRegion(null!));
    }

    /// <summary>
    /// 同名区域有多个时，优先返回所属视图处于活动链上的那个。
    /// </summary>
    /// <remarks>
    /// 嵌套视图的多个实例各自拥有同名子区域，用户看到的是活动链上那个，导航必须命中它。
    /// </remarks>
    [Fact]
    public void FindRegion_DuplicateName_PrefersVisibleRegion()
    {
        var provider = new RegionProvider();
        var hiddenView = new FakeView("demo");
        var visibleView = new FakeView("demo");

        var hiddenRegion = new Region("Nested", "demo", hiddenView);
        var visibleRegion = new Region("Nested", "demo", visibleView);

        // 两个同名视图都在根区域里，但只有 visibleView 处于活动状态。
        var rootRegion = new Region("MainRegion", "MainView");
        rootRegion.Add(hiddenView);
        rootRegion.Add(visibleView);
        rootRegion.Activate(visibleView);

        provider.Add(hiddenRegion);
        provider.Add(visibleRegion);
        provider.Add(rootRegion);

        var finder = new RegionFinder(provider);

        Assert.Same(visibleRegion, finder.FindRegion("Nested"));
    }

    /// <summary>
    /// 同名区域都不可见时回退到第一个，保证查找结果稳定。
    /// </summary>
    [Fact]
    public void FindRegion_DuplicateName_NoneVisible_ReturnsFirst()
    {
        var provider = new RegionProvider();
        var first = new Region("Nested", "demo", new FakeView("demo"));
        var second = new Region("Nested", "demo", new FakeView("demo"));
        provider.Add(first);
        provider.Add(second);

        var finder = new RegionFinder(provider);

        Assert.Same(first, finder.FindRegion("Nested"));
    }

    /// <summary>
    /// 所属视图不在任何区域中时视为根视图，其区域可见。
    /// </summary>
    [Fact]
    public void FindRegion_OwnerViewNotInAnyRegion_IsVisible()
    {
        var provider = new RegionProvider();
        var shellView = new FakeView("main");
        var shellRegion = new Region("ShellRegion", "main", shellView);

        // 竞争区域的所属视图在根区域中但未激活，因此不可见。
        var hiddenView = new FakeView("main");
        var hiddenRegion = new Region("ShellRegion", "main", hiddenView);
        var rootRegion = new Region("MainRegion", "MainView");
        rootRegion.Add(hiddenView);

        provider.Add(hiddenRegion);
        provider.Add(shellRegion);
        provider.Add(rootRegion);

        var finder = new RegionFinder(provider);

        Assert.Same(shellRegion, finder.FindRegion("ShellRegion"));
    }
}
