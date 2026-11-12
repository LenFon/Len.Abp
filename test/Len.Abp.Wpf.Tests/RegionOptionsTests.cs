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
}
