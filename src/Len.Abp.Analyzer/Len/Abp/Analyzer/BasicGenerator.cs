using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace Len.Abp.Analyzer;

/// <summary>
/// 增量源生成器的通用基类，负责按特性名筛选类、准备数据并输出源文件。
/// </summary>
/// <typeparam name="T">派生生成器使用的数据类型。</typeparam>
/// <param name="attributeFullyNames">触发该生成器的特性完全限定名集合。</param>
public abstract class BasicGenerator<T>(params HashSet<string> attributeFullyNames) : IIncrementalGenerator
{
    /// <summary>
    /// 触发该生成器的特性完全限定名集合。
    /// </summary>
    private readonly HashSet<string> _attributeFullyNames = attributeFullyNames;

    /// <summary>
    /// 注册增量生成管线。
    /// </summary>
    /// <param name="context">增量生成器初始化上下文。</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var version = context.GetVersion(GetType());
        var prepared = new List<IncrementalValuesProvider<(ClassData Class, T Data, string Version)>>();

        foreach (var attributeFullyName in _attributeFullyNames)
        {
            var classes = context.SyntaxProvider.GetClasses(attributeFullyName).Combine(version);

            // 诊断与源文件分开注册：诊断不依赖生成结果，即使 PrepareData 返回 null 也要报告。
            context.RegisterSourceOutput(classes, (ctx, pair) => ReportDiagnostics(ctx, pair.Left));

            var items = classes
                .SelectAndReportExceptions(PrepareData, context, GetLocation)
                .WhereNotNull();

            items
                // 生成阶段拿不到语法节点（ClassData 只保留文本），位置退化为 Location.None；
                // 诊断消息里已包含类型全名，足以定位。
                .SelectAndReportExceptions(context => new FileWithName(CreateFileName(context), CreateSourceCode(context)), context)
                .AddSource(context);

            prepared.Add(items);
        }

        RegisterAdditionalSources(context, Merge(prepared));
        RegisterAdditionalOutputs(context);
    }

    /// <summary>
    /// 把各特性名的已准备数据合并成一个集合提供程序。
    /// </summary>
    /// <param name="prepared">每个特性名对应的已准备数据提供程序。</param>
    /// <returns>聚合后的集合提供程序。</returns>
    /// <remarks>
    /// <see cref="IncrementalValuesProvider{T}"/> 没有 <c>Concat</c>，只能先各自
    /// <c>Collect()</c> 再两两 <c>Combine</c>。聚合是必要的：同一个视图模型可能被
    /// 多个视图（乃至两种视图特性写法）引用，去重必须看到全部条目。
    /// </remarks>
    private static IncrementalValueProvider<ImmutableArray<(ClassData Class, T Data, string Version)>> Merge(
        IReadOnlyList<IncrementalValuesProvider<(ClassData Class, T Data, string Version)>> prepared)
    {
        var merged = prepared[0].Collect();

        for (var i = 1; i < prepared.Count; i++)
        {
            var next = prepared[i].Collect();

            merged = merged
                .Combine(next)
                .Select(static (pair, _) => pair.Left.AddRange(pair.Right));
        }

        return merged;
    }

    /// <summary>
    /// 注册基于已准备数据的附加源文件。
    /// </summary>
    /// <param name="context">增量生成器初始化上下文。</param>
    /// <param name="prepared">所有特性名的已准备数据，已聚合为一个集合。</param>
    /// <remarks>
    /// <para>
    /// 默认不注册。需要为同一个目标类型产出多个源文件、或需要跨视图聚合的生成器重写本方法。
    /// </para>
    /// <para>
    /// 与 <see cref="RegisterAdditionalOutputs"/> 的区别：这里拿到的是
    /// <see cref="PrepareData"/> 的产出，因此复用同一套前置校验，不会为被跳过的类型生成文件；
    /// 后者拿到的是原始 <see cref="ClassContext"/>，用于需要语法位置的诊断。
    /// </para>
    /// </remarks>
    protected virtual void RegisterAdditionalSources(
        IncrementalGeneratorInitializationContext context,
        IncrementalValueProvider<ImmutableArray<(ClassData Class, T Data, string Version)>> prepared)
    {
    }

    /// <summary>
    /// 注册需要跨条目聚合的额外输出。
    /// </summary>
    /// <param name="context">增量生成器初始化上下文。</param>
    /// <remarks>
    /// 默认不注册。需要跨文件检查的生成器（例如重复的视图名称）重写本方法，
    /// 用 <c>Collect()</c> 聚合后再报告。
    /// 与 <see cref="Initialize"/> 分开是因为后者不是虚方法：让派生类重写整个管线
    /// 容易漏掉基类的注册步骤。
    /// </remarks>
    protected virtual void RegisterAdditionalOutputs(IncrementalGeneratorInitializationContext context)
    {
    }

    /// <summary>
    /// 报告类级别的诊断。
    /// </summary>
    /// <param name="context">源生成上下文。</param>
    /// <param name="classContext">类上下文。</param>
    /// <remarks>
    /// 默认不报告任何诊断。派生生成器可重写以校验使用者声明是否合法。
    /// 与 <see cref="PrepareData"/> 分离，是因为后者返回 null 时管线会跳过该类，
    /// 诊断就再也没机会报告。
    /// </remarks>
    protected virtual void ReportDiagnostics(SourceProductionContext context, ClassContext classContext)
    {
    }

    /// <summary>
    /// 生成源文件的名称。
    /// </summary>
    /// <param name="context">类数据、生成器数据与版本号。</param>
    /// <returns>源文件名。</returns>
    protected abstract string CreateFileName((ClassData Class, T Data, string Version) context);

    /// <summary>
    /// 生成源文件的代码内容。
    /// </summary>
    /// <param name="context">类数据、生成器数据与版本号。</param>
    /// <returns>源文件代码。</returns>
    protected abstract string CreateSourceCode((ClassData Class, T Data, string Version) context);

    /// <summary>
    /// 从类上下文中准备生成所需的数据。
    /// </summary>
    /// <param name="context">类上下文与版本号。</param>
    /// <returns>准备好的数据；返回 null 表示跳过该类。</returns>
    protected abstract (ClassData Class, T Data, string Version)? PrepareData((ClassContext, string) context);

    /// <summary>
    /// 从类上下文取诊断位置。
    /// </summary>
    /// <param name="context">类上下文与版本号。</param>
    /// <returns>类型标识符的位置。</returns>
    private static Location GetLocation((ClassContext ClassContext, string Version) context) =>
        context.ClassContext.ClassSyntax.Identifier.GetLocation();
}