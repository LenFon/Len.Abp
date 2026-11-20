using Len.Abp.Analyzer;

namespace Microsoft.CodeAnalysis;

/// <summary>
/// <see cref="IncrementalValuesProvider{T}"/> 和 <see cref="IncrementalValueProvider{T}"/> 的扩展方法。
/// </summary>
public static class IncrementalValuesProviderExtensions
{
    /// <summary>
    /// 将 <see cref="IncrementalValuesProvider{FileWithName}"/> 中的源文件添加到编译中。
    /// </summary>
    /// <param name="source">源文件集合。</param>
    /// <param name="context">增量生成器初始化上下文。</param>
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
    /// 将 <see cref="IncrementalValueProvider{FileWithName}"/> 中的单个源文件添加到编译中。
    /// </summary>
    /// <param name="source">源文件。</param>
    /// <param name="context">增量生成器初始化上下文。</param>
    /// <remarks>
    /// 与多值重载对称：单值提供程序无法直接传给多值重载，调用方需要额外包一层
    /// <c>SelectMany</c>，因此保留独立重载。
    /// </remarks>
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
    /// 对单值提供程序应用选择器，并把选择过程中抛出的异常报告为编译诊断。
    /// </summary>
    /// <typeparam name="TSource">源数据类型。</typeparam>
    /// <typeparam name="TResult">结果数据类型。</typeparam>
    /// <param name="source">源提供程序。</param>
    /// <param name="selector">数据转换函数。</param>
    /// <param name="initializationContext">增量生成器初始化上下文。</param>
    /// <param name="locationSelector">从源数据取诊断位置；为 null 时使用 <see cref="Location.None"/>。</param>
    /// <returns>转换后的提供程序。</returns>
    public static IncrementalValueProvider<TResult> SelectAndReportExceptions<TSource, TResult>(
        this IncrementalValueProvider<TSource> source,
        Func<TSource, CancellationToken, TResult> selector,
        IncrementalGeneratorInitializationContext initializationContext,
        Func<TSource, Location?>? locationSelector = null
    )
    {
        var outputWithErrors = source.Select((s, cancellationToken) =>
            TrySelect(s, cancellationToken, selector, locationSelector)
        );

        initializationContext.RegisterSourceOutput(
            outputWithErrors,
            (context, tuple) =>
            {
                if (tuple.Exception is null)
                {
                    return;
                }

                context.ReportException(tuple.Exception, tuple.Location);
            }
        );

        return outputWithErrors.Select(static (x, _) => x.Value!);
    }

    /// <summary>
    /// 对多值提供程序应用选择器，并把选择过程中抛出的异常报告为编译诊断。
    /// </summary>
    /// <typeparam name="TSource">源数据类型。</typeparam>
    /// <typeparam name="TResult">结果数据类型。</typeparam>
    /// <param name="source">源提供程序。</param>
    /// <param name="selector">数据转换函数。</param>
    /// <param name="initializationContext">增量生成器初始化上下文。</param>
    /// <param name="locationSelector">从源数据取诊断位置；为 null 时使用 <see cref="Location.None"/>。</param>
    /// <returns>转换后的提供程序。</returns>
    public static IncrementalValuesProvider<TResult> SelectAndReportExceptions<TSource, TResult>(
        this IncrementalValuesProvider<TSource> source,
        Func<TSource, CancellationToken, TResult> selector,
        IncrementalGeneratorInitializationContext initializationContext,
        Func<TSource, Location?>? locationSelector = null
    )
    {
        var outputWithErrors = source.Select((value, cancellationToken) =>
            TrySelect(value, cancellationToken, selector, locationSelector)
        );

        initializationContext.RegisterSourceOutput(
            outputWithErrors.Where(static x => x.Exception is not null),
            (context, tuple) =>
            {
                context.ReportException(tuple.Exception!, tuple.Location);
            }
        );

        return outputWithErrors.Where(static x => x.Exception is null).Select(static (x, _) => x.Value!);
    }

    /// <summary>
    /// 执行选择器，把异常与诊断位置打包进结果，而不是抛出。
    /// </summary>
    /// <typeparam name="TSource">源数据类型。</typeparam>
    /// <typeparam name="TResult">结果数据类型。</typeparam>
    /// <param name="source">源数据。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <param name="selector">数据转换函数。</param>
    /// <param name="locationSelector">从源数据取诊断位置。</param>
    /// <returns>值、异常与位置的三元组。</returns>
    private static (TResult? Value, Exception? Exception, Location? Location) TrySelect<TSource, TResult>(
        TSource source,
        CancellationToken cancellationToken,
        Func<TSource, CancellationToken, TResult> selector,
        Func<TSource, Location?>? locationSelector
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            return (Value: selector(source, cancellationToken), Exception: null, Location: null);
        }
        catch (Exception exception)
        {
            return (Value: default, Exception: exception, Location: locationSelector?.Invoke(source));
        }
    }

    /// <summary>
    /// 对多值提供程序应用选择器（不接收取消令牌），并把异常报告为编译诊断。
    /// </summary>
    /// <typeparam name="TSource">源数据类型。</typeparam>
    /// <typeparam name="TResult">结果数据类型。</typeparam>
    /// <param name="source">源提供程序。</param>
    /// <param name="selector">数据转换函数。</param>
    /// <param name="initializationContext">增量生成器初始化上下文。</param>
    /// <param name="locationSelector">从源数据取诊断位置；为 null 时使用 <see cref="Location.None"/>。</param>
    /// <returns>转换后的提供程序。</returns>
    public static IncrementalValuesProvider<TResult> SelectAndReportExceptions<TSource, TResult>(
        this IncrementalValuesProvider<TSource> source,
        Func<TSource, TResult> selector,
        IncrementalGeneratorInitializationContext initializationContext,
        Func<TSource, Location?>? locationSelector = null
    )
    {
        return source.SelectAndReportExceptions((x, _) => selector(x), initializationContext, locationSelector);
    }

    /// <summary>
    /// 对单值提供程序应用选择器（不接收取消令牌），并把异常报告为编译诊断。
    /// </summary>
    /// <typeparam name="TSource">源数据类型。</typeparam>
    /// <typeparam name="TResult">结果数据类型。</typeparam>
    /// <param name="source">源提供程序。</param>
    /// <param name="selector">数据转换函数。</param>
    /// <param name="initializationContext">增量生成器初始化上下文。</param>
    /// <param name="locationSelector">从源数据取诊断位置；为 null 时使用 <see cref="Location.None"/>。</param>
    /// <returns>转换后的提供程序。</returns>
    public static IncrementalValueProvider<TResult> SelectAndReportExceptions<TSource, TResult>(
        this IncrementalValueProvider<TSource> source,
        Func<TSource, TResult> selector,
        IncrementalGeneratorInitializationContext initializationContext,
        Func<TSource, Location?>? locationSelector = null
    )
    {
        return source.SelectAndReportExceptions((x, _) => selector(x), initializationContext, locationSelector);
    }

    /// <summary>
    /// 过滤指定 <see cref="IncrementalValuesProvider{TSource}"/> 中的值，排除 null。
    /// </summary>
    /// <typeparam name="TSource">源提供程序中元素的基础值类型，必须是非空值类型。</typeparam>
    /// <param name="source">包含可空值类型、需要过滤的增量值提供程序。</param>
    /// <returns>仅包含源提供程序中非空值的 <see cref="IncrementalValuesProvider{TSource}"/>。</returns>
    public static IncrementalValuesProvider<TSource> WhereNotNull<TSource>(this IncrementalValuesProvider<TSource?> source)
        where TSource : struct => source.Where(static w => w is not null).Select(static (s, _) => s!.Value);
}