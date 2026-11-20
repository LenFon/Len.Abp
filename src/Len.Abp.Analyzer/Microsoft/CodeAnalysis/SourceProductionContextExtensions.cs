using Len.Abp.Analyzer;

namespace Microsoft.CodeAnalysis;

/// <summary>
/// <see cref="SourceProductionContext"/> 的扩展方法。
/// </summary>
public static class SourceProductionContextExtensions
{
    /// <summary>
    /// 将生成过程中捕获的异常报告为编译诊断，便于在编译输出中定位问题。
    /// </summary>
    /// <param name="context">源生成上下文。</param>
    /// <param name="exception">要报告的异常。</param>
    /// <param name="location">诊断位置；为 null 时退化为 <see cref="Location.None"/>。</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="exception"/> 为 null 时抛出。</exception>
    /// <remarks>
    /// 统一使用 <c>LENABP005</c>：异常来自生成器自身，与具体是哪个生成器无关，
    /// 异常类型与消息已写进诊断内容。ID 固定后使用者可以按 ID 检索与抑制。
    /// </remarks>
    public static void ReportException(
        this SourceProductionContext context,
        Exception exception,
        Location? location = null
    )
    {
        exception = exception ?? throw new ArgumentNullException(nameof(exception));

        context.ReportDiagnostic(
            Diagnostic.Create(
                DiagnosticDescriptors.GeneratorFailure,
                location ?? Location.None,
                exception.GetType().Name,
                exception.Message
            )
        );
    }
}