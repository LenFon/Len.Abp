using Len.Abp.Analyzer;
using Xunit;

namespace Len.Abp.Analyzer.Tests;

/// <summary>
/// 覆盖 <see cref="GeneratedCode"/> 的片段拼装。
/// </summary>
public class GeneratedCodeTests
{
    /// <summary>
    /// 空片段与 null 片段被跳过，片段之间留一个空行。
    /// </summary>
    [Fact]
    public void Join_SkipsEmptySections_AndSeparatesWithBlankLine()
    {
        var result = GeneratedCode.Join("a", null, "  ", "b");

        Assert.Equal("a\n\nb\n", result);
    }

    /// <summary>
    /// 全部片段为空时返回空字符串。
    /// </summary>
    [Fact]
    public void Join_AllEmpty_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, GeneratedCode.Join(null, "", "   "));
    }

    /// <summary>
    /// 片段首尾空白被去除。
    /// </summary>
    [Fact]
    public void Join_TrimsSections()
    {
        Assert.Equal("a\n", GeneratedCode.Join("  a  "));
    }

    /// <summary>
    /// null 行被跳过，空字符串行保留为空行。
    /// </summary>
    [Fact]
    public void Lines_SkipsNull_KeepsEmptyLine()
    {
        var result = GeneratedCode.Lines("a", null, string.Empty, "b");

        Assert.Equal("a\n\nb\n", result);
    }

    /// <summary>
    /// 行尾空白被去除。
    /// </summary>
    [Fact]
    public void Lines_TrimsTrailingWhitespace()
    {
        Assert.Equal("a\n", GeneratedCode.Lines("a   "));
    }

    /// <summary>
    /// 单行片段直接接在前缀之后。
    /// </summary>
    [Fact]
    public void Inline_SingleLine_AppendsDirectly()
    {
        Assert.Equal("prefix: value", GeneratedCode.Inline("prefix: ", "value", 4));
    }

    /// <summary>
    /// 多行片段的续行按指定宽度缩进，空行不补缩进。
    /// </summary>
    [Fact]
    public void Inline_MultiLine_IndentsContinuationLines()
    {
        var result = GeneratedCode.Inline("prefix: ", "a\nb\n\nc", 4);

        Assert.Equal("prefix: a\n    b\n\n    c", result);
    }

    /// <summary>
    /// 片段末尾的换行被忽略，不产生多余空行。
    /// </summary>
    [Fact]
    public void Inline_TrailingNewLine_IsTrimmed()
    {
        Assert.Equal("prefix: a\n    b", GeneratedCode.Inline("prefix: ", "a\nb\n", 4));
    }

    /// <summary>
    /// CRLF 与单独的 CR 都按换行处理。
    /// </summary>
    [Fact]
    public void Inline_MixedLineEndings_SplitsCorrectly()
    {
        Assert.Equal("p: a\n  b\n  c", GeneratedCode.Inline("p: ", "a\r\nb\rc", 2));
    }
}
