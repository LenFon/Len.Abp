using Len.Abp.Analyzer;

namespace Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// Extensions for <see cref="AnalyzerConfigOptionsProvider"/>.
/// </summary>
public static class AnalyzerConfigOptionsProviderExtensions
{
    public const string FrameworkIsNotRecognized =
        @"Framework is not recognized.You can explicitly specify the framework by setting one of the following constants in your project:HAS_WPF";

    /// <summary>
    /// Get the target framework of the project.
    /// </summary>
    /// <param name="provider"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static Framework GetFramework(this AnalyzerConfigOptionsProvider provider)
    {
        var framework = provider.TryGetFramework();
        if (framework != Framework.Unkwon)
        {
            return framework;
        }

        throw new InvalidOperationException(FrameworkIsNotRecognized);
    }

    /// <summary>
    /// Gets a global option from the provider.
    /// </summary>
    /// <param name="provider"></param>
    /// <param name="name"></param>
    /// <param name="prefix"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static string? GetGlobalOption(this AnalyzerConfigOptionsProvider provider, string name, string? prefix = null)
    {
        provider = provider ?? throw new ArgumentNullException(nameof(provider));
        name = name ?? throw new ArgumentNullException(nameof(name));

        return provider.GlobalOptions.GetOption($"build_property.{GetFullName(name, prefix)}");
    }

    /// <summary>
    /// Determines whether the current build is a design-time build.
    /// </summary>
    /// <param name="provider"></param>
    /// <returns></returns>
    public static bool IsDesignTime(this AnalyzerConfigOptionsProvider provider)
    {
        var isBuildingProjectValue = provider.GetGlobalOption("BuildingProject"); // legacy projects
        var isDesignTimeBuildValue = provider.GetGlobalOption("DesignTimeBuild"); // sdk-style projects

        return string.Equals(isBuildingProjectValue, "false", StringComparison.OrdinalIgnoreCase)
            || string.Equals(isDesignTimeBuildValue, "true", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Tries to get the target framework of the project.
    /// </summary>
    /// <param name="provider"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException">provider is null.</exception>
    public static Framework TryGetFramework(this AnalyzerConfigOptionsProvider provider)
    {
        provider = provider ?? throw new ArgumentNullException(nameof(provider));

        var constants = provider.GetGlobalOption("DefineConstants", prefix: "Framework") ?? string.Empty;
        var useWpf = bool.Parse(provider.GetGlobalOption("UseWPF") ?? bool.FalseString) || constants.Contains("HAS_WPF");

        return (useWpf) switch
        {
            true => Framework.Wpf,
            _ => Framework.Unkwon,
        };
    }

    private static string GetFullName(string name, string? prefix = null)
    {
        return prefix == null ? name : $"{prefix}_{name}";
    }
}