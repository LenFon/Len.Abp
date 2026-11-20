using Len.Abp.Analyzer.TestSupport;
using Xunit;

namespace Len.Abp.Analyzer.Wpf.Tests;

/// <summary>
/// 覆盖 <c>LENABP015</c> / <c>LENABP016</c>：视图与视图模型必须分目录存放。
/// </summary>
/// <remarks>
/// 这两条诊断依赖源文件路径，因此测试必须给语法树指定 <c>path</c>；
/// 不指定时生成器无从判断目录，按设计放行（见 <c>FolderConventionDiagnostics</c>）。
/// </remarks>
public class FolderConventionTests
{
    private const string Snippet = """
        namespace Demo
        {
            public partial class PageViewModel { }

            [Len.Abp.Wpf.Mvvm.View<PageViewModel>(ViewName = "page")]
            public partial class PageView { }
        }
        """;

    /// <summary>
    /// 视图在 <c>Views</c>、视图模型在 <c>ViewModels</c> 时不报告诊断。
    /// </summary>
    [Fact]
    public void CompliantLayout_ReportsNothing()
    {
        var diagnostics = RunView("D:/App/Views/PageView.cs");

        Assert.DoesNotContain(diagnostics, d => d.Id == "LENABP015");
    }

    /// <summary>
    /// 视图不在 <c>Views</c> 目录时报告 <c>LENABP015</c>。
    /// </summary>
    [Fact]
    public void ViewOutsideViewsFolder_ReportsLENABP015()
    {
        var diagnostics = RunView("D:/App/Pages/PageView.cs");

        Assert.Contains(diagnostics, d => d.Id == "LENABP015");
    }

    /// <summary>
    /// 视图模型不在 <c>ViewModels</c> 目录时报告 <c>LENABP016</c>。
    /// </summary>
    /// <remarks>
    /// 视图与视图模型写在同一份源码里，因此两者共用同一个路径。
    /// 视图合规、视图模型不合规的场景用 <c>Views/PageView.cs</c> 表达。
    /// </remarks>
    [Fact]
    public void ViewModelOutsideViewModelsFolder_ReportsLENABP016()
    {
        var diagnostics = RunView("D:/App/Views/PageView.cs");

        Assert.Contains(diagnostics, d => d.Id == "LENABP016");
        Assert.DoesNotContain(diagnostics, d => d.Id == "LENABP015");
    }

    /// <summary>
    /// 目录名比较忽略大小写。
    /// </summary>
    /// <remarks>
    /// Windows 上目录名大小写不敏感，<c>views</c> 与 <c>Views</c> 是同一个目录，
    /// 按 <see cref="StringComparison.Ordinal"/> 比较会误报。
    /// </remarks>
    [Theory]
    [InlineData("D:/App/views/PageView.cs")]
    [InlineData("D:/App/VIEWS/PageView.cs")]
    public void FolderNameComparison_IsCaseInsensitive(string path)
    {
        var diagnostics = RunView(path);

        Assert.DoesNotContain(diagnostics, d => d.Id == "LENABP015");
    }

    /// <summary>
    /// 按功能分目录时，只要路径里有 <c>Views</c> 段就算合规。
    /// </summary>
    [Fact]
    public void NestedFeatureFolder_IsCompliant()
    {
        var diagnostics = RunView("D:/App/Features/Orders/Views/PageView.cs");

        Assert.DoesNotContain(diagnostics, d => d.Id == "LENABP015");
    }

    /// <summary>
    /// 名字里含 <c>Views</c> 但不是独立目录段时不算合规。
    /// </summary>
    /// <remarks>
    /// 用 <c>Contains</c> 判断会让 <c>MyViews/</c>、<c>ViewsArchive/</c> 误判为合规，
    /// 必须按路径分隔符切分后逐段比较。
    /// </remarks>
    [Theory]
    [InlineData("D:/App/MyViews/PageView.cs")]
    [InlineData("D:/App/ViewsArchive/PageView.cs")]
    public void FolderNameMustMatchWholeSegment(string path)
    {
        var diagnostics = RunView(path);

        Assert.Contains(diagnostics, d => d.Id == "LENABP015");
    }

    /// <summary>
    /// 源文件没有路径时不检查目录。
    /// </summary>
    /// <remarks>
    /// 生成器测试的内存源码、从标准输入编译都没有路径。
    /// 这种情况下报告诊断会让所有既有生成器测试都命中，而使用者无从修复。
    /// </remarks>
    [Fact]
    public void NoFilePath_DoesNotReport()
    {
        var diagnostics = RunView(filePath: null);

        Assert.DoesNotContain(diagnostics, d => d.Id == "LENABP015");
        Assert.DoesNotContain(diagnostics, d => d.Id == "LENABP016");
    }

    /// <summary>
    /// 非 <c>partial</c> 的视图模型不检查目录。
    /// </summary>
    /// <remarks>
    /// 非 <c>partial</c> 时生成器不会为视图模型追加任何成员，目录问题与生成器无关；
    /// 那种情况已由 <c>LENABP006</c> 覆盖，再报目录只会增加噪音。
    /// </remarks>
    [Fact]
    public void NonPartialViewModel_DoesNotReportFolderDiagnostic()
    {
        var source = GeneratorTestSources.With(
            """
            namespace Demo
            {
                public class PageViewModel { }

                [Len.Abp.Wpf.Mvvm.View<PageViewModel>(ViewName = "page")]
                public partial class PageView { }
            }
            """
        );

        var (_, diagnostics) = GeneratorTestHost.RunWithDiagnostics(new MvvmGenerator(), source, "D:/App/Views/PageView.cs");

        Assert.DoesNotContain(diagnostics, d => d.Id == "LENABP016");
    }

    /// <summary>
    /// 诊断消息里带上实际路径，便于定位。
    /// </summary>
    [Fact]
    public void DiagnosticMessage_ContainsActualPath()
    {
        var diagnostics = RunView("D:/App/Pages/PageView.cs");

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "LENABP015");

        Assert.Contains("D:/App/Pages/PageView.cs", diagnostic.GetMessage());
    }

    private static IReadOnlyList<Microsoft.CodeAnalysis.Diagnostic> RunView(string? filePath)
    {
        var source = GeneratorTestSources.With(Snippet);

        var (_, diagnostics) = GeneratorTestHost.RunWithDiagnostics(new MvvmGenerator(), source, filePath);

        return diagnostics;
    }
}
