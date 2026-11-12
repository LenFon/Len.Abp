using Microsoft.CodeAnalysis;

namespace Len.Abp.Analyzer;

/// <summary>
/// 增量源生成器的通用基类，负责按特性名筛选类、准备数据并输出源文件。
/// </summary>
/// <typeparam name="T">派生生成器使用的数据类型。</typeparam>
/// <param name="id">诊断 ID，用于报告生成过程中的异常。</param>
/// <param name="attributeFullyNames">触发该生成器的特性完全限定名集合。</param>
public abstract class BasicGenerator<T>(string id, params HashSet<string> attributeFullyNames) : IIncrementalGenerator
{
    /// <summary>
    /// 触发该生成器的特性完全限定名集合。
    /// </summary>
    protected readonly HashSet<string> _attributeFullyNames = attributeFullyNames;
    private readonly string _id = id;

    /// <summary>
    /// 注册增量生成管线。
    /// </summary>
    /// <param name="context">增量生成器初始化上下文。</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var version = context.GetVersion();

        foreach (var attributeFullyName in _attributeFullyNames)
        {
            context
                .SyntaxProvider.GetClasses(attributeFullyName)
                .Combine(version)
                .SelectAndReportExceptions(PrepareData, context, _id)
                .WhereNotNull()
                .SelectAndReportExceptions(context => new FileWithName(CreateFileName(context), CreateSourceCode(context)), context, _id)
                .AddSource(context);
        }
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
}