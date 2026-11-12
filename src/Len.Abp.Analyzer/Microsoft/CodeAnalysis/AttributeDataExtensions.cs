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
