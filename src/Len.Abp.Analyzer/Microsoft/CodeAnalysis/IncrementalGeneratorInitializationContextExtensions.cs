namespace Microsoft.CodeAnalysis;

/// <summary>
/// <see cref="IncrementalGeneratorInitializationContext"/> 的扩展方法。
/// </summary>
public static class IncrementalGeneratorInitializationContextExtensions
{
    /// <summary>
    /// 获取指定生成器所在程序集的版本号。
    /// </summary>
    /// <param name="context">增量生成器初始化上下文。</param>
    /// <param name="generatorType">生成器类型，通常是 <c>GetType()</c>。</param>
    /// <returns>生成器程序集的版本号。</returns>
    /// <remarks>
    /// <para>
    /// 显式传入类型而不是用 <see cref="System.Reflection.Assembly.GetCallingAssembly"/>：
    /// 后者在方法被内联时返回的是<b>调用方</b>的程序集，取到的可能是使用者的版本号。
    /// 本方法只有几个指令，被内联的概率不低，而版本号会写进生成代码的
    /// <c>GeneratedCode</c> 特性，错了很难发现。
    /// </para>
    /// <para>
    /// 也不能用 <see cref="System.Reflection.Assembly.GetExecutingAssembly"/>：它返回本扩展方法所在的
    /// <c>Len.Abp.Analyzer</c>，而生成器在 <c>Len.Abp.Analyzer.Wpf</c>，
    /// 两个程序集的版本号并不相同。
    /// </para>
    /// <para>
    /// 这里只读取程序集元数据（<c>GetName().Version</c>），不做类型或成员反射。
    /// </para>
    /// </remarks>
    public static IncrementalValueProvider<string> GetVersion(
        this IncrementalGeneratorInitializationContext context,
        Type generatorType
    )
    {
        // 分析器目标框架是 netstandard2.0，没有 ArgumentNullException.ThrowIfNull。
        if (generatorType is null)
        {
            throw new ArgumentNullException(nameof(generatorType));
        }

        var version = $"{generatorType.Assembly.GetName().Version}";

        return context.AnalyzerConfigOptionsProvider.Select((_, _) => version);
    }
}
