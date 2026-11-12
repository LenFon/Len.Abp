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

    private static (IReadOnlyList<GeneratedSource> Sources, Compilation Compilation) RunCore(
        IIncrementalGenerator generator,
        string source
    )
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);

        var compilation = CSharpCompilation.Create(
            assemblyName: "GeneratorTests",
            syntaxTrees: [CSharpSyntaxTree.ParseText(source, parseOptions)],
            references: FrameworkReferences,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable)
        );

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [generator.AsSourceGenerator()],
            parseOptions: parseOptions
        );
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var updated, out var diagnostics);

        var result = driver.GetRunResult();

        var errors = diagnostics
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .Select(d => d.ToString())
            .ToArray();

        if (errors.Length > 0)
        {
            throw new InvalidOperationException("生成器产生错误：" + Environment.NewLine + string.Join(Environment.NewLine, errors));
        }

        IReadOnlyList<GeneratedSource> sources =
        [
            .. result.Results
                .SelectMany(r => r.GeneratedSources)
                .Select(s => new GeneratedSource(s.HintName, s.SourceText.ToString()))
        ];

        return (sources, updated);
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
