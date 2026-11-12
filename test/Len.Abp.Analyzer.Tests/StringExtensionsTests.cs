using Len.Abp.Analyzer;
using Xunit;

namespace Len.Abp.Analyzer.Tests;

/// <summary>
/// 覆盖 <see cref="StringExtensions"/> 的转义与换行处理。
/// </summary>
public class StringExtensionsTests
{
    /// <summary>
    /// 空字符串原样返回。
    /// </summary>
    [Fact]
    public void ToCSharpStringLiteral_Empty_ReturnsSame()
    {
        Assert.Equal(string.Empty, string.Empty.ToCSharpStringLiteral());
        Assert.Null(((string?)null)!.ToCSharpStringLiteral());
    }

    /// <summary>
    /// 反斜杠、引号、回车、换行、制表符与空字符被转义。
    /// </summary>
    [Fact]
    public void ToCSharpStringLiteral_SpecialCharacters_AreEscaped()
    {
        var text = "a\\b\"c\rd\ne\tf\0g";

        Assert.Equal("a\\\\b\\\"c\\rd\\ne\\tf\\0g", text.ToCSharpStringLiteral());
    }

    /// <summary>
    /// 普通字符保持不变。
    /// </summary>
    [Fact]
    public void ToCSharpStringLiteral_PlainText_IsUnchanged()
    {
        Assert.Equal("abc中文", "abc中文".ToCSharpStringLiteral());
    }

    /// <summary>
    /// XML 注释中的尖括号与和号被转义。
    /// </summary>
    [Fact]
    public void ToXmlCommentText_EscapesMarkup()
    {
        Assert.Equal("a &amp; b &lt;c&gt;", "a & b <c>".ToXmlCommentText());
    }

    /// <summary>
    /// 目标换行符为 null 时原样返回。
    /// </summary>
    [Fact]
    public void NormalizeLineEndings_NullNewLine_ReturnsSame()
    {
        Assert.Equal("a\r\nb", "a\r\nb".NormalizeLineEndings());
    }

    /// <summary>
    /// 不含换行符的文本原样返回。
    /// </summary>
    [Fact]
    public void NormalizeLineEndings_NoLineBreak_ReturnsSame()
    {
        Assert.Equal("abc", "abc".NormalizeLineEndings("\n"));
    }

    /// <summary>
    /// CRLF、单独的 CR 与单独的 LF 都归一化为目标换行符。
    /// </summary>
    [Theory]
    [InlineData("a\r\nb\rc\nd", "a\nb\nc\nd")]
    [InlineData("a\r\nb", "a|b")]
    [InlineData("\r\n\r\n", "||")]
    public void NormalizeLineEndings_MixedLineBreaks_Normalizes(string input, string expected)
    {
        var newLine = expected.Contains('|') ? "|" : "\n";

        Assert.Equal(expected, input.NormalizeLineEndings(newLine));
    }

    /// <summary>
    /// 多字符换行符按完整长度写入。
    /// </summary>
    [Fact]
    public void NormalizeLineEndings_MultiCharacterNewLine_WritesWholeSequence()
    {
        Assert.Equal("a<NL>b<NL>c", "a\r\nb\nc".NormalizeLineEndings("<NL>"));
    }

    /// <summary>
    /// 空字符串原样返回。
    /// </summary>
    [Fact]
    public void RemoveBlankLines_Empty_ReturnsSame()
    {
        Assert.Equal(string.Empty, string.Empty.RemoveBlankLines());
    }

    /// <summary>
    /// 空白行被移除，换行符统一为 \n。
    /// </summary>
    [Fact]
    public void RemoveBlankLines_RemovesBlankLines()
    {
        var text = "a\r\n\r\n   \nb\n\t\nc";

        Assert.Equal("a\nb\nc", text.RemoveBlankLines());
    }

    /// <summary>
    /// 全部为空白行时返回空字符串。
    /// </summary>
    [Fact]
    public void RemoveBlankLines_AllBlank_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, " \r\n\t\n  ".RemoveBlankLines());
    }

    /// <summary>
    /// 没有空白行时只做换行符归一化。
    /// </summary>
    [Fact]
    public void RemoveBlankLines_NoBlankLines_OnlyNormalizes()
    {
        Assert.Equal("a\nb", "a\r\nb".RemoveBlankLines());
    }
}
