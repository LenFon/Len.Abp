using Microsoft.CodeAnalysis;

namespace Len.Abp.Analyzer;

/// <summary>
/// 判断当前特性是否为该类型上第一个匹配的特性。
/// </summary>
/// <remarks>
/// <c>GetClasses</c> 用 <c>SelectMany</c> 为每个特性产出一个 <see cref="ClassContext"/>，
/// 因此诊断检查是<b>按特性</b>调用的：一个类上标了 3 个 <c>[DependencyProperty]</c> 会进来 3 次。
/// 需要"每个类型只报告一次"的检查用本类去重。
/// </remarks>
internal static class AttributeDeduplication
{
    /// <summary>
    /// 判断当前特性是否为该类型上第一个匹配的特性。
    /// </summary>
    /// <param name="classContext">类上下文。</param>
    /// <returns>是第一个时返回 <see langword="true"/>。</returns>
    /// <remarks>
    /// 用引用相等比较 <see cref="AttributeData"/> 实例，避免依赖名称或顺序。
    /// </remarks>
    public static bool IsFirstOfType(ClassContext classContext) =>
        IsFirst(
            classContext,
            static (candidate, current) =>
                SymbolEqualityComparer.Default.Equals(candidate.AttributeClass, current.AttributeClass)
        );

    /// <summary>
    /// 判断当前特性是否为该类型上第一个生成同名成员的特性。
    /// </summary>
    /// <param name="classContext">类上下文。</param>
    /// <param name="name">当前特性的成员名称。</param>
    /// <returns>是第一个时返回 <see langword="true"/>。</returns>
    /// <remarks>
    /// 依赖属性与路由事件会生成同名成员并互相冲突，此时两者都要报告，
    /// 但同一个问题只应出现一次。按"成员名"而不是"特性类"去重，
    /// 才能让跨特性类的冲突只报一条。
    /// </remarks>
    public static bool IsFirstWithName(ClassContext classContext, string name) =>
        IsFirst(
            classContext,
            (candidate, _) =>
                string.Equals(candidate.GetNameArgument(), name, StringComparison.OrdinalIgnoreCase)
        );

    /// <summary>
    /// 判断当前特性是否为该类型上第一个视图特性。
    /// </summary>
    /// <param name="classContext">类上下文。</param>
    /// <returns>是第一个时返回 <see langword="true"/>。</returns>
    /// <remarks>
    /// <c>ViewAttribute</c> 与 <c>ViewAttribute&lt;T&gt;</c> 是两个不同的特性类，
    /// 按特性类去重会让两条生成管线各报一次。这里按"特性族"去重：
    /// 两种形态都算视图特性，只有第一个命中的那条管线负责报告。
    /// </remarks>
    public static bool IsFirstViewAttribute(ClassContext classContext) =>
        IsFirst(classContext, static (candidate, _) => IsViewAttribute(candidate.AttributeClass));

    /// <summary>
    /// 判断当前特性是否为该类型上第一个满足条件的特性。
    /// </summary>
    /// <param name="classContext">类上下文。</param>
    /// <param name="matches">判断候选特性是否与当前特性属于同一去重维度。</param>
    /// <returns>是第一个时返回 <see langword="true"/>。</returns>
    /// <remarks>
    /// 三种去重维度的差别只在"什么算同一个特性"，其余流程完全一致：
    /// 取当前特性、在类型上按同一谓词找第一个、用引用相等确认就是它。
    /// 用引用相等而不是名称或顺序，避免依赖 <c>GetAttributes</c> 的返回顺序。
    /// </remarks>
    private static bool IsFirst(
        ClassContext classContext,
        Func<AttributeData, AttributeData, bool> matches
    )
    {
        if (classContext.Attributes.FirstOrDefault() is not { } current)
        {
            return false;
        }

        var first = classContext.ClassSymbol
            .GetAttributes()
            .FirstOrDefault(candidate => matches(candidate, current));

        return ReferenceEquals(first, current);
    }

    /// <summary>
    /// 判断特性是否为视图特性（含泛型与非泛型两种形态）。
    /// </summary>
    /// <param name="attributeClass">特性类型。</param>
    /// <returns>是视图特性时返回 <see langword="true"/>。</returns>
    /// <remarks>
    /// 两种形态的简单名都是 <c>ViewAttribute</c>，只有元数据名带反引号区分。
    /// 必须同时校验命名空间，避免误判使用者自定义的同名特性。
    /// </remarks>
    private static bool IsViewAttribute(INamedTypeSymbol? attributeClass) =>
        attributeClass is { MetadataName: "ViewAttribute" or "ViewAttribute`1" }
        && attributeClass.ContainingNamespace?.ToDisplayString() == "Len.Abp.Wpf.Mvvm";
}
