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
    /// <param name="id">诊断 ID。</param>
    /// <param name="exception">要报告的异常。</param>
    /// <param name="prefix">可选，附加在诊断 ID 前面的前缀。</param>
    /// <exception cref="ArgumentNullException">当 <paramref name="id"/> 或 <paramref name="exception"/> 为 null 时抛出。</exception>
    public static void ReportException(this SourceProductionContext context, string id, Exception exception, string? prefix = null)
    {
        id = id ?? throw new ArgumentNullException(nameof(id));
        exception = exception ?? throw new ArgumentNullException(nameof(exception));

        context.ReportDiagnostic(exception.ToDiagnostic(id, prefix));
    }

    private static Diagnostic ToDiagnostic(this Exception exception, string id, string? prefix = null)
    {
        if (exception is null) throw new ArgumentNullException(nameof(exception));
        if (id is null) throw new ArgumentNullException(nameof(id));

        if (prefix is not null)
        {
            id = $"{prefix}{id}";
        }

        return Diagnostic.Create(new DiagnosticDescriptor(id, "Exception: ", exception.ToString(), "Usage", DiagnosticSeverity.Error, true), Location.None);
    }
}