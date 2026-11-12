using Microsoft.CodeAnalysis;

namespace Len.Abp.Analyzer.Tests;

/// <summary>
/// 测试编译共用的框架引用集。
/// </summary>
internal static class TestReferences
{
    public static readonly Lazy<MetadataReference[]> Framework = new(() =>
    {
        var trusted = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty;

        return
        [
            .. trusted
                .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
                .Where(path => path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                .Select(path => MetadataReference.CreateFromFile(path))
        ];
    });
}
