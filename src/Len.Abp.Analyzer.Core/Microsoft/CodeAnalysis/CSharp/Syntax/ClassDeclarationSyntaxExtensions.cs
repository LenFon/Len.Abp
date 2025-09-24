using Len.Abp.Analyzer;

namespace Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>
/// Extensions for <see cref="ClassDeclarationSyntax"/>.
/// </summary>
public static class ClassDeclarationSyntaxExtensions
{
    /// <summary>
    /// Tries to find the attribute syntax in the class declaration.
    /// </summary>
    /// <param name="classSyntax">The class declaration.</param>
    /// <param name="attribute">The attribute data to find. The first constructor argument is used as the name to match.</param>
    /// <returns></returns>
    public static AttributeSyntax? TryFindAttributeSyntax(this ClassDeclarationSyntax classSyntax, AttributeData attribute)
    {
        var name = attribute.ConstructorArguments.ElementAtOrDefault(0).Value?.ToString();

        return classSyntax.AttributeLists
            .SelectMany(static s => s.Attributes)
            .FirstOrDefault(w => w.ArgumentList?.Arguments.FirstOrDefault()?.ToString().Trim('"').RemoveNameof() == name);
    }
}