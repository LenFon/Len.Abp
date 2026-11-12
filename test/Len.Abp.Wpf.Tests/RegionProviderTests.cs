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
    /// RemoveByViewName 移除所有属于该视图的区域。
    /// </summary>
    [Fact]
    public void RemoveByViewName_RemovesMatchingRegions()
    {
        var provider = new RegionProvider();
        provider.Add(new Region("a", "view1"));
        provider.Add(new Region("b", "view1"));
        provider.Add(new Region("c", "view2"));

        provider.RemoveByViewName("view1");

        var remaining = Assert.Single(provider.Regions);
        Assert.Equal("c", remaining.Name);
    }

    /// <summary>
    /// RemoveByViewName 对不存在的视图名称不产生任何影响。
    /// </summary>
    [Fact]
    public void RemoveByViewName_UnknownView_KeepsAllRegions()
    {
        var provider = new RegionProvider();
        provider.Add(new Region("a", "view1"));

        provider.RemoveByViewName("missing");

        Assert.Single(provider.Regions);
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
}
