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
    /// 报告的诊断固定使用 <c>LENABP005</c>，并包含异常类型与消息。
    /// </summary>
    [Fact]
    public void ReportException_UsesGeneratorFailureDescriptor()
    {
        var diagnostics = RunReportingGenerator(ctx => ctx.ReportException(new InvalidOperationException("boom")));

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("LENABP005", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("InvalidOperationException", diagnostic.GetMessage());
        Assert.Contains("boom", diagnostic.GetMessage());
    }

    /// <summary>
    /// 未传位置时退化为 <see cref="Location.None"/>。
    /// </summary>
    [Fact]
    public void ReportException_WithoutLocation_UsesLocationNone()
    {
        var diagnostics = RunReportingGenerator(ctx => ctx.ReportException(new Exception("boom")));

        Assert.Equal(Location.None, Assert.Single(diagnostics).Location);
    }

    /// <summary>
    /// 传入位置时诊断带该位置，可跳转到源码。
    /// </summary>
    /// <remarks>
    /// 位置必须来自编译中的语法树，否则 Roslyn 会把诊断位置降级为 <see cref="Location.None"/>。
    /// </remarks>
    [Fact]
    public void ReportException_WithLocation_UsesProvidedLocation()
    {
        var diagnostics = RunReportingGenerator(
            (ctx, compilation) =>
            {
                var location = compilation.SyntaxTrees
                    .Single()
                    .GetRoot()
                    .DescendantNodes()
                    .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>()
                    .Single()
                    .Identifier.GetLocation();

                ctx.ReportException(new Exception("boom"), location);
            }
        );

        var diagnostic = Assert.Single(diagnostics);
        Assert.NotEqual(Location.None, diagnostic.Location);
        Assert.True(diagnostic.Location.IsInSource);
    }

    /// <summary>
    /// 异常为 null 时抛出异常。
    /// </summary>
    [Fact]
    public void ReportException_NullException_Throws()
    {
        var exception = Assert.Throws<ArgumentNullException>(
            () => default(SourceProductionContext).ReportException(null!)
        );

        Assert.Equal("exception", exception.ParamName);
    }

    private static System.Collections.Immutable.ImmutableArray<Diagnostic> RunReportingGenerator(
        Action<SourceProductionContext> report
    ) => RunReportingGenerator((ctx, _) => report(ctx));

    private static System.Collections.Immutable.ImmutableArray<Diagnostic> RunReportingGenerator(
        Action<SourceProductionContext, Compilation> report
    )
    {
        var compilation = CSharpCompilation.Create(
            "ContextTests",
            syntaxTrees: [CSharpSyntaxTree.ParseText("class C { }")]
        );

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ReportingGenerator(report).AsSourceGenerator());
        driver = driver.RunGenerators(compilation);

        return driver.GetRunResult().Diagnostics;
    }

    private sealed class ReportingGenerator(Action<SourceProductionContext, Compilation> report) : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context) =>
            context.RegisterSourceOutput(context.CompilationProvider, (ctx, compilation) => report(ctx, compilation));
    }
}
