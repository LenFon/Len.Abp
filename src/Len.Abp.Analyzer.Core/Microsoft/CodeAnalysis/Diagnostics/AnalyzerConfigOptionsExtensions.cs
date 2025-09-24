namespace Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// Extensions for <see cref="AnalyzerConfigOptions"/>.
/// </summary>
public static class AnalyzerConfigOptionsExtensions
{
    /// <summary>
    /// Gets the value associated with the specified key from the <see cref="AnalyzerConfigOptions"/>.
    /// </summary>
    /// <param name="options"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static string? GetOption(this AnalyzerConfigOptions options, string key)
    {
        options = options ?? throw new ArgumentNullException(nameof(options));
        key = key ?? throw new ArgumentNullException(nameof(key));

        return options.TryGetValue(key, out var result) && !string.IsNullOrWhiteSpace(result) ? result : null;
    }
}