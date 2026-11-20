using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Len.Abp.Analyzer.TestSupport;

/// <summary>
/// 驱动增量生成器针对内存源码运行的共享辅助方法。
/// </summary>
public static class GeneratorTestHost
{
    private static readonly MetadataReference[] FrameworkReferences = BuildFrameworkReferences();

    /// <summary>
    /// 对 <paramref name="source"/> 运行 <paramref name="generator"/>，返回生成的源码。
    /// </summary>
    public static IReadOnlyList<GeneratedSource> Run(IIncrementalGenerator generator, string source)
    {
        var (sources, _) = RunCore(generator, source);
        return sources;
    }

    /// <summary>
    /// 对 <paramref name="source"/> 运行 <paramref name="generator"/>，并把生成结果编译一次。
    /// 生成器只断言文本片段时，非法代码会漏到用户项目里；这里把编译错误一并暴露出来。
    /// </summary>
    /// <returns>生成的源码与生成后的编译错误。</returns>
    public static (IReadOnlyList<GeneratedSource> Sources, IReadOnlyList<string> CompileErrors) RunAndCompile(
        IIncrementalGenerator generator,
        string source
    )
    {
        var (sources, compilation) = RunCore(generator, source);

        var errors = compilation
            .GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .Select(d => d.ToString())
            .ToArray();

        return (sources, errors);
    }

    /// <summary>
    /// 对 <paramref name="source"/> 运行 <paramref name="generator"/>，返回生成的源码与生成器诊断。
    /// </summary>
    /// <param name="generator">待运行的生成器。</param>
    /// <param name="source">待编译的源码。</param>
    /// <param name="filePath">源码对应的文件路径；用于依赖路径的检查（如目录约定）。</param>
    /// <remarks>
    /// 生成器诊断（如 <c>LENABP006</c>）不是编译错误，不会出现在
    /// <see cref="RunAndCompile"/> 的结果里，需要单独从运行结果读取。
    /// </remarks>
    public static (IReadOnlyList<GeneratedSource> Sources, IReadOnlyList<Diagnostic> Diagnostics) RunWithDiagnostics(
        IIncrementalGenerator generator,
        string source,
        string? filePath = null
    )
    {
        var (sources, _, diagnostics) = RunCoreWithDiagnostics([generator], source, allowErrors: false, DocumentationMode.None, filePath);
        return (sources, diagnostics);
    }

    /// <summary>
    /// 对 <paramref name="source"/> 运行 <paramref name="generator"/>，允许生成器报告错误级诊断。
    /// </summary>
    /// <remarks>
    /// <c>LENABP001</c>–<c>LENABP005</c> 都是错误级诊断，用于验证这些诊断的测试必须走本入口，
    /// 否则会被 <see cref="RunWithDiagnostics"/> 的"生成器产生错误"检查拦下。
    /// </remarks>
    /// <returns>生成的源码与生成器诊断。</returns>
    public static (IReadOnlyList<GeneratedSource> Sources, IReadOnlyList<Diagnostic> Diagnostics) RunWithErrors(
        IIncrementalGenerator generator,
        string source
    )
    {
        var (sources, _, diagnostics) = RunCoreWithDiagnostics(generator, source, allowErrors: true);
        return (sources, diagnostics);
    }

    /// <summary>
    /// 对 <paramref name="source"/> 同时运行多个生成器，返回生成的源码、编译错误与生成器诊断。
    /// </summary>
    /// <remarks>
    /// 跨生成器的冲突（例如依赖属性与附加属性生成同名支撑字段）只有同时运行两者才会暴露：
    /// 单独运行任何一个都只看到自己那份成员，编译错误不会出现。
    /// 生成器诊断（如 <c>LENABP004</c>）是错误级，因此本入口始终允许错误级诊断，
    /// 否则"报告诊断且不生成代码"的场景无法断言。
    /// </remarks>
    public static (
        IReadOnlyList<GeneratedSource> Sources,
        IReadOnlyList<string> CompileErrors,
        IReadOnlyList<Diagnostic> Diagnostics
    ) RunAll(IReadOnlyList<IIncrementalGenerator> generators, string source, DocumentationMode documentationMode = DocumentationMode.None)
    {
        var (sources, compilation, diagnostics) = RunCoreWithDiagnostics(generators, source, allowErrors: true, documentationMode);

        var errors = compilation
            .GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .Select(d => d.ToString())
            .ToArray();

        return (sources, errors, diagnostics);
    }

    /// <summary>
    /// 对 <paramref name="source"/> 同时运行多个生成器，返回生成的源码与**全部**编译诊断（含警告）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="RunAll"/> 只返回错误级诊断，适合断言"代码能编译"。
    /// 但 XML 文档问题（<c>CS1570</c>）是**警告**，用 <see cref="RunAll"/> 断言会假绿——
    /// 集合永远是空的，无论有没有问题。需要检查警告时必须走本入口。
    /// </para>
    /// <para>
    /// <paramref name="documentationMode"/> 默认 <see cref="DocumentationMode.Diagnose"/>：
    /// 只有该模式下编译器才解析并诊断文档注释，<see cref="DocumentationMode.None"/>
    /// 下 <c>CS1570</c> 根本不产生。
    /// </para>
    /// </remarks>
    public static (
        IReadOnlyList<GeneratedSource> Sources,
        IReadOnlyList<Diagnostic> CompileDiagnostics,
        IReadOnlyList<Diagnostic> Diagnostics
    ) RunAllWithWarnings(
        IReadOnlyList<IIncrementalGenerator> generators,
        string source,
        DocumentationMode documentationMode = DocumentationMode.Diagnose
    )
    {
        var (sources, compilation, diagnostics) = RunCoreWithDiagnostics(generators, source, allowErrors: true, documentationMode);

        var compileDiagnostics = compilation
            .GetDiagnostics()
            .Where(d => d.Severity >= DiagnosticSeverity.Warning)
            .ToArray();

        return (sources, compileDiagnostics, diagnostics);
    }

    /// <summary>
    /// 对 <paramref name="source"/> 运行 <paramref name="generator"/>，返回生成后的编译对象。
    /// </summary>
    /// <param name="generator">待运行的生成器。</param>
    /// <param name="source">待编译的源码。</param>
    /// <returns>生成后的编译对象，可用符号 API 检查类型、接口与特性。</returns>
    /// <remarks>
    /// 需要断言"生成结果编译后的结构"（接口实现、特性挂载位置、类型是否存在）时用本入口，
    /// 而不是把结果 <c>Emit</c> 成程序集再反射查询：
    /// 符号 API 不加载程序集，不会污染测试进程，也不需要 <c>Assembly.Load</c> 这类反射调用。
    /// </remarks>
    public static Compilation RunToCompilation(IIncrementalGenerator generator, string source)
    {
        var (_, compilation, _) = RunCoreWithDiagnostics([generator], source, allowErrors: false);
        return compilation;
    }

    private static (IReadOnlyList<GeneratedSource> Sources, Compilation Compilation) RunCore(
        IIncrementalGenerator generator,
        string source
    )
    {
        var (sources, compilation, _) = RunCoreWithDiagnostics([generator], source, allowErrors: false);
        return (sources, compilation);
    }

    private static (IReadOnlyList<GeneratedSource> Sources, Compilation Compilation) RunCore(
        IReadOnlyList<IIncrementalGenerator> generators,
        string source
    )
    {
        var (sources, compilation, _) = RunCoreWithDiagnostics(generators, source, allowErrors: false);
        return (sources, compilation);
    }

    private static (IReadOnlyList<GeneratedSource> Sources, Compilation Compilation, IReadOnlyList<Diagnostic> Diagnostics)
        RunCoreWithDiagnostics(IIncrementalGenerator generator, string source, bool allowErrors) =>
        RunCoreWithDiagnostics([generator], source, allowErrors);

    private static (IReadOnlyList<GeneratedSource> Sources, Compilation Compilation, IReadOnlyList<Diagnostic> Diagnostics)
        RunCoreWithDiagnostics(IReadOnlyList<IIncrementalGenerator> generators, string source, bool allowErrors) =>
        RunCoreWithDiagnostics(generators, source, allowErrors, DocumentationMode.None);

    private static (IReadOnlyList<GeneratedSource> Sources, Compilation Compilation, IReadOnlyList<Diagnostic> Diagnostics)
        RunCoreWithDiagnostics(
            IReadOnlyList<IIncrementalGenerator> generators,
            string source,
            bool allowErrors,
            DocumentationMode documentationMode,
            string? filePath = null
        )
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview, documentationMode: documentationMode);

        var compilation = CSharpCompilation.Create(
            assemblyName: "GeneratorTests",
            syntaxTrees: [CSharpSyntaxTree.ParseText(source, parseOptions, path: filePath ?? string.Empty)],
            references: FrameworkReferences,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable)
        );

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [.. generators.Select(generator => generator.AsSourceGenerator())],
            parseOptions: parseOptions
        );
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var updated, out var diagnostics);

        var result = driver.GetRunResult();

        var errors = diagnostics
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .Select(d => d.ToString())
            .ToArray();

        if (!allowErrors && errors.Length > 0)
        {
            throw new InvalidOperationException("生成器产生错误：" + Environment.NewLine + string.Join(Environment.NewLine, errors));
        }

        IReadOnlyList<GeneratedSource> sources =
        [
            .. result.Results
                .SelectMany(r => r.GeneratedSources)
                .Select(s => new GeneratedSource(s.HintName, s.SourceText.ToString()))
        ];

        return (sources, updated, result.Diagnostics);
    }

    private static MetadataReference[] BuildFrameworkReferences()
    {
        var trusted = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty;

        return
        [
            .. trusted
                .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
                .Where(path => path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                .Select(path => MetadataReference.CreateFromFile(path))
        ];
    }
}

/// <summary>
/// 生成器产出的单个源文件。
/// </summary>
/// <param name="HintName">源文件的 hint name。</param>
/// <param name="Text">源文件内容。</param>
public readonly record struct GeneratedSource(string HintName, string Text);
