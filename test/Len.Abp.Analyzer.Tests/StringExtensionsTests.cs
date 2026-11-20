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

}
