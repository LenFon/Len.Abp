using Len.Abp.Analyzer;

namespace Microsoft.CodeAnalysis;

/// <summary>
/// Extensions for <see cref="IncrementalValuesProvider{T}"/> and <see cref="IncrementalValueProvider{T}"/>.
/// </summary>
public static class IncrementalValuesProviderExtensions
{
    /// <summary>
    /// Adds source files to the compilation from an <see cref="IncrementalValuesProvider{FileWithName}"/>.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="context"></param>
    public static void AddSource(this IncrementalValuesProvider<FileWithName> source, IncrementalGeneratorInitializationContext context) =>
        context.RegisterSourceOutput(
            source,
            static (ctx, file) =>
            {
                if (file.IsEmpty)
                    return;

                ctx.AddSource(file.Name, file.Text);
            }
        );

    /// <summary>
    /// Adds a source file to the compilation from an <see cref="IncrementalValueProvider{FileWithName}"/>.
    /// </summary>
    /// <param name="source"></param>
    /// <param name="context"></param>
    public static void AddSource(this IncrementalValueProvider<FileWithName> source, IncrementalGeneratorInitializationContext context) =>
        context.RegisterSourceOutput(
            source,
            static (ctx, file) =>
            {
                if (file.IsEmpty)
                    return;

                ctx.AddSource(file.Name, file.Text);
            }
        );

    public static IncrementalValueProvider<TResult> SelectAndReportExceptions<TSource, TResult>(
        this IncrementalValueProvider<TSource> source,
        Func<TSource, CancellationToken, TResult> selector,
        IncrementalGeneratorInitializationContext initializationContext,
        string id = "SRE001"
    )
    {
        var outputWithErrors = source.Select<TSource, (TResult? Value, Exception? Exception)>(
            (s, cancellationToken) =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    return (Value: selector(s, cancellationToken), Exception: null);
                }
                catch (Exception exception)
                {
                    return (Value: default, Exception: exception);
                }
            }
        );

        initializationContext.RegisterSourceOutput(
            outputWithErrors,
            (context, tuple) =>
            {
                if (tuple.Exception is null)
                {
                    return;
                }

                context.ReportException(id: id, exception: tuple.Exception);
            }
        );

        return outputWithErrors.Select(static (x, _) => x.Value!);
    }

    public static IncrementalValuesProvider<TResult> SelectAndReportExceptions<TSource, TResult>(
        this IncrementalValuesProvider<TSource> source,
        Func<TSource, CancellationToken, TResult> selector,
        IncrementalGeneratorInitializationContext initializationContext,
        string id = "SRE001"
    )
    {
        var outputWithErrors = source.Select<TSource, (TResult? Value, Exception? Exception)>(
            (value, cancellationToken) =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    return (Value: selector(value, cancellationToken), Exception: null);
                }
                catch (Exception exception)
                {
                    return (Value: default, Exception: exception);
                }
            }
        );

        initializationContext.RegisterSourceOutput(
            outputWithErrors.Where(static x => x.Exception is not null),
            (context, tuple) =>
            {
                context.ReportException(id: id, exception: tuple.Exception!);
            }
        );

        return outputWithErrors.Where(static x => x.Exception is null).Select(static (x, _) => x.Value!);
    }

    public static IncrementalValuesProvider<TResult> SelectAndReportExceptions<TSource, TResult>(
        this IncrementalValuesProvider<TSource> source,
        Func<TSource, TResult> selector,
        IncrementalGeneratorInitializationContext initializationContext,
        string id = "SRE001"
    )
    {
        return source.SelectAndReportExceptions((x, _) => selector(x), initializationContext, id);
    }

    public static IncrementalValueProvider<TResult> SelectAndReportExceptions<TSource, TResult>(
        this IncrementalValueProvider<TSource> source,
        Func<TSource, TResult> selector,
        IncrementalGeneratorInitializationContext initializationContext,
        string id = "SRE001"
    )
    {
        return source.SelectAndReportExceptions((x, _) => selector(x), initializationContext, id);
    }

    public static IncrementalValuesProvider<TResult> SelectAndReportExceptions<TResult, TLeft>(
        this IncrementalValuesProvider<(TLeft Left, Framework Right)> source,
        Func<Framework, TLeft, TResult> selector,
        IncrementalGeneratorInitializationContext context,
        string id = "SRE001"
    )
    {
        return source.SelectAndReportExceptions(x => selector(x.Right, x.Left), context, id);
    }

    /// <summary>
    /// Filters the values of the specified <see cref="IncrementalValuesProvider{TSource}"/> to exclude null values.
    /// </summary>
    /// <typeparam name="TSource">The underlying value type of the elements in the source provider. Must be a non-nullable value type.</typeparam>
    /// <param name="source">The incremental values provider containing nullable value types to filter.</param>
    /// <returns>An <see cref="IncrementalValuesProvider{TSource}"/> containing only the non-null values from the source
    /// provider.</returns>
    public static IncrementalValuesProvider<TSource> WhereNotNull<TSource>(this IncrementalValuesProvider<TSource?> source)
        where TSource : struct => source.Where(static w => w is not null).Select(static (s, _) => s!.Value);
}