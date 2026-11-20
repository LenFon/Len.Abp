using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis;

/// <summary>
/// <see cref="AttributeData"/> 与 <see cref="AttributeSyntax"/> 的扩展方法。
/// </summary>
public static class AttributeDataExtensions
{
    /// <summary>
    /// 获取特性泛型类型参数中指定位置的类型。
    /// </summary>
    /// <param name="attributeData">特性数据。</param>
    /// <param name="position">类型参数的位置。</param>
    /// <returns>指定位置的类型参数；不存在时返回 null。</returns>
    public static ITypeSymbol? GetGenericTypeArgument(this AttributeData attributeData, int position)
    {
        attributeData = attributeData ?? throw new ArgumentNullException(nameof(attributeData));

        return attributeData.AttributeClass?.TypeArguments.ElementAtOrDefault(position);
    }

    /// <summary>
    /// 获取特性的类型参数：优先取泛型类型参数，回退到指定位置的构造函数参数。
    /// </summary>
    /// <param name="attributeData">特性数据。</param>
    /// <param name="genericPosition">泛型类型参数的位置。</param>
    /// <param name="constructorPosition">构造函数参数的位置。</param>
    /// <returns>类型参数；两处都不存在时返回 null。</returns>
    /// <remarks>
    /// 生成器特性都有泛型与非泛型两种形态（如 <c>ViewAttribute&lt;T&gt;</c> 与
    /// <c>ViewAttribute(Type)</c>），类型参数的位置随形态不同，因此需要两处都查。
    /// </remarks>
    public static ITypeSymbol? GetTypeArgument(
        this AttributeData attributeData,
        int genericPosition,
        int constructorPosition
    )
    {
        attributeData = attributeData ?? throw new ArgumentNullException(nameof(attributeData));

        return attributeData.GetGenericTypeArgument(genericPosition)
            ?? attributeData.ConstructorArguments.ElementAtOrDefault(constructorPosition).Value as ITypeSymbol;
    }

    /// <summary>
    /// 获取特性指定名称的命名参数值。
    /// </summary>
    /// <param name="attributeData">特性数据。</param>
    /// <param name="name">命名参数的名称。</param>
    /// <returns>命名参数的值；未设置时返回默认的 <see cref="TypedConstant"/>。</returns>
    public static TypedConstant GetNamedArgument(this AttributeData attributeData, string name)
    {
        attributeData = attributeData ?? throw new ArgumentNullException(nameof(attributeData));
        return attributeData.NamedArguments.FirstOrDefault(pair => pair.Key == name).Value;
    }

    /// <summary>
    /// 获取特性构造函数的第一个字符串参数，即生成器特性的成员名称。
    /// </summary>
    /// <param name="attributeData">特性数据。</param>
    /// <returns>名称；该特性没有名称参数时返回 <see langword="null"/>。</returns>
    /// <remarks>
    /// 依赖属性、路由事件及其附加版本都把名称放在构造函数的第一个参数上。
    /// 集中在这里读取，避免各生成器与诊断各写一遍取值逻辑，判定口径出现分歧。
    /// </remarks>
    public static string? GetNameArgument(this AttributeData attributeData)
    {
        attributeData = attributeData ?? throw new ArgumentNullException(nameof(attributeData));

        return attributeData.ConstructorArguments.Length > 0
            && attributeData.ConstructorArguments[0].Kind == TypedConstantKind.Primitive
            && attributeData.ConstructorArguments[0].Value is string name
                ? name
                : null;
    }

    /// <summary>
    /// 获取特性指定名称的命名参数的字符串值。
    /// </summary>
    /// <param name="attributeData">特性数据。</param>
    /// <param name="name">命名参数的名称。</param>
    /// <returns>命名参数值的字符串形式；未设置时返回 <see langword="null"/>。</returns>
    public static string? GetNamedArgumentString(this AttributeData attributeData, string name) =>
        attributeData.GetNamedArgument(name).Value?.ToString();

    /// <summary>
    /// 获取特性语法中指定名称的命名参数的原始表达式文本。
    /// </summary>
    /// <param name="attributeSyntax">特性语法节点。</param>
    /// <param name="name">命名参数的名称。</param>
    /// <returns>命名参数的表达式文本；未找到时返回 null。</returns>
    public static string? GetNamedArgumentExpression(this AttributeSyntax attributeSyntax, string name)
    {
        attributeSyntax = attributeSyntax ?? throw new ArgumentNullException(nameof(attributeSyntax));

        return attributeSyntax
            .ArgumentList?.Arguments.FirstOrDefault(x =>
            {
                var nameEquals = x.NameEquals?.ToFullString().Trim('=', ' ', '\t', '\r', '\n');

                return nameEquals == name;
            })
            ?.Expression.ToFullString();
    }

    /// <summary>
    /// 获取特性数据对应的语法节点。
    /// </summary>
    /// <param name="attributeData">特性数据。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>特性的语法节点；无法获取时返回 null。</returns>
    public static AttributeSyntax? GetSyntax(this AttributeData attributeData, CancellationToken cancellationToken = default) =>
        attributeData.ApplicationSyntaxReference?.GetSyntax(cancellationToken) as AttributeSyntax;
}
