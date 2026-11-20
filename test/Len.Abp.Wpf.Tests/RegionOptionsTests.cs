using Len.Abp.Wpf.Navigation.Regions;
using Xunit;

namespace Len.Abp.Wpf.Tests;

/// <summary>
/// 覆盖 <see cref="RegionOptions"/> 的行为键管理。
/// </summary>
public class RegionOptionsTests
{
    /// <summary>
    /// 添加的行为键按添加顺序返回。
    /// </summary>
    [Fact]
    public void AddBehaviorKey_AppendsKey()
    {
        var options = new RegionOptions();

        options.AddBehaviorKey("first");
        options.AddBehaviorKey("second");

        Assert.Equal(["first", "second"], options.BehaviorKeys);
    }

    /// <summary>
    /// 重复添加同一行为键抛出异常。
    /// </summary>
    [Fact]
    public void AddBehaviorKey_Duplicate_Throws()
    {
        var options = new RegionOptions();
        options.AddBehaviorKey("test");

        var ex = Assert.Throws<InvalidOperationException>(() => options.AddBehaviorKey("test"));

        Assert.Contains("已存在", ex.Message);
    }

    /// <summary>
    /// 空键抛出异常。
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void AddBehaviorKey_EmptyKey_Throws(string? key)
    {
        var options = new RegionOptions();

        // null 会抛出 ArgumentNullException，空字符串抛出 ArgumentException，两者都是参数错误。
        Assert.ThrowsAny<ArgumentException>(() => options.AddBehaviorKey(key!));
    }

    /// <summary>
    /// 新建时行为键集合为空。
    /// </summary>
    [Fact]
    public void NewOptions_HasNoBehaviorKeys()
    {
        Assert.Empty(new RegionOptions().BehaviorKeys);
    }

    /// <summary>
    /// 只有空白字符的键是合法键，不做裁剪或拒绝。
    /// </summary>
    /// <remarks>
    /// 键来自 <c>[ExposeKeyedService]</c> 的名称，空白键属于配置错误但不应在
    /// 启动期抛异常；这里锁定"按原样保存"的行为。
    /// </remarks>
    [Fact]
    public void AddBehaviorKey_WhitespaceKey_IsAcceptedAsIs()
    {
        var options = new RegionOptions();

        options.AddBehaviorKey(" ");

        Assert.Equal([" "], options.BehaviorKeys);
    }

    /// <summary>
    /// 键名区分大小写，大小写不同视为不同键。
    /// </summary>
    [Fact]
    public void AddBehaviorKey_IsCaseSensitive()
    {
        var options = new RegionOptions();

        options.AddBehaviorKey("Behavior");
        options.AddBehaviorKey("behavior");

        Assert.Equal(2, options.BehaviorKeys.Count());
    }

    /// <summary>
    /// 大量行为键全部保留且顺序不变。
    /// </summary>
    [Fact]
    public void AddBehaviorKey_ManyKeys_PreservesOrder()
    {
        var options = new RegionOptions();

        for (var i = 0; i < 500; i++)
        {
            options.AddBehaviorKey($"key{i}");
        }

        Assert.Equal(500, options.BehaviorKeys.Count());
        Assert.Equal("key0", options.BehaviorKeys.First());
        Assert.Equal("key499", options.BehaviorKeys.Last());
    }
}
