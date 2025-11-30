namespace Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// Extensions for <see cref="AnalyzerConfigOptionsProvider"/>.
/// </summary>
public static class AnalyzerConfigOptionsProviderExtensions
{
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

    private static string GetFullName(string name, string? prefix = null)
    {
        return prefix == null ? name : $"{prefix}_{name}";
    }
}