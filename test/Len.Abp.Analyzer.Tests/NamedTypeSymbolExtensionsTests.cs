using Len.Abp.Analyzer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Len.Abp.Analyzer.Tests;

/// <summary>
/// 覆盖 <see cref="Microsoft.CodeAnalysis.NamedTypeSymbolExtensions"/> 的类数据提取。
/// </summary>
public class NamedTypeSymbolExtensionsTests
{
    /// <summary>
    /// 顶层类提取命名空间、名称与类型名。
    /// </summary>
    [Fact]
    public void GetClassData_TopLevelClass_ExtractsNames()
    {
        var data = GetClassData("namespace Demo.Sub { public class Sample { } }", "Demo.Sub.Sample");

        Assert.Equal("Demo.Sub", data.Namespace);
        Assert.Equal("Sample", data.Name);
        Assert.Equal("Sample", data.SimpleName);
        Assert.Equal("Demo.Sub.Sample", data.FullName);
        Assert.Empty(data.ContainingTypes);
        Assert.False(data.IsRecord);
    }

    /// <summary>
    /// 全局命名空间的类型命名空间为空字符串。
    /// </summary>
    [Fact]
    public void GetClassData_GlobalNamespace_ReturnsEmptyNamespace()
    {
        var data = GetClassData("public class Sample { }", "Sample");

        Assert.Equal(string.Empty, data.Namespace);
    }

    /// <summary>
    /// 记录类型标记为 record。
    /// </summary>
    [Fact]
    public void GetClassData_Record_SetsIsRecord()
    {
        var data = GetClassData("namespace Demo { public record Sample; }", "Demo.Sample");

        Assert.True(data.IsRecord);
    }

    /// <summary>
    /// 嵌套类型的外层声明按由外到内排列，并保留类、记录与结构体关键字。
    /// </summary>
    [Fact]
    public void GetClassData_NestedTypes_CollectsContainingDeclarations()
    {
        var data = GetClassData(
            """
            namespace Demo
            {
                public partial class Outer
                {
                    public partial record Middle
                    {
                        public partial struct Inner { }
                    }
                }
            }
            """,
            "Demo.Outer+Middle+Inner"
        );

        Assert.Equal(["partial class Outer", "partial record Middle"], data.ContainingTypes);
    }

    /// <summary>
    /// 方法名与方法显示名都被收集，显示名去掉可空标记。
    /// </summary>
    [Fact]
    public void GetClassData_CollectsMethods()
    {
        var data = GetClassData(
            """
            namespace Demo
            {
                public class Sample
                {
                    public string? Find(int id) => null;
                    public void Run() { }
                }
            }
            """,
            "Demo.Sample"
        );

        Assert.Contains("Find", data.MethodNames);
        Assert.Contains("Run", data.MethodNames);
        Assert.Contains(data.Methods, m => m.Contains("Find") && !m.Contains('?'));
    }

    /// <summary>
    /// 没有显式构造函数时两个构造函数标记都为 false。
    /// </summary>
    [Fact]
    public void GetClassData_NoExplicitConstructor_SetsFlagsFalse()
    {
        var data = GetClassData("namespace Demo { public class Sample { } }", "Demo.Sample");

        Assert.False(data.HasAnyConstructor);
        Assert.False(data.HasParameterlessConstructor);
    }

    /// <summary>
    /// 只有带参构造函数时 HasAnyConstructor 为 true、无参标记为 false。
    /// </summary>
    [Fact]
    public void GetClassData_ParameterizedConstructorOnly_SetsFlags()
    {
        var data = GetClassData(
            "namespace Demo { public class Sample { public Sample(int value) { } } }",
            "Demo.Sample"
        );

        Assert.True(data.HasAnyConstructor);
        Assert.False(data.HasParameterlessConstructor);
    }

    /// <summary>
    /// 显式无参构造函数时两个标记都为 true。
    /// </summary>
    [Fact]
    public void GetClassData_ParameterlessConstructor_SetsFlags()
    {
        var data = GetClassData(
            "namespace Demo { public class Sample { public Sample() { } } }",
            "Demo.Sample"
        );

        Assert.True(data.HasAnyConstructor);
        Assert.True(data.HasParameterlessConstructor);
    }

    /// <summary>
    /// 符号为 null 时抛出异常。
    /// </summary>
    [Fact]
    public void GetClassData_NullSymbol_Throws()
    {
        var exception = Assert.Throws<ArgumentNullException>(
            () => ((INamedTypeSymbol)null!).GetClassData()
        );

        Assert.Equal("classSymbol", exception.ParamName);
    }

    private static ClassData GetClassData(string source, string metadataName)
    {
        var compilation = CSharpCompilation.Create(
            "NamedTypeSymbolTests",
            syntaxTrees: [CSharpSyntaxTree.ParseText(source)],
            references: TestReferences.Framework.Value
        );

        return compilation.GetTypeByMetadataName(metadataName)!.GetClassData();
    }
}
