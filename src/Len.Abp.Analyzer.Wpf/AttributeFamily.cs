using Microsoft.CodeAnalysis;

namespace Len.Abp.Analyzer;

/// <summary>
/// 生成器特性族的识别：族名、附加版本判定与类型参数读取。
/// </summary>
/// <remarks>
/// <para>
/// 每个生成器特性都有四种形态：<c>XxxAttribute</c>、<c>XxxAttribute&lt;T&gt;</c>、
/// <c>AttachedXxxAttribute</c> 与 <c>AttachedXxxAttribute&lt;T&gt;</c>。
/// 校验规则按"族"而不是按具体类型定义——附加版本与非附加版本生成同一套成员空间，
/// 规则必须一致，否则同一份声明换个写法就绕过校验。
/// </para>
/// <para>
/// 集中在这里而不是散落在各诊断里：新增一个特性族时只需在这里补一条映射，
/// 不必在多个诊断方法里各改一处。
/// </para>
/// </remarks>
internal static class AttributeFamily
{
    /// <summary>
    /// 依赖属性族的族名。
    /// </summary>
    public const string DependencyProperty = "DependencyPropertyAttribute";

    /// <summary>
    /// 路由事件族的族名。
    /// </summary>
    public const string RoutedEvent = "RoutedEventAttribute";

    /// <summary>
    /// 去掉 <c>Attached</c> 前缀与泛型参数后取特性族名。
    /// </summary>
    /// <param name="attributeClass">特性类型。</param>
    /// <returns>族名；不是生成器特性时返回 <see langword="null"/>。</returns>
    /// <remarks>
    /// 泛型版本的元数据名带反引号（<c>DependencyPropertyAttribute`1</c>），
    /// 用前缀匹配同时覆盖两者。
    /// </remarks>
    public static string? GetFamilyName(INamedTypeSymbol? attributeClass)
    {
        var name = attributeClass?.Name;

        if (name is null)
        {
            return null;
        }

        if (name.StartsWith("Attached", StringComparison.Ordinal))
        {
            name = name["Attached".Length..];
        }

        if (name.StartsWith(DependencyProperty, StringComparison.Ordinal))
        {
            return DependencyProperty;
        }

        return name.StartsWith(RoutedEvent, StringComparison.Ordinal) ? RoutedEvent : null;
    }

    /// <summary>
    /// 判断特性是否为附加版本。
    /// </summary>
    /// <param name="attributeClass">特性类型。</param>
    /// <returns>是附加版本时返回 <see langword="true"/>。</returns>
    public static bool IsAttached(INamedTypeSymbol? attributeClass) =>
        attributeClass?.Name.StartsWith("Attached", StringComparison.Ordinal) == true;

    /// <summary>
    /// 判断特性是否为指定类型（含泛型版本）。
    /// </summary>
    /// <param name="attribute">特性数据。</param>
    /// <param name="name">非泛型特性名，例如 <c>ViewAttribute</c>。</param>
    /// <returns>匹配时返回 <see langword="true"/>。</returns>
    /// <remarks>
    /// 泛型版本的元数据名带反引号（<c>ViewAttribute`1</c>），用前缀匹配同时覆盖两者。
    /// 注意本方法<b>不</b>覆盖附加版本，需要跨族判断时用 <see cref="IsFamily"/>。
    /// </remarks>
    public static bool Is(AttributeData attribute, string name) =>
        attribute.AttributeClass?.Name.StartsWith(name, StringComparison.Ordinal) == true;

    /// <summary>
    /// 判断特性是否属于指定族（含泛型版本与附加版本）。
    /// </summary>
    /// <param name="attribute">特性数据。</param>
    /// <param name="familyName">族名，例如 <see cref="RoutedEvent"/>。</param>
    /// <returns>属于该族时返回 <see langword="true"/>。</returns>
    /// <remarks>
    /// 附加版本（<c>AttachedRoutedEventAttribute</c>）与实例版本生成同一套成员空间，
    /// 校验规则必须一致。按前缀直接比较会漏掉附加版本，因此复用
    /// <see cref="GetFamilyName"/>——它会先剥离 <c>Attached</c> 前缀。
    /// </remarks>
    public static bool IsFamily(AttributeData attribute, string familyName) =>
        GetFamilyName(attribute.AttributeClass) == familyName;

    /// <summary>
    /// 读取路由事件特性的处理程序类型。
    /// </summary>
    /// <param name="attribute">路由事件特性数据。</param>
    /// <returns>处理程序类型；未指定时返回 <see langword="null"/>。</returns>
    /// <remarks>
    /// 泛型版本把委托类型放在类型参数上，非泛型版本放在第三个构造参数上。
    /// </remarks>
    public static ITypeSymbol? GetHandlerType(AttributeData attribute) => attribute.GetTypeArgument(0, 2);

    /// <summary>
    /// 判断类型是否为委托。
    /// </summary>
    /// <param name="type">待判断的类型。</param>
    /// <returns>是委托时返回 <see langword="true"/>。</returns>
    public static bool IsDelegate(ITypeSymbol type) => type.TypeKind == TypeKind.Delegate;

    /// <summary>
    /// 判断类型是否派生自 <c>System.Windows.RoutedEventArgs</c>。
    /// </summary>
    /// <param name="type">待判断的类型。</param>
    /// <returns>派生自该类型时返回 <see langword="true"/>。</returns>
    /// <remarks>
    /// <para>
    /// 按元数据名与命名空间判断，不引用 WPF 程序集——生成器只依赖
    /// <c>Microsoft.CodeAnalysis</c>，不能引用 <c>PresentationCore</c>。
    /// </para>
    /// <para>
    /// 类型链上没有任何匹配即返回 <see langword="false"/>：解析不到该类型时同样如此，
    /// 由调用方报告诊断，而不是静默放行。
    /// </para>
    /// </remarks>
    public static bool IsRoutedEventArgs(ITypeSymbol type)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (current is { Name: "RoutedEventArgs", ContainingNamespace: { } ns }
                && ns.ToDisplayString() == "System.Windows")
            {
                return true;
            }
        }

        return false;
    }
}
