using Len.Abp.Wpf.Navigation.Regions;
using Xunit;

namespace Len.Abp.Wpf.Tests;

/// <summary>
/// 覆盖 <see cref="RegionBehaviorCollection"/> 的添加与附加语义。
/// </summary>
public class RegionBehaviorCollectionTests
{
    /// <summary>
    /// 添加行为时把区域赋给行为并调用 Attach。
    /// </summary>
    [Fact]
    public void Add_AssignsRegionAndAttaches()
    {
        var region = new Region("MainRegion", "MainView");
        var behavior = new RecordingBehavior();

        region.Behaviors.Add("test", behavior);

        Assert.Same(region, behavior.Region);
        Assert.True(behavior.Attached);
    }

    /// <summary>
    /// 添加后可通过索引器与 ContainsKey 访问。
    /// </summary>
    [Fact]
    public void Add_IsAccessibleByKey()
    {
        var region = new Region("MainRegion", "MainView");
        var behavior = new RecordingBehavior();
        region.Behaviors.Add("test", behavior);

        Assert.Same(behavior, region.Behaviors["test"]);
        Assert.True(region.Behaviors.ContainsKey("test"));
        Assert.False(region.Behaviors.ContainsKey("missing"));
    }

    /// <summary>
    /// 使用相同键重复添加行为抛出异常。
    /// </summary>
    [Fact]
    public void Add_DuplicateKey_Throws()
    {
        var region = new Region("MainRegion", "MainView");
        region.Behaviors.Add("test", new RecordingBehavior());

        var ex = Assert.Throws<ArgumentException>(() => region.Behaviors.Add("test", new RecordingBehavior()));

        Assert.Equal("key", ex.ParamName);
    }

    /// <summary>
    /// 键为 null 时抛出异常。
    /// </summary>
    [Fact]
    public void Add_NullKey_Throws()
    {
        var region = new Region("MainRegion", "MainView");

        Assert.Throws<ArgumentNullException>(() => region.Behaviors.Add(null!, new RecordingBehavior()));
    }

    /// <summary>
    /// 行为为 null 时抛出异常。
    /// </summary>
    [Fact]
    public void Add_NullBehavior_Throws()
    {
        var region = new Region("MainRegion", "MainView");

        Assert.Throws<ArgumentNullException>(() => region.Behaviors.Add("test", null!));
    }

    /// <summary>
    /// 枚举行为集合时返回已添加的键值对。
    /// </summary>
    [Fact]
    public void Enumerate_ReturnsAddedBehaviors()
    {
        var region = new Region("MainRegion", "MainView");
        var behavior = new RecordingBehavior();
        region.Behaviors.Add("test", behavior);

        var pair = Assert.Single(region.Behaviors);
        Assert.Equal("test", pair.Key);
        Assert.Same(behavior, pair.Value);
    }

    /// <summary>
    /// 非泛型枚举与泛型枚举返回相同的条目。
    /// </summary>
    [Fact]
    public void NonGenericEnumerate_ReturnsSameEntries()
    {
        var region = new Region("MainRegion", "MainView");
        region.Behaviors.Add("test", new RecordingBehavior());

        var count = 0;
        foreach (var _ in (System.Collections.IEnumerable)region.Behaviors)
        {
            count++;
        }

        Assert.Equal(1, count);
    }

    private sealed class RecordingBehavior : IRegionBehavior
    {
        public IRegion Region { get; set; } = default!;

        public bool Attached { get; private set; }

        public void Attach() => Attached = true;
    }
}
