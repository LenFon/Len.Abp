using System.Reflection;

namespace Microsoft.CodeAnalysis;

/// <summary>
/// Extensions for <see cref="IncrementalGeneratorInitializationContext"/>.
/// </summary>
public static class IncrementalGeneratorInitializationContextExtensions
{
    /// <summary>
    /// Get the version of the RecognizeFramework from the analyzer config options.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public static IncrementalValueProvider<string> GetVersion(this IncrementalGeneratorInitializationContext context)
    {
        var version = $"{Assembly.GetCallingAssembly().GetName().Version}";

        return context.AnalyzerConfigOptionsProvider.Select((_, _) => version);
    }
}