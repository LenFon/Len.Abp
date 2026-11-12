using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Len.Abp.Analyzer.Tests;

/// <summary>
/// 覆盖 <see cref="Microsoft.CodeAnalysis.TypedConstantExtensions"/> 的常量转换。
/// </summary>
/// <remarks>
/// <see cref="TypedConstant"/> 没有公开构造函数，因此这里从真实源码中的特性参数取值。
/// </remarks>
public class TypedConstantExtensionsTests
{
    /// <summary>
    /// 值为 null 时返回默认值。
    /// </summary>
    [Fact]
    public void To_NullValue_ReturnsDefault()
    {
        var constant = GetNamedArgument("Missing");

        Assert.Equal(0, constant.To<int>());
        Assert.Equal(42, constant.To(42));
    }

    /// <summary>
    /// 数值常量转换为目标数值类型。
    /// </summary>
    [Fact]
    public void To_NumericValue_Converts()
    {
        var constant = GetNamedArgument("Number");

        Assert.Equal(7L, constant.To<long>());
        Assert.Equal(7.0, constant.To<double>());
    }

    /// <summary>
    /// 字符串常量转换为 Guid。
    /// </summary>
    [Fact]
    public void To_GuidString_Converts()
    {
        var constant = GetNamedArgument("GuidText");

        Assert.Equal(Guid.Parse(ConstantHolder.GuidValue), constant.To<Guid>());
    }

    /// <summary>
    /// 可空转换在值为 null 时返回 null。
    /// </summary>
    [Fact]
    public void ToNullable_NullValue_ReturnsNull()
    {
        var constant = GetNamedArgument("NullValue");

        Assert.Null(constant.ToNullable<int>());
    }

    /// <summary>
    /// 可空转换在值存在时返回转换结果。
    /// </summary>
    [Fact]
    public void ToNullable_Value_Converts()
    {
        var constant = GetNamedArgument("Number");

        Assert.Equal(7, constant.ToNullable<int>());
    }

    /// <summary>
    /// 可空转换支持 Guid。
    /// </summary>
    [Fact]
    public void ToNullable_GuidString_Converts()
    {
        var constant = GetNamedArgument("GuidText");

        Assert.Equal(Guid.Parse(ConstantHolder.GuidValue), constant.ToNullable<Guid>());
    }

    private static TypedConstant GetNamedArgument(string name)
    {
        var source = $$"""
            [global::Len.Abp.Analyzer.Tests.TypedConstantExtensionsTests.ConstantHolderAttribute({{ConstantHolder.Number}}, "{{ConstantHolder.GuidValue}}")]
            public class Sample;
            """;

        var compilation = CSharpCompilation.Create(
            "TypedConstantTests",
            syntaxTrees: [CSharpSyntaxTree.ParseText(source)],
            references: TestReferences.Framework.Value
        );

        var attribute = compilation.GetTypeByMetadataName("Sample")!.GetAttributes()[0];

        return name switch
        {
            "Number" => attribute.ConstructorArguments[0],
            "GuidText" => attribute.ConstructorArguments[1],
            _ => default,
        };
    }

    /// <summary>
    /// 承载测试常量的特性。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ConstantHolderAttribute(int number, string? guidText) : Attribute
    {
        /// <summary>
        /// 数值常量。
        /// </summary>
        public int Number { get; } = number;

        /// <summary>
        /// Guid 文本常量。
        /// </summary>
        public string? GuidText { get; } = guidText;
    }

    /// <summary>
    /// 测试常量值。
    /// </summary>
    public static class ConstantHolder
    {
        /// <summary>
        /// 数值常量。
        /// </summary>
        public const int Number = 7;

        /// <summary>
        /// Guid 文本常量。
        /// </summary>
        public const string GuidValue = "6f9619ff-8b86-d011-b42d-00c04fc964ff";

        /// <summary>
        /// 未设置的常量名称。
        /// </summary>
        public const string NullValue = "NullValue";
    }
}
