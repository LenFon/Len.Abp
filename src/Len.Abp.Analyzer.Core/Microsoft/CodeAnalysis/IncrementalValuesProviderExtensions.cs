using Len.Abp.Analyzer;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

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

    /// <summary>
    /// Get the target framework for the current project during source generation.
    /// </summary>
    /// <remarks>This method is intended for use within source generators to determine the framework  being
    /// targeted by the project. The returned value can be used to tailor generated code  to the specific
    /// framework.</remarks>
    /// <param name="context">The context for the incremental generator, providing access to the compilation and project information.</param>
    /// <returns>An <see cref="IncrementalValueProvider{T}"/> that provides the detected <see cref="Framework"/>  for the current
    /// project.</returns>
    public static IncrementalValueProvider<Framework> GetFramework(this IncrementalGeneratorInitializationContext context)
    {
        var frameworkWithDiagnostic = context.AnalyzerConfigOptionsProvider.Select(
            (s, _) =>
            {
                var framework = s.TryGetFramework();
                var diagnostic =
                    framework == Framework.Unkwon
                        ? Diagnostic.Create(
                            new DiagnosticDescriptor(
                                "DF001",
                                "Framework is not recognized",
                                AnalyzerConfigOptionsProviderExtensions.FrameworkIsNotRecognized,
                                "Usage",
                                DiagnosticSeverity.Error,
                                true
                            ),
                            Location.None
                        )
                        : null;

                return (Framework: framework, Diagnostic: diagnostic);
            }
        );

        context.RegisterSourceOutput(
            frameworkWithDiagnostic,
            static (ctx, s) =>
            {
                if (s.Diagnostic == null)
                    return;

                ctx.ReportDiagnostic(s.Diagnostic);
            }
        );

        return frameworkWithDiagnostic.Select(static (s, _) => s.Framework);
    }

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
    /// Selects attributes from the given <see cref="IncrementalValuesProvider{GeneratorAttributeSyntaxContext}"/>,
    /// </summary>
    /// <param name="source">
    /// The source <see cref="IncrementalValuesProvider{GeneratorAttributeSyntaxContext}"/> to select attributes from.
    /// </param>
    /// <returns>
    /// An <see cref="IncrementalValuesProvider{ClassWithAttributesContext}"/> containing the selected attributes
    /// </returns>
    public static IncrementalValuesProvider<ClassWithAttributesContext> SelectAttributes(
        this IncrementalValuesProvider<GeneratorAttributeSyntaxContext> source
    )
    {
        return source.Select(
            static (context, _) =>
                new ClassWithAttributesContext(
                    SemanticModel: context.SemanticModel,
                    Attributes: context.Attributes,
                    ClassSyntax: (ClassDeclarationSyntax)context.TargetNode,
                    ClassSymbol: (INamedTypeSymbol)context.TargetSymbol
                )
        );
    }

    /// <summary>
    /// Selects each attribute individually from the given <see cref="IncrementalValuesProvider{GeneratorAttributeSyntaxContext}"/>.
    /// </summary>
    /// <param name="source">
    /// The source <see cref="IncrementalValuesProvider{GeneratorAttributeSyntaxContext}"/> to select attributes from.
    /// </param>
    /// <returns>
    /// An <see cref="IncrementalValuesProvider{ClassWithAttributesContext}"/> containing each attribute and its associated class information
    /// </returns>
    public static IncrementalValuesProvider<ClassWithAttributesContext> SelectManyAttributes(
        this IncrementalValuesProvider<GeneratorAttributeSyntaxContext> source
    )
    {
        return source.SelectMany(
            static (context, _) =>
                context
                    .Attributes.Where(w =>
                    {
                        var classSyntax = (ClassDeclarationSyntax)context.TargetNode;
                        var attributeSyntax = classSyntax.TryFindAttributeSyntax(w);

                        return attributeSyntax != null;
                    })
                    .Select(s => new ClassWithAttributesContext(
                        SemanticModel: context.SemanticModel,
                        Attributes: [s],
                        ClassSyntax: (ClassDeclarationSyntax)context.TargetNode,
                        ClassSymbol: (INamedTypeSymbol)context.TargetSymbol
                    ))
        );
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