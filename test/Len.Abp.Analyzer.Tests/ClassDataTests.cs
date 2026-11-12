using Len.Abp.Analyzer;
using Xunit;

namespace Len.Abp.Analyzer.Tests;

/// <summary>
/// 覆盖 <see cref="ClassData"/> 的派生属性与 partial 声明拼装。
/// </summary>
public class ClassDataTests
{
    /// <summary>
    /// 命名空间为空时声明为空字符串。
    /// </summary>
    [Fact]
    public void NamespaceDeclaration_EmptyNamespace_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, Create(Namespace: string.Empty).NamespaceDeclaration);
    }

    /// <summary>
    /// 命名空间非空时生成文件范围声明。
    /// </summary>
    [Fact]
    public void NamespaceDeclaration_WithNamespace_ReturnsDeclaration()
    {
        Assert.Equal("namespace Demo.Sub;", Create(Namespace: "Demo.Sub").NamespaceDeclaration);
    }

    /// <summary>
    /// hint name 去掉泛型参数列表，并把逗号与空格替换为下划线。
    /// </summary>
    [Theory]
    [InlineData("Demo.Control", "Demo.Control")]
    [InlineData("Demo.Control<T>", "Demo.Control")]
    [InlineData("Demo.Pair<TKey, TValue>", "Demo.Pair")]
    [InlineData("Demo.Nested<T>.Inner", "Demo.Nested.Inner")]
    public void HintName_RemovesGenericArguments(string fullName, string expected)
    {
        Assert.Equal(expected, Create(FullName: fullName).HintName);
    }

    /// <summary>
    /// 没有显式构造函数时可以链到无参构造函数。
    /// </summary>
    [Fact]
    public void CanChainToParameterlessConstructor_NoConstructor_ReturnsTrue()
    {
        Assert.True(Create(HasAnyConstructor: false, HasParameterlessConstructor: false).CanChainToParameterlessConstructor);
    }

    /// <summary>
    /// 有显式无参构造函数时可以链。
    /// </summary>
    [Fact]
    public void CanChainToParameterlessConstructor_WithParameterless_ReturnsTrue()
    {
        Assert.True(Create(HasAnyConstructor: true, HasParameterlessConstructor: true).CanChainToParameterlessConstructor);
    }

    /// <summary>
    /// 只有带参构造函数时不能链。
    /// </summary>
    [Fact]
    public void CanChainToParameterlessConstructor_OnlyParameterized_ReturnsFalse()
    {
        Assert.False(Create(HasAnyConstructor: true, HasParameterlessConstructor: false).CanChainToParameterlessConstructor);
    }

    /// <summary>
    /// 方法名按简单名称精确匹配，不做前缀匹配。
    /// </summary>
    [Fact]
    public void HasMethod_MatchesExactSimpleName()
    {
        var data = Create(MethodNames: ["CoerceTitle", "CoerceTitle2"]);

        Assert.True(data.HasMethod("CoerceTitle"));
        Assert.True(data.HasMethod("CoerceTitle2"));
        Assert.False(data.HasMethod("Coerce"));
    }

    /// <summary>
    /// 无外层类型时生成单个 partial 类声明。
    /// </summary>
    [Fact]
    public void WrapInTypeDeclarations_TopLevelClass_WrapsBody()
    {
        var result = Create(Name: "Sample").WrapInTypeDeclarations("void M() { }");

        Assert.Equal(
            """
            partial class Sample
            {
                void M() { }
            }

            """.ReplaceLineEndings("\n"),
            result
        );
    }

    /// <summary>
    /// 记录类型生成 partial record 声明。
    /// </summary>
    [Fact]
    public void WrapInTypeDeclarations_Record_UsesRecordKeyword()
    {
        var result = Create(Name: "Sample", IsRecord: true).WrapInTypeDeclarations("int X;");

        Assert.Contains("partial record Sample", result);
    }

    /// <summary>
    /// 静态类生成 <c>static partial class</c>，修饰符顺序不能颠倒。
    /// </summary>
    /// <remarks>
    /// 写成 <c>partial static class</c> 会报 CS0267；漏掉 <c>static</c> 会与用户声明的静态类冲突。
    /// </remarks>
    [Fact]
    public void WrapInTypeDeclarations_StaticClass_UsesStaticPartialOrder()
    {
        var result = Create(Name: "Sample", IsStatic: true).WrapInTypeDeclarations("int X;");

        Assert.Contains("static partial class Sample", result);
        Assert.DoesNotContain("partial static class", result);
    }

    /// <summary>
    /// 指定自身声明时使用该声明，并保留基类型列表的续行缩进。
    /// </summary>
    [Fact]
    public void WrapInTypeDeclarations_SelfDeclaration_PreservesContinuationIndent()
    {
        var result = Create(Name: "Sample").WrapInTypeDeclarations(
            "int X;",
            "partial class Sample :\n    global::System.IDisposable"
        );

        Assert.Equal(
            """
            partial class Sample :
                global::System.IDisposable
            {
                int X;
            }

            """.ReplaceLineEndings("\n"),
            result
        );
    }

    /// <summary>
    /// 嵌套类型逐层展开，外层声明按由外到内缩进。
    /// </summary>
    [Fact]
    public void WrapInTypeDeclarations_NestedTypes_ExpandsEachLevel()
    {
        var result = Create(Name: "Inner", ContainingTypes: ["partial class Outer", "partial struct Middle"])
            .WrapInTypeDeclarations("int X;");

        Assert.Equal(
            """
            partial class Outer
            {
                partial struct Middle
                {
                    partial class Inner
                    {
                        int X;
                    }
                }
            }

            """.ReplaceLineEndings("\n"),
            result
        );
    }

    /// <summary>
    /// 类型体首尾的空行被去掉，内部空行保留。
    /// </summary>
    [Fact]
    public void WrapInTypeDeclarations_BodyWithLeadingAndTrailingBlankLines_TrimsThem()
    {
        var result = Create(Name: "Sample").WrapInTypeDeclarations("\n\nint A;\n\nint B;\n\n");

        Assert.Equal(
            """
            partial class Sample
            {
                int A;

                int B;
            }

            """.ReplaceLineEndings("\n"),
            result
        );
    }

    /// <summary>
    /// 类型体的公共缩进被剥离，成员内部相对缩进保留。
    /// </summary>
    [Fact]
    public void WrapInTypeDeclarations_BodyWithCommonIndent_StripsIndent()
    {
        var result = Create(Name: "Sample").WrapInTypeDeclarations("    int A;\n        int B;");

        Assert.Equal(
            """
            partial class Sample
            {
                int A;
                    int B;
            }

            """.ReplaceLineEndings("\n"),
            result
        );
    }

    /// <summary>
    /// 公共缩进大于某行长度时该行退化为空行。
    /// </summary>
    [Fact]
    public void WrapInTypeDeclarations_LineShorterThanCommonIndent_BecomesBlank()
    {
        var result = Create(Name: "Sample").WrapInTypeDeclarations("    int A;\n  \n    int B;");

        Assert.Equal(
            """
            partial class Sample
            {
                int A;

                int B;
            }

            """.ReplaceLineEndings("\n"),
            result
        );
    }

    /// <summary>
    /// 声明中的空行被跳过，不产生多余空行。
    /// </summary>
    [Fact]
    public void WrapInTypeDeclarations_DeclarationWithBlankLine_SkipsIt()
    {
        var result = Create(Name: "Sample").WrapInTypeDeclarations("int X;", "partial class Sample\n\n");

        Assert.Equal(
            """
            partial class Sample
            {
                int X;
            }

            """.ReplaceLineEndings("\n"),
            result
        );
    }

    private static ClassData Create(
        string Namespace = "Demo",
        string Name = "Sample",
        string SimpleName = "Sample",
        string FullName = "Demo.Sample",
        string Type = "global::Demo.Sample",
        IReadOnlyList<string>? Methods = null,
        IReadOnlyList<string>? MethodNames = null,
        IReadOnlyList<string>? ContainingTypes = null,
        bool IsRecord = false,
        bool HasAnyConstructor = false,
        bool HasParameterlessConstructor = false,
        bool IsStatic = false
    ) =>
        new(
            Namespace,
            Name,
            SimpleName,
            FullName,
            Type,
            Methods ?? [],
            MethodNames ?? [],
            ContainingTypes ?? [],
            IsRecord,
            HasAnyConstructor,
            HasParameterlessConstructor,
            IsStatic
        );
}
