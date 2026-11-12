using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Len.Abp.Analyzer.Tests;

/// <summary>
/// 覆盖 <see cref="Microsoft.CodeAnalysis.TypeSymbolExtensions"/> 的特殊类型判断。
/// </summary>
public class TypeSymbolExtensionsTests
{
    /// <summary>
    /// 符号为 null 时返回 null。
    /// </summary>
    [Fact]
    public void IsSpecialType_Null_ReturnsNull()
    {
        Assert.Null(((ITypeSymbol?)null).IsSpecialType());
    }

    /// <summary>
    /// 数组类型视为特殊类型。
    /// </summary>
    [Fact]
    public void IsSpecialType_Array_ReturnsTrue()
    {
        Assert.True(GetType("int[]").IsSpecialType());
    }

    /// <summary>
    /// 内置类型视为特殊类型。
    /// </summary>
    [Theory]
    [InlineData("int")]
    [InlineData("string")]
    [InlineData("bool")]
    public void IsSpecialType_BuiltIn_ReturnsTrue(string typeName)
    {
        Assert.True(GetType(typeName).IsSpecialType());
    }

    /// <summary>
    /// 基础类型为内置类型的可空类型视为特殊类型。
    /// </summary>
    [Fact]
    public void IsSpecialType_NullableOfBuiltIn_ReturnsTrue()
    {
        Assert.True(GetType("int?").IsSpecialType());
    }

    /// <summary>
    /// 自定义类型不是特殊类型。
    /// </summary>
    [Fact]
    public void IsSpecialType_CustomType_ReturnsFalse()
    {
        Assert.False(GetType("Sample").IsSpecialType());
    }

    /// <summary>
    /// 基础类型为自定义类型的可空类型不是特殊类型。
    /// </summary>
    [Fact]
    public void IsSpecialType_NullableOfCustomType_ReturnsFalse()
    {
        Assert.False(GetType("Sample?").IsSpecialType());
    }

    private static ITypeSymbol GetType(string typeName)
    {
        var source = $$"""
            public class Sample { }
            public class Holder { public {{typeName}} Value { get; set; } = default!; }
            """;

        var compilation = CSharpCompilation.Create(
            "TypeSymbolTests",
            syntaxTrees: [CSharpSyntaxTree.ParseText(source)],
            references: TestReferences.Framework.Value
        );

        var holder = compilation.GetTypeByMetadataName("Holder")!;
        return holder.GetMembers("Value").OfType<IPropertySymbol>().Single().Type;
    }
}
