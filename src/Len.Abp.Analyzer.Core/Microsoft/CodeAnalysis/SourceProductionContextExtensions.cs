namespace Microsoft.CodeAnalysis;

/// <summary>
///
/// </summary>
public static class SourceProductionContextExtensions
{
    /// <summary>
    ///
    /// </summary>
    /// <param name="context"></param>
    /// <param name="id"></param>
    /// <param name="exception"></param>
    /// <param name="prefix"></param>
    /// <exception cref="ArgumentNullException"></exception>
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