using Len.Abp.Analyzer;
using Len.Abp.Analyzer.TestSupport;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Len.Abp.Analyzer.Tests;

/// <summary>
/// 覆盖 <see cref="IncrementalValuesProviderExtensions"/> 的异常上报与空文件过滤。
/// </summary>
public class IncrementalValuesProviderExtensionsTests
{
    /// <summary>
    /// 选择器抛出的异常被报告为编译诊断，且不产出源文件。
    /// </summary>
    [Fact]
    public void SelectAndReportExceptions_ThrowingSelector_ReportsDiagnostic()
    {
        var (sources, diagnostics) = RunGenerator(
            new ThrowingGenerator(),
            """
            namespace Demo
            {
                [global::Len.Abp.Analyzer.Tests.IncrementalValuesProviderExtensionsTests.MarkerAttribute]
                public partial class Sample { }
            }
            """
        );

        Assert.Empty(sources);
        Assert.Contains(diagnostics, d => d.Id == "TEST001" && d.Severity == DiagnosticSeverity.Error);
    }

    /// <summary>
    /// 返回空文件时不添加源文件。
    /// </summary>
    [Fact]
    public void AddSource_EmptyFile_IsSkipped()
    {
        var (sources, diagnostics) = RunGenerator(
            new EmptyFileGenerator(),
            """
            namespace Demo
            {
                [global::Len.Abp.Analyzer.Tests.IncrementalValuesProviderExtensionsTests.MarkerAttribute]
                public partial class Sample { }
            }
            """
        );

        Assert.Empty(sources);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
    }

    /// <summary>
    /// 返回非空文件时添加源文件。
    /// </summary>
    [Fact]
    public void AddSource_NonEmptyFile_IsAdded()
    {
        var (sources, _) = RunGenerator(
            new NonEmptyFileGenerator(),
            """
            namespace Demo
            {
                [global::Len.Abp.Analyzer.Tests.IncrementalValuesProviderExtensionsTests.MarkerAttribute]
                public partial class Sample { }
            }
            """
        );

        Assert.Single(sources);
        Assert.Contains("// generated", sources[0].Text);
    }

    private static (IReadOnlyList<GeneratedSource> Sources, IReadOnlyList<Diagnostic> Diagnostics) RunGenerator(
        IIncrementalGenerator generator,
        string source
    )
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        var compilation = CSharpCompilation.Create(
            assemblyName: "AnalyzerInfraTests",
            syntaxTrees: [CSharpSyntaxTree.ParseText(source, parseOptions)],
            references: TestReferences.Framework.Value,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [generator.AsSourceGenerator()],
            parseOptions: parseOptions
        );
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);

        var result = driver.GetRunResult();

        return (
            [.. result.Results.SelectMany(r => r.GeneratedSources).Select(s => new GeneratedSource(s.HintName, s.SourceText.ToString()))],
            diagnostics
        );
    }

    /// <summary>
    /// 测试用的标记特性。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class MarkerAttribute : Attribute;

    private sealed class ThrowingGenerator : BasicGenerator<object>
    {
        public ThrowingGenerator() : base("TEST001", "Len.Abp.Analyzer.Tests.IncrementalValuesProviderExtensionsTests+MarkerAttribute") { }

        protected override string CreateFileName((ClassData Class, object Data, string Version) context) => "x.g.cs";

        protected override string CreateSourceCode((ClassData Class, object Data, string Version) context) => "// x";

        protected override (ClassData Class, object Data, string Version)? PrepareData((ClassContext, string) context) =>
            throw new InvalidOperationException("准备数据失败");
    }

    private sealed class EmptyFileGenerator : BasicGenerator<object>
    {
        public EmptyFileGenerator() : base("TEST002", "Len.Abp.Analyzer.Tests.IncrementalValuesProviderExtensionsTests+MarkerAttribute") { }

        protected override string CreateFileName((ClassData Class, object Data, string Version) context) => string.Empty;

        protected override string CreateSourceCode((ClassData Class, object Data, string Version) context) => string.Empty;

        protected override (ClassData Class, object Data, string Version)? PrepareData((ClassContext, string) context) =>
            (context.Item1.ClassSymbol.GetClassData(), new object(), context.Item2);
    }

    private sealed class NonEmptyFileGenerator : BasicGenerator<object>
    {
        public NonEmptyFileGenerator() : base("TEST003", "Len.Abp.Analyzer.Tests.IncrementalValuesProviderExtensionsTests+MarkerAttribute") { }

        protected override string CreateFileName((ClassData Class, object Data, string Version) context) => "sample.g.cs";

        protected override string CreateSourceCode((ClassData Class, object Data, string Version) context) => "// generated";

        protected override (ClassData Class, object Data, string Version)? PrepareData((ClassContext, string) context) =>
            (context.Item1.ClassSymbol.GetClassData(), new object(), context.Item2);
    }
}
