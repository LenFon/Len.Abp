using Len.Abp.Analyzer;
using Xunit;

namespace Len.Abp.Analyzer.Tests;

/// <summary>
/// 覆盖 <see cref="FileWithName"/> 的空值判断。
/// </summary>
public class FileWithNameTests
{
    /// <summary>
    /// 名称为空时视为空文件。
    /// </summary>
    [Fact]
    public void IsEmpty_EmptyName_ReturnsTrue()
    {
        Assert.True(new FileWithName(string.Empty, "text").IsEmpty);
    }

    /// <summary>
    /// 内容为空时视为空文件。
    /// </summary>
    [Fact]
    public void IsEmpty_EmptyText_ReturnsTrue()
    {
        Assert.True(new FileWithName("a.g.cs", string.Empty).IsEmpty);
    }

    /// <summary>
    /// 名称与内容都非空时不是空文件。
    /// </summary>
    [Fact]
    public void IsEmpty_BothPresent_ReturnsFalse()
    {
        Assert.False(new FileWithName("a.g.cs", "text").IsEmpty);
    }

    /// <summary>
    /// 静态空实例的名称与内容均为空字符串。
    /// </summary>
    [Fact]
    public void Empty_HasEmptyNameAndText()
    {
        var empty = FileWithName.Empty;

        Assert.Equal(string.Empty, empty.Name);
        Assert.Equal(string.Empty, empty.Text);
        Assert.True(empty.IsEmpty);
    }
}
