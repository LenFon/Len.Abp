using Len.Abp.Analyzer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Len.Abp.Analyzer.Tests;

/// <summary>
/// 覆盖 <see cref="SourceProductionContextExtensions"/> 的诊断构造。
/// </summary>
/// <remarks>
/// <see cref="SourceProductionContext"/> 是只读结构体，只能由生成器驱动提供，
/// 因此这里通过一个最小生成器在真实上下文里调用被测方法。
/// </remarks>
public class SourceProductionContextExtensionsTests
{
    /// <summary>
    /// 报告的诊断包含指定的 ID、错误级别与异常文本。
    /// </summary>
    [Fact]
    public void ReportException_CreatesDiagnosticWithId()
    {
        var diagnostics = RunReportingGenerator(ctx => ctx.ReportException("TEST001", new InvalidOperationException("boom")));

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("TEST001", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("boom", diagnostic.GetMessage());
    }

    /// <summary>
    /// 指定前缀时诊断 ID 带上前缀。
    /// </summary>
    [Fact]
    public void ReportException_WithPrefix_PrependsToId()
    {
        var diagnostics = RunReportingGenerator(
            ctx => ctx.ReportException("001", new Exception("boom"), "PREFIX")
        );

        Assert.Equal("PREFIX001", Assert.Single(diagnostics).Id);
    }

    /// <summary>
    /// ID 为 null 时抛出异常。
    /// </summary>
    [Fact]
    public void ReportException_NullId_Throws()
    {
        // 参数校验发生在 ReportDiagnostic 之前，因此用 default 上下文即可触发。
        var exception = Assert.Throws<ArgumentNullException>(
            () => default(SourceProductionContext).ReportException(null!, new Exception())
        );

        Assert.Equal("id", exception.ParamName);
    }

    /// <summary>
    /// 异常为 null 时抛出异常。
    /// </summary>
    [Fact]
    public void ReportException_NullException_Throws()
    {
        var exception = Assert.Throws<ArgumentNullException>(
            () => default(SourceProductionContext).ReportException("TEST001", null!)
        );

        Assert.Equal("exception", exception.ParamName);
    }

    private static System.Collections.Immutable.ImmutableArray<Diagnostic> RunReportingGenerator(
        Action<SourceProductionContext> report
    )
    {
        var compilation = CSharpCompilation.Create("ContextTests");
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ReportingGenerator(report).AsSourceGenerator());
        driver = driver.RunGenerators(compilation);

        return driver.GetRunResult().Diagnostics;
    }

    private sealed class ReportingGenerator(Action<SourceProductionContext> report) : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context) =>
            context.RegisterSourceOutput(context.CompilationProvider, (ctx, _) => report(ctx));
    }
}
