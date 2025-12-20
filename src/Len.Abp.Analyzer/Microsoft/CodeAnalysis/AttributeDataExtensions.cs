using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis;

public static class AttributeDataExtensions
{
    public static ITypeSymbol? GetGenericTypeArgument(this AttributeData attributeData, int position)
    {
        attributeData = attributeData ?? throw new ArgumentNullException(nameof(attributeData));

        return attributeData.AttributeClass?.TypeArguments.ElementAtOrDefault(position);
    }

    public static TypedConstant GetNamedArgument(this AttributeData attributeData, string name)
    {
        attributeData = attributeData ?? throw new ArgumentNullException(nameof(attributeData));
        return attributeData.NamedArguments.FirstOrDefault(pair => pair.Key == name).Value;
    }

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

    public static AttributeSyntax? GetSyntax(this AttributeData attributeData, CancellationToken cancellationToken = default) =>
        attributeData.ApplicationSyntaxReference?.GetSyntax(cancellationToken) as AttributeSyntax;
}