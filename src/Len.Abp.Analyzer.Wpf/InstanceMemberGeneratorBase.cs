using Len.Abp.Analyzer;
using Microsoft.CodeAnalysis;

namespace Len.Abp.Analyzer;

/// <summary>
/// 生成器数据中带成员名称的类型。
/// </summary>
/// <remarks>
/// 依赖属性与路由事件的数据都带一个"名称"，它同时决定成员名、支撑字段名与 hint name。
/// 抽成接口是为了让 <see cref="InstanceMemberGeneratorBase{T}"/> 能统一拼装文件名，
/// 不必为两种数据各写一份。
/// </remarks>
internal interface INamedMemberData
{
    /// <summary>
    /// 获取成员名称。
    /// </summary>
    string Name { get; }
}

/// <summary>
/// 生成"实例成员 + 附加版本"成对特性的生成器共享基类。
/// </summary>
/// <typeparam name="T">派生生成器使用的数据类型。</typeparam>
/// <remarks>
/// <para>
/// 依赖属性与路由事件的结构完全同构：一个实例版本、一个附加版本，
/// 两者的前置校验、诊断报告与文件名规则逐字相同，只有成员声明与回调体不同。
/// 这些共同部分放在这里，派生类只提供"生成什么成员"。
/// </para>
/// <para>
/// 校验顺序固定为：特性存在 → 参数合法且类型 partial → 宿主能承载实例成员。
/// 三者的失败原因不同，但处理方式一致：只报告诊断、不生成代码。
/// 集中在一处才能保证两个生成器的判定口径不会分叉。
/// </para>
/// </remarks>
/// <param name="attributeFullyNames">触发该生成器的特性完全限定名集合。</param>
internal abstract class InstanceMemberGeneratorBase<T>(params HashSet<string> attributeFullyNames)
    : BasicGenerator<T>(attributeFullyNames)
    where T : struct, INamedMemberData
{
    /// <summary>
    /// 生成成员上 <c>GeneratedCode</c> 特性所标注的生成器名称。
    /// </summary>
    protected abstract string GeneratorName { get; }

    /// <summary>
    /// 获取生成器是否产出实例成员。
    /// </summary>
    /// <remarks>
    /// 非附加版本生成实例成员与 <c>protected</c> 成员，静态类无法承载（CS0708/CS1057）；
    /// 附加版本只生成静态方法，静态类宿主在编译层面合法。
    /// </remarks>
    protected abstract bool GeneratesInstanceMembers { get; }

    /// <summary>
    /// 获取诊断消息中使用的成员种类描述，例如"依赖属性"或"路由事件"。
    /// </summary>
    protected abstract string MemberKindDescription { get; }

    /// <summary>
    /// 获取诊断消息中建议使用的附加版本特性名，例如 <c>AttachedDependencyProperty</c>。
    /// </summary>
    protected abstract string AttachedAttributeName { get; }

    /// <summary>
    /// 从类上下文解析生成器数据。
    /// </summary>
    /// <param name="classContext">类上下文。</param>
    /// <returns>解析后的生成器数据。</returns>
    protected abstract T ParseData(ClassContext classContext);

    /// <inheritdoc/>
    /// <remarks>
    /// 文件名带生成器标识：依赖属性与路由事件可能落在同一个类上，
    /// 而 hint name 必须全局唯一。不带标识时两者都产出 <c>{类}.{名称}.g.cs</c>，
    /// Roslyn 只保留一个文件，另一个成员静默丢失。
    /// </remarks>
    protected override string CreateFileName((ClassData Class, T Data, string Version) context) =>
        $"{context.Class.HintName}.{context.Data.Name}.{GeneratorName}.g.cs";

    /// <inheritdoc/>
    protected override (ClassData Class, T Data, string Version)? PrepareData((ClassContext, string) context)
    {
        var (classContext, version) = context;
        if (classContext.Attributes.FirstOrDefault() is null)
        {
            return null;
        }

        // 参数非法或类型未声明 partial 时不生成代码：
        // 前者会产出无法编译的源文件，后者会与编译器的 CS0260 重复报错。
        if (!GenerationPreconditions.IsSatisfied(classContext))
        {
            return null;
        }

        // 非附加版本生成实例成员，静态类无法承载（CS0708/CS1057）。
        // 附加版本只生成静态方法，静态类在编译层面合法，因此只在非附加版本里检查。
        if (GeneratesInstanceMembers && HostTypeDiagnostics.IsInvalidInstanceMemberHost(classContext.ClassSymbol))
        {
            return null;
        }

        return (classContext.ClassSymbol.GetClassData(), ParseData(classContext), version);
    }

    /// <inheritdoc/>
    protected override void ReportDiagnostics(SourceProductionContext context, ClassContext classContext)
    {
        PartialTypeDiagnostics.Report(context, classContext);
        AttributeArgumentDiagnostics.Report(context, classContext);
        ReportInvalidHostType(context, classContext);
    }

    /// <summary>
    /// 报告不合法的宿主类型（静态类或结构体），替代生成无法编译的代码。
    /// </summary>
    /// <param name="context">源生成上下文。</param>
    /// <param name="classContext">类上下文。</param>
    private void ReportInvalidHostType(SourceProductionContext context, ClassContext classContext)
    {
        if (!GeneratesInstanceMembers
            || !AttributeDeduplication.IsFirstOfType(classContext)
            || !HostTypeDiagnostics.IsInvalidInstanceMemberHost(classContext.ClassSymbol))
        {
            return;
        }

        var reason = classContext.ClassSymbol.IsStatic ? "是静态类" : "是结构体";

        context.ReportDiagnostic(
            Diagnostic.Create(
                DiagnosticDescriptors.InstanceMemberHostMustNotBeStatic,
                classContext.ClassSyntax.Identifier.GetLocation(),
                MemberKindDescription,
                classContext.ClassSymbol.Name,
                reason,
                AttachedAttributeName
            )
        );
    }
}
