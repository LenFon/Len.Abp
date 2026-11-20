using Len.Abp.Analyzer.TestSupport;
using Xunit;

namespace Len.Abp.Analyzer.Wpf.Tests;

/// <summary>
/// 覆盖：<c>RequiredParameters</c> 中的嵌套类型名必须能解析。
/// </summary>
/// <remarks>
/// <para>
/// <c>GetTypeByMetadataName</c> 只认元数据形式（<c>Demo.Outer+Mode</c>），
/// 而使用者写的是源码形式（<c>Demo.Outer.Mode</c>）。修复前三种写法全部解析失败，
/// 静默退化成"只校验存在性"——类型写错和故意不写类型产出完全相同的代码。
/// </para>
/// <para>
/// 这里只断言生成文本，不做编译验证：<see cref="GeneratorTestSources.Stubs"/>
/// 含 WPF 桩类型，与测试宿主引用的真实 WPF 冲突（CS0101）。
/// </para>
/// </remarks>
public class RequiredParameterNestedTypeTests
{
    /// <summary>
    /// 完全限定的嵌套类型名解析为 <c>typeof</c> 表达式。
    /// </summary>
    [Fact]
    public void FullyQualifiedNestedTypeName_Resolves()
    {
        var generated = Generate("""RequiredParameters = new[] { "Mode:Demo.Outer.Mode" }""");

        Assert.Contains("[\"Mode\"] = typeof(global::Demo.Outer.Mode)", generated);
    }

    /// <summary>
    /// 相对嵌套类型名按视图所在命名空间解析。
    /// </summary>
    [Fact]
    public void RelativeNestedTypeName_ResolvesInViewNamespace()
    {
        var generated = Generate("""RequiredParameters = new[] { "Mode:Outer.Mode" }""");

        Assert.Contains("[\"Mode\"] = typeof(global::Demo.Outer.Mode)", generated);
    }

    /// <summary>
    /// 视图与枚举同处一个外层类型时，只写最内层类型名也能解析。
    /// </summary>
    /// <remarks>
    /// 这是最常见的写法：视图与枚举都嵌在同一个外层类里，使用者只写 <c>Mode</c>。
    /// 修复前它同样静默降级。
    /// </remarks>
    [Fact]
    public void SimpleNestedTypeName_ResolvesFromContainingTypes()
    {
        var generated = Generate("""RequiredParameters = new[] { "Mode:Mode" }""", nestedView: true);

        Assert.Contains("[\"Mode\"] = typeof(global::Demo.Outer.Mode)", generated);
    }

    /// <summary>
    /// 多层嵌套类型名解析。
    /// </summary>
    [Fact]
    public void DeeplyNestedTypeName_Resolves()
    {
        var generated = Generate(
            """RequiredParameters = new[] { "Mode:Demo.Outer.Middle.Mode" }""",
            nestedDepth: 2
        );

        Assert.Contains("[\"Mode\"] = typeof(global::Demo.Outer.Middle.Mode)", generated);
    }

    /// <summary>
    /// 命名空间中的类型仍然优先于同名的嵌套类型。
    /// </summary>
    /// <remarks>
    /// 对照组：把点号换成 <c>+</c> 的查找不能抢在正常解析之前，
    /// 否则 <c>Demo.Mode</c> 这种"命名空间 + 类型"会被误拆成嵌套类型。
    /// </remarks>
    [Fact]
    public void NamespaceType_TakesPrecedenceOverNestedType()
    {
        var generated = Generate(
            """RequiredParameters = new[] { "Mode:Demo.Mode" }""",
            extraType: "public enum Mode { A, B }"
        );

        Assert.Contains("[\"Mode\"] = typeof(global::Demo.Mode)", generated);
        Assert.DoesNotContain("global::Demo.Outer.Mode", generated);
    }

    /// <summary>
    /// 无法解析的类型名报告 <c>LENABP014</c> 警告，而不是静默降级。
    /// </summary>
    [Fact]
    public void UnresolvedTypeName_ReportsLENABP014()
    {
        var (_, diagnostics) = GeneratorTestHost.RunWithErrors(new MvvmGenerator(), BuildSource("""RequiredParameters = new[] { "Id:NotAType" }"""));

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "LENABP014");
        Assert.Equal(Microsoft.CodeAnalysis.DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Contains("NotAType", diagnostic.GetMessage());
        Assert.Contains("Id", diagnostic.GetMessage());
    }

    /// <summary>
    /// 未声明类型时不报告诊断：只校验存在性是合法用法。
    /// </summary>
    /// <remarks>
    /// 对照组：诊断必须区分"没写类型"与"写了但解析不出来"，
    /// 否则每个只校验存在性的参数都会收到警告。
    /// </remarks>
    [Fact]
    public void NameOnly_DoesNotReportLENABP014()
    {
        var (_, diagnostics) = GeneratorTestHost.RunWithErrors(new MvvmGenerator(), BuildSource("""RequiredParameters = new[] { "Id" }"""));

        Assert.DoesNotContain(diagnostics, d => d.Id == "LENABP014");
    }

    /// <summary>
    /// 解析成功的嵌套类型不报告诊断。
    /// </summary>
    [Fact]
    public void ResolvedNestedType_DoesNotReportLENABP014()
    {
        var (_, diagnostics) = GeneratorTestHost.RunWithErrors(new MvvmGenerator(), BuildSource("""RequiredParameters = new[] { "Mode:Demo.Outer.Mode" }"""));

        Assert.DoesNotContain(diagnostics, d => d.Id == "LENABP014");
    }

    /// <summary>
    /// 诊断定位到出问题的那一条声明，而不是整个特性。
    /// </summary>
    /// <remarks>
    /// 一个视图可能声明多个参数，只报其中一条时使用者需要知道是哪一条。
    /// </remarks>
    [Fact]
    public void DiagnosticLocation_PointsAtOffendingDeclaration()
    {
        var source = BuildSource("""RequiredParameters = new[] { "Id:int", "Source:NotAType" }""");

        var (_, diagnostics) = GeneratorTestHost.RunWithErrors(new MvvmGenerator(), source);

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "LENABP014");
        var span = diagnostic.Location.SourceSpan;

        Assert.True(span.Length > 0, "诊断应带源码位置。");
        Assert.Contains("Source:NotAType", source[span.Start..span.End]);
    }

    private static string Generate(
        string requiredParameters,
        string? extraType = null,
        int nestedDepth = 1,
        bool nestedView = false
    )
    {
        var sources = GeneratorTestHost.Run(
            new MvvmGenerator(),
            BuildSource(requiredParameters, extraType, nestedDepth, nestedView)
        );

        return sources.ViewModelSource().Text;
    }

    private static string BuildSource(
        string requiredParameters,
        string? extraType = null,
        int nestedDepth = 1,
        bool nestedView = false
    )
    {
        var outer = nestedDepth >= 2 ? "public partial class Outer { public partial class Middle {" : "public partial class Outer {";
        var outerClose = nestedDepth >= 2 ? "} }" : "}";

        var attribute = "[Len.Abp.Wpf.Mvvm.View<PageViewModel>(ViewName = \"page\", " + requiredParameters + ")]";

        // 视图与枚举同处一个外层类型：使用者只写最内层类型名时靠外层链解析。
        var view = nestedView
            ? "    public partial class Outer { public enum Mode { A, B } " + attribute + " public partial class PageView { } }\n"
            : "    " + outer + " public enum Mode { A, B } " + outerClose + "\n\n"
                + "    " + attribute + "\n"
                + "    public partial class PageView { }\n";

        return GeneratorTestSources.With(
            "namespace Demo\n"
                + "{\n"
                + "    public partial class PageViewModel { }\n\n"
                + (extraType is null ? string.Empty : "    " + extraType + "\n\n")
                + view
                + "}\n"
        );
    }
}
