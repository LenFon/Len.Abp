using Microsoft.CodeAnalysis;

namespace Len.Abp.Analyzer;

/// <summary>
/// 校验生成器特性的参数，非法时报告 <c>LENABP004</c>。
/// </summary>
/// <remarks>
/// <para>
/// 本类只负责编排：决定"要不要检查重复"、把各专项校验的结果汇总成一条诊断。
/// 具体规则分散在 <see cref="MemberNameDiagnostics"/>（标识符与成员冲突）、
/// <see cref="RoutedEventHandlerDiagnostics"/>（处理程序签名）与
/// <see cref="ValidateViewAttribute"/>（视图特性的 ViewName）。
/// </para>
/// <para>
/// 名称重复必须看该类型上的全部同类特性，不能只看当前这一个，
/// 因此去重判断由 <see cref="AttributeDeduplication"/> 提供。
/// </para>
/// </remarks>
internal static class AttributeArgumentDiagnostics
{
    /// <summary>
    /// 检查特性参数是否合法，非法时报告 <c>LENABP004</c>。
    /// </summary>
    /// <param name="context">源生成上下文。</param>
    /// <param name="classContext">类上下文。</param>
    public static void Report(SourceProductionContext context, ClassContext classContext)
    {
        // 重复名称只报一次：否则每个同名特性都会报一条。
        // 按成员名去重而不是按特性类：依赖属性与路由事件同名时两者互相冲突，
        // 但同一个问题只应出现一次。
        var attribute = classContext.Attributes.FirstOrDefault();
        var name = attribute?.GetNameArgument();

        // 视图特性按"特性族"去重：ViewAttribute 与 ViewAttribute<T> 是两个特性类，
        // 按特性类去重会让"同时应用两者"这条诊断报两次。
        var checkDuplicates = attribute is not null && AttributeFamily.Is(attribute, "ViewAttribute")
            ? AttributeDeduplication.IsFirstViewAttribute(classContext)
            : name is null
                ? AttributeDeduplication.IsFirstOfType(classContext)
                : AttributeDeduplication.IsFirstWithName(classContext, name);

        if (GetInvalidArgumentMessage(classContext, checkDuplicates) is not { } message)
        {
            return;
        }

        context.ReportDiagnostic(
            Diagnostic.Create(
                DiagnosticDescriptors.InvalidAttributeArgument,
                GetAttributeLocation(classContext),
                message
            )
        );
    }

    /// <summary>
    /// 判断当前类上下文的特性参数是否合法。
    /// </summary>
    /// <param name="classContext">类上下文。</param>
    /// <returns>合法时返回 <see langword="null"/>；否则返回可读的错误说明。</returns>
    /// <remarks>
    /// 重复名称只在"当前特性是该类型的第一个匹配特性"时判定：
    /// 否则每个同名特性都会报一次，同一个问题重复出现。
    /// </remarks>
    public static string? Validate(ClassContext classContext) => GetInvalidArgumentMessage(classContext, checkDuplicates: true);

    /// <summary>
    /// 计算特性参数的错误说明。
    /// </summary>
    /// <param name="classContext">类上下文。</param>
    /// <param name="checkDuplicates">是否检查名称重复。</param>
    /// <returns>合法时返回 <see langword="null"/>。</returns>
    private static string? GetInvalidArgumentMessage(ClassContext classContext, bool checkDuplicates)
    {
        if (classContext.Attributes.FirstOrDefault() is not { } attribute)
        {
            return null;
        }

        // ViewAttribute 的名称来自 ViewName 命名参数，规则与其它特性不同，单独处理。
        if (AttributeFamily.Is(attribute, "ViewAttribute"))
        {
            // 双特性检查受 checkDuplicates 控制：Validate 传 true 以阻止两条管线生成代码，
            // Report 只在第一条管线报诊断，避免同一个问题出现两次。
            return ValidateViewAttribute(attribute)
                ?? (checkDuplicates ? ValidateSingleViewAttribute(classContext) : null);
        }

        var attributeName = attribute.AttributeClass?.Name ?? "特性";
        var name = attribute.GetNameArgument();

        if (name is not null && string.IsNullOrWhiteSpace(name))
        {
            return $"{attributeName} 的名称不能为空。";
        }

        // 名称会被直接拼进成员声明、支撑字段名与 Register(name: "...") 的字面量。
        // 不是合法标识符时产出的是十余条互相矛盾的语法错误，没有一条指向真正的问题。
        if (name is not null && MemberNameDiagnostics.ValidateIdentifier(name) is { } invalidIdentifier)
        {
            return invalidIdentifier;
        }

        if (checkDuplicates && name is not null && MemberNameDiagnostics.GetDuplicateNameMessage(classContext, name) is { } duplicate)
        {
            return duplicate;
        }

        if (AttributeFamily.IsFamily(attribute, AttributeFamily.RoutedEvent))
        {
            return RoutedEventHandlerDiagnostics.Validate(attribute);
        }

        return null;
    }

    /// <summary>
    /// 校验视图特性：只有显式设成空白的 <c>ViewName</c> 才报错。
    /// </summary>
    /// <param name="attribute">视图特性数据。</param>
    /// <returns>合法时返回 <see langword="null"/>。</returns>
    /// <remarks>
    /// 未设置 <c>ViewName</c> 是合法的（视图不参与导航），因此不能按"名称为空"处理。
    /// </remarks>
    private static string? ValidateViewAttribute(AttributeData attribute)
    {
        var viewName = attribute.GetNamedArgument("ViewName");

        if (!viewName.IsNull && string.IsNullOrWhiteSpace(viewName.Value?.ToString()))
        {
            return $"{attribute.AttributeClass?.Name ?? "ViewAttribute"} 的 ViewName 不能为空。";
        }

        return null;
    }

    /// <summary>
    /// 校验同一类型上只有一个视图特性。
    /// </summary>
    /// <param name="classContext">类上下文。</param>
    /// <returns>合法时返回 <see langword="null"/>。</returns>
    /// <remarks>
    /// <para>
    /// <c>ViewAttribute</c> 与 <c>ViewAttribute&lt;T&gt;</c> 是两个不同的特性类，
    /// <c>AllowMultiple = false</c> 只约束同一个类，因此两者可以同时应用。
    /// </para>
    /// <para>
    /// 这种组合必须拦截而不是生成两份代码：两条管线会产出同名 hintName，
    /// Roslyn 直接抛异常（CS8785），该类型一个源文件都不产出；
    /// 即使能生成，两个 <c>ExposeKeyedService</c> 也会用不同 ViewName 注册同一个类型，
    /// 语义本身就可疑。
    /// </para>
    /// </remarks>
    private static string? ValidateSingleViewAttribute(ClassContext classContext)
    {
        var viewAttributes = classContext.ClassSymbol
            .GetAttributes()
            .Where(candidate => AttributeFamily.Is(candidate, "ViewAttribute"))
            .ToArray();

        if (viewAttributes.Length <= 1)
        {
            return null;
        }

        var names = string.Join(
            "、",
            viewAttributes.Select(candidate =>
                candidate.AttributeClass?.Arity > 0 ? "ViewAttribute<T>" : "ViewAttribute"
            )
        );

        return $"类型 {classContext.ClassSymbol.Name} 上同时应用了 {names}。"
            + "两者会生成同名的源文件与注册特性，无法共存。请只保留一个视图特性。";
    }

    /// <summary>
    /// 取特性在源码中的位置。
    /// </summary>
    /// <param name="classContext">类上下文。</param>
    /// <returns>特性语法位置；取不到时退化为类型标识符。</returns>
    private static Location GetAttributeLocation(ClassContext classContext) =>
        classContext.Attributes.FirstOrDefault()?.ApplicationSyntaxReference?.GetSyntax().GetLocation()
        ?? classContext.ClassSyntax.Identifier.GetLocation();
}
