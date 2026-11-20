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
        var result = Create(DeclarationName: "Sample").WrapInTypeDeclarations("void M() { }");

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
        var result = Create(DeclarationName: "Sample", IsRecord: true).WrapInTypeDeclarations("int X;");

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
        var result = Create(DeclarationName: "Sample", IsStatic: true).WrapInTypeDeclarations("int X;");

        Assert.Contains("static partial class Sample", result);
        Assert.DoesNotContain("partial static class", result);
    }

    /// <summary>
    /// 指定自身声明时使用该声明，并保留基类型列表的续行缩进。
    /// </summary>
    [Fact]
    public void WrapInTypeDeclarations_SelfDeclaration_PreservesContinuationIndent()
    {
        var result = Create(DeclarationName: "Sample").WrapInTypeDeclarations(
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
        var result = Create(DeclarationName: "Inner", ContainingTypes: ["partial class Outer", "partial struct Middle"])
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
        var result = Create(DeclarationName: "Sample").WrapInTypeDeclarations("\n\nint A;\n\nint B;\n\n");

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
        var result = Create(DeclarationName: "Sample").WrapInTypeDeclarations("    int A;\n        int B;");

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
        var result = Create(DeclarationName: "Sample").WrapInTypeDeclarations("    int A;\n  \n    int B;");

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
        var result = Create(DeclarationName: "Sample").WrapInTypeDeclarations("int X;", "partial class Sample\n\n");

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

    /// <summary>
    /// 无基类型时只生成类型声明本身。
    /// </summary>
    [Fact]
    public void SelfDeclaration_WithoutBaseTypes_HasNoColon()
    {
        Assert.Equal("partial class Sample", Create(DeclarationName: "Sample").SelfDeclaration());
    }

    /// <summary>
    /// 声明用未闭合的类型参数，而 <see cref="ClassData.Type"/> 保持闭合。
    /// </summary>
    /// <remarks>
    /// 回归：这两个字段原先共用一个 <c>Name</c>，语义在"声明名"与"显示名"之间摇摆。
    /// 分部声明必须用未闭合参数（CS1001/CS0264），<c>typeof()</c> 与特性参数必须用闭合实参
    /// （CS0416）。任何一处混用都会在泛型类型上产出无法编译的代码。
    /// </remarks>
    [Fact]
    public void DeclarationNameAndType_HaveDifferentSemantics()
    {
        var data = Create(
            DeclarationName: "PageViewModel<T>",
            SimpleName: "PageViewModel",
            FullName: "Demo.PageViewModel<T>",
            Type: "global::Demo.PageViewModel<int>"
        );

        Assert.Contains("partial class PageViewModel<T>", data.SelfDeclaration());
        Assert.Equal("global::Demo.PageViewModel<int>", data.Type);
        Assert.DoesNotContain("<int>", data.SelfDeclaration());
    }

    /// <summary>
    /// 有基类型时按顺序拼接，用逗号分隔。
    /// </summary>
    [Fact]
    public void SelfDeclaration_WithBaseTypes_JoinsByComma()
    {
        var result = Create(DeclarationName: "Sample").SelfDeclaration(["global::System.IDisposable", "global::System.IAsyncDisposable"]);

        Assert.Equal(
            "partial class Sample : global::System.IDisposable, global::System.IAsyncDisposable",
            result
        );
    }

    /// <summary>
    /// record 类型必须保留 <c>record</c> 关键字，否则与用户声明冲突（CS0261）。
    /// </summary>
    [Fact]
    public void SelfDeclaration_Record_UsesRecordKeyword()
    {
        Assert.StartsWith("partial record Sample", Create(DeclarationName: "Sample", IsRecord: true).SelfDeclaration());
    }

    /// <summary>
    /// 静态类型的修饰符顺序必须是 <c>static partial</c>，写成 <c>partial static</c> 会报 CS0267。
    /// </summary>
    [Fact]
    public void SelfDeclaration_StaticClass_UsesStaticPartialOrder()
    {
        Assert.Equal("static partial class Sample", Create(DeclarationName: "Sample", IsStatic: true).SelfDeclaration());
    }

    /// <summary>
    /// 特性紧贴最内层类型声明，嵌套时按当前层级缩进。
    /// </summary>
    /// <remarks>
    /// 特性拼在整个块之前会被编译器绑定到外层类型，嵌套视图的注册特性会因此挂错目标。
    /// </remarks>
    [Fact]
    public void WrapInTypeDeclarations_LeadingAttributes_AttachToInnermostType()
    {
        var result = Create(DeclarationName: "Inner", ContainingTypes: ["partial class Outer"])
            .WrapInTypeDeclarations("int X;", leadingAttributes: "[global::System.Obsolete]\n");

        Assert.Equal(
            """
            partial class Outer
            {
                [global::System.Obsolete]
                partial class Inner
                {
                    int X;
                }
            }

            """.ReplaceLineEndings("\n"),
            result
        );
    }

    private static ClassData Create(
        string Namespace = "Demo",
        string DeclarationName = "Sample",
        string SimpleName = "Sample",
        string FullName = "Demo.Sample",
        string Type = "global::Demo.Sample",
        IReadOnlyList<string>? MethodNames = null,
        IReadOnlyList<string>? ContainingTypes = null,
        bool IsRecord = false,
        bool HasAnyConstructor = false,
        bool HasParameterlessConstructor = false,
        bool IsStatic = false
    ) =>
        new(
            Namespace,
            DeclarationName,
            SimpleName,
            FullName,
            Type,
            MethodNames ?? [],
            ContainingTypes ?? [],
            IsRecord,
            HasAnyConstructor,
            HasParameterlessConstructor,
            IsStatic
        );
}
