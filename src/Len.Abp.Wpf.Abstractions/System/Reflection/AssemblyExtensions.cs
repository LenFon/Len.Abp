using System.Collections;
using System.Resources;
using Volo.Abp;

namespace System.Reflection;

/// <summary>
/// <see cref="Assembly"/> 的扩展方法，用于读取 WPF 资源。
/// </summary>
public static class AssemblyExtensions
{
    /// <summary>
    /// 判断程序集中是否存在指定的资源。
    /// </summary>
    /// <param name="assembly">目标程序集。</param>
    /// <param name="resourceName">资源名称。</param>
    /// <returns>存在返回 true，否则返回 false。</returns>
    public static bool ExistsResource(this Assembly assembly, string resourceName)
    {
        Check.NotNull(assembly, nameof(assembly));
        Check.NotNull(resourceName, nameof(resourceName));

        return GetResourceNames(assembly).Contains(resourceName.ToLowerInvariant());
    }

    /// <summary>
    /// 获取程序集中所有 WPF 资源的名称。
    /// </summary>
    /// <param name="assembly">目标程序集。</param>
    /// <returns>资源名称集合；程序集不包含资源时返回空数组。</returns>
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

    /// <summary>
    /// 判断程序集是否为 WPF 程序集，即是否引用了 WPF 核心程序集。
    /// </summary>
    /// <param name="assembly">目标程序集。</param>
    /// <returns>是 WPF 程序集返回 true，否则返回 false。</returns>
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
