using System.Reflection;

namespace Microsoft.CodeAnalysis;

/// <summary>
/// <see cref="IncrementalGeneratorInitializationContext"/> 的扩展方法。
/// </summary>
public static class IncrementalGeneratorInitializationContextExtensions
{
    /// <summary>
    /// 从分析器配置选项中获取生成器程序集的版本号。
    /// </summary>
    /// <param name="context">增量生成器初始化上下文。</param>
    /// <returns>生成器程序集的版本号。</returns>
    public static IncrementalValueProvider<string> GetVersion(this IncrementalGeneratorInitializationContext context)
    {
        var version = $"{Assembly.GetCallingAssembly().GetName().Version}";

        return context.AnalyzerConfigOptionsProvider.Select((_, _) => version);
    }
}