using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Len.Abp.Analyzer.Tests;

/// <summary>
/// 覆盖 <see cref="Microsoft.CodeAnalysis.AttributeDataExtensions"/> 的特性参数读取。
/// </summary>
public class AttributeDataExtensionsTests
{
    /// <summary>
    /// 读取泛型特性指定位置的类型参数。
    /// </summary>
    [Fact]
    public void GetGenericTypeArgument_ReturnsTypeAtPosition()
    {
        var attribute = GetAttribute("[Sample<int, string>]");

        Assert.Equal("int", attribute.GetGenericTypeArgument(0)!.ToDisplayString());
        Assert.Equal("string", attribute.GetGenericTypeArgument(1)!.ToDisplayString());
    }

    /// <summary>
    /// 位置越界时返回 null。
    /// </summary>
    [Fact]
    public void GetGenericTypeArgument_OutOfRange_ReturnsNull()
    {
        var attribute = GetAttribute("[Sample<int, string>]");

        Assert.Null(attribute.GetGenericTypeArgument(5));
    }

    /// <summary>
    /// 特性数据为 null 时抛出异常。
    /// </summary>
    [Fact]
    public void GetGenericTypeArgument_NullAttribute_Throws()
    {
        var exception = Assert.Throws<ArgumentNullException>(
            () => ((AttributeData)null!).GetGenericTypeArgument(0)
        );

        Assert.Equal("attributeData", exception.ParamName);
    }

    /// <summary>
    /// 读取命名参数的值。
    /// </summary>
    [Fact]
    public void GetNamedArgument_ReturnsValue()
    {
        var attribute = GetAttribute("[Sample<int, string>(Name = \"abc\", Count = 3)]");

        Assert.Equal("abc", attribute.GetNamedArgument("Name").Value);
        Assert.Equal(3, attribute.GetNamedArgument("Count").Value);
    }

    /// <summary>
    /// 命名参数不存在时返回默认值。
    /// </summary>
    [Fact]
    public void GetNamedArgument_Missing_ReturnsDefault()
    {
        var attribute = GetAttribute("[Sample<int, string>]");

        Assert.Null(attribute.GetNamedArgument("Name").Value);
    }

    /// <summary>
    /// 特性数据为 null 时抛出异常。
    /// </summary>
    [Fact]
    public void GetNamedArgument_NullAttribute_Throws()
    {
        var exception = Assert.Throws<ArgumentNullException>(
            () => ((AttributeData)null!).GetNamedArgument("Name")
        );

        Assert.Equal("attributeData", exception.ParamName);
    }

    /// <summary>
    /// 从特性语法读取命名参数的表达式文本。
    /// </summary>
    [Fact]
    public void GetNamedArgumentExpression_ReturnsExpressionText()
    {
        var syntax = GetAttributeSyntax("[Sample<int, string>(Name = \"abc\")]");

        Assert.Equal("\"abc\"", syntax.GetNamedArgumentExpression("Name"));
    }

    /// <summary>
    /// 命名参数不存在时返回 null。
    /// </summary>
    [Fact]
    public void GetNamedArgumentExpression_Missing_ReturnsNull()
    {
        var syntax = GetAttributeSyntax("[Sample<int, string>]");

        Assert.Null(syntax.GetNamedArgumentExpression("Name"));
    }

    /// <summary>
    /// 特性语法为 null 时抛出异常。
    /// </summary>
    [Fact]
    public void GetNamedArgumentExpression_NullSyntax_Throws()
    {
        var exception = Assert.Throws<ArgumentNullException>(
            () => ((AttributeSyntax)null!).GetNamedArgumentExpression("Name")
        );

        Assert.Equal("attributeSyntax", exception.ParamName);
    }

    /// <summary>
    /// 从特性数据取回对应的语法节点。
    /// </summary>
    [Fact]
    public void GetSyntax_ReturnsAttributeSyntax()
    {
        var attribute = GetAttribute("[Sample<int, string>(Name = \"abc\")]");

        Assert.NotNull(attribute.GetSyntax());
    }

    private static AttributeData GetAttribute(string attributeText)
    {
        var compilation = Compile(attributeText);

        return compilation.GetTypeByMetadataName("Holder")!.GetAttributes()[0];
    }

    private static AttributeSyntax GetAttributeSyntax(string attributeText)
    {
        var compilation = Compile(attributeText);

        return compilation
            .GetTypeByMetadataName("Holder")!
            .GetAttributes()[0]
            .GetSyntax()!;
    }

    private static CSharpCompilation Compile(string attributeText)
    {
        var source = $$"""
            using System;

            public class SampleAttribute<T1, T2> : Attribute
            {
                public string? Name { get; set; }
                public int Count { get; set; }
            }

            {{attributeText}}
            public class Holder { }
            """;

        return CSharpCompilation.Create(
            "AttributeDataTests",
            syntaxTrees: [CSharpSyntaxTree.ParseText(source)],
            references: TestReferences.Framework.Value
        );
    }
}
