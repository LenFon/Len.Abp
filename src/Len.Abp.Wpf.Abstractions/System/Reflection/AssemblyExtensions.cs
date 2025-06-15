using System.Collections;
using System.Resources;
using Volo.Abp;

namespace System.Reflection;

public static class AssemblyExtensions
{
    public static bool ExistsResource(this Assembly assembly, string resourceName)
    {
        Check.NotNull(assembly, nameof(assembly));
        Check.NotNull(resourceName, nameof(resourceName));

        return GetResourceNames(assembly).Contains(resourceName.ToLowerInvariant());
    }

    public static object[] GetResourceNames(this Assembly assembly)
    {
        Check.NotNull(assembly, nameof(assembly));

        string resName = assembly.GetName().Name + ".g.resources";
        using var stream = assembly.GetManifestResourceStream(resName);

        if (stream is null)
            return [];

        using var reader = new ResourceReader(stream);

        return [.. reader.Cast<DictionaryEntry>().Select(s => s.Key)];
    }

    public static bool IsWpfAssembly(this Assembly assembly)
    {
        Check.NotNull(assembly, nameof(assembly));

        // WPF 核心程序集
        string[] wpfAssemblies = ["PresentationFramework", "WindowsBase", "PresentationCore"];

        // 获取该 DLL 引用的所有程序集
        var referencedAssemblies = assembly.GetReferencedAssemblies();

        // 检查是否引用了 WPF 相关程序集
        return referencedAssemblies.Any(r => wpfAssemblies.Contains(r.Name));
    }
}