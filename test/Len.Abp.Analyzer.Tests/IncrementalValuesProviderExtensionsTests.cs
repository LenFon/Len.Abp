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

    /// <summary>
    /// 单值提供程序返回非空文件时添加源文件。
    /// </summary>
    [Fact]
    public void AddSource_SingleValue_NonEmptyFile_IsAdded()
    {
        var (sources, diagnostics) = RunGenerator(
            new SingleFileGenerator("// single"),
            """
            namespace Demo
            {
                [global::Len.Abp.Analyzer.Tests.IncrementalValuesProviderExtensionsTests.MarkerAttribute]
                public partial class Sample { }
            }
            """
        );

        Assert.Single(sources);
        Assert.Contains("// single", sources[0].Text);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
    }

    /// <summary>
    /// 单值提供程序返回空文件时不添加源文件。
    /// </summary>
    [Fact]
    public void AddSource_SingleValue_EmptyFile_IsSkipped()
    {
        var (sources, diagnostics) = RunGenerator(
            new SingleFileGenerator(string.Empty),
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
    /// 单值选择器抛出的异常被报告为编译诊断；单值重载不丢弃值，仍输出默认值。
    /// </summary>
    [Fact]
    public void SelectAndReportExceptions_SingleValueThrowingSelector_ReportsDiagnostic()
    {
        var (sources, diagnostics) = RunGenerator(
            new SingleValueThrowingGenerator(),
            """
            namespace Demo
            {
                [global::Len.Abp.Analyzer.Tests.IncrementalValuesProviderExtensionsTests.MarkerAttribute]
                public partial class Sample { }
            }
            """
        );

        Assert.Contains(diagnostics, d => d.Id == "TEST004" && d.Severity == DiagnosticSeverity.Error);
        Assert.Single(sources);
    }

    /// <summary>
    /// 多值选择器（不接收取消令牌）抛出的异常被报告为编译诊断，且该值被过滤。
    /// </summary>
    [Fact]
    public void SelectAndReportExceptions_ValuesSelectorWithoutToken_ReportsDiagnostic()
    {
        var (sources, diagnostics) = RunGenerator(
            new ValuesThrowingGeneratorWithoutToken(),
            """
            namespace Demo
            {
                [global::Len.Abp.Analyzer.Tests.IncrementalValuesProviderExtensionsTests.MarkerAttribute]
                public partial class Sample { }
            }
            """
        );

        Assert.Empty(sources);
        Assert.Contains(diagnostics, d => d.Id == "TEST005" && d.Severity == DiagnosticSeverity.Error);
    }

    /// <summary>
    /// 单值选择器（不接收取消令牌）抛出的异常被报告为编译诊断。
    /// </summary>
    [Fact]
    public void SelectAndReportExceptions_SingleValueSelectorWithoutToken_ReportsDiagnostic()
    {
        var (sources, diagnostics) = RunGenerator(
            new SingleValueThrowingGeneratorWithoutToken(),
            """
            namespace Demo
            {
                [global::Len.Abp.Analyzer.Tests.IncrementalValuesProviderExtensionsTests.MarkerAttribute]
                public partial class Sample { }
            }
            """
        );

        Assert.Contains(diagnostics, d => d.Id == "TEST006" && d.Severity == DiagnosticSeverity.Error);
        Assert.Single(sources);
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

    /// <summary>
    /// 通过单值提供程序输出一个文件，用于覆盖 <c>AddSource(IncrementalValueProvider)</c>。
    /// </summary>
    private sealed class SingleFileGenerator(string text) : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var file = context.CompilationProvider.Select((_, _) => new FileWithName("single.g.cs", text));

            file.AddSource(context);
        }
    }

    /// <summary>
    /// 单值选择器抛异常的生成器。
    /// </summary>
    private sealed class SingleValueThrowingGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var value = context.CompilationProvider
                .Select((_, _) => 0)
                .SelectAndReportExceptions<int, int>((_, _) => throw new InvalidOperationException("单值失败"), context, "TEST004")
                .Select((x, _) => new FileWithName("x.g.cs", x.ToString()));

            value.AddSource(context);
        }
    }

    /// <summary>
    /// 多值选择器（不接收取消令牌）抛异常的生成器。
    /// </summary>
    private sealed class ValuesThrowingGeneratorWithoutToken : IIncrementalGenerator
    {
        private static readonly int[] SingleValueSource = [1];

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var values = context.CompilationProvider
                .SelectMany((_, _) => SingleValueSource)
                .SelectAndReportExceptions<int, int>(_ => throw new InvalidOperationException("多值失败"), context, "TEST005")
                .Select((x, _) => new FileWithName("x.g.cs", x.ToString()));

            values.AddSource(context);
        }
    }

    /// <summary>
    /// 单值选择器（不接收取消令牌）抛异常的生成器。
    /// </summary>
    private sealed class SingleValueThrowingGeneratorWithoutToken : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var value = context.CompilationProvider
                .Select((_, _) => 0)
                .SelectAndReportExceptions<int, int>(_ => throw new InvalidOperationException("单值失败"), context, "TEST006")
                .Select((x, _) => new FileWithName("x.g.cs", x.ToString()));

            value.AddSource(context);
        }
    }
}
