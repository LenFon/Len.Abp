using Len.Abp.Analyzer;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis;

/// <summary>
/// Extensions for <see cref="SyntaxValueProvider"/>.
/// </summary>
public static class SyntaxValueProviderExtensions
{
    /// <summary>
    /// Finds all classes and records with the specified attribute.
    /// </summary>
    /// <param name="source">The source <see cref="SyntaxValueProvider"/>. </param>
    /// <param name="attributeFullyName">The fully qualified metadata name of the attribute. </param>
    /// <returns></returns>
    public static IncrementalValuesProvider<ClassContext> GetClasses(this SyntaxValueProvider source, string attributeFullyName)
    {
        return source
            .ForAttributeWithMetadataName(
                fullyQualifiedMetadataName: attributeFullyName,
                predicate: static (node, _) =>
                    node is ClassDeclarationSyntax { AttributeLists.Count: > 0, } or RecordDeclarationSyntax { AttributeLists.Count: > 0, },
                transform: static (context, _) =>
                    context
                        .Attributes.Where(w =>
                        {
                            var classSyntax = (ClassDeclarationSyntax)context.TargetNode;
                            var attributeSyntax = classSyntax.TryFindAttributeSyntax(w);

                            return attributeSyntax != null;
                        })
                        .Select(s => new ClassContext(
                            SemanticModel: context.SemanticModel,
                            Attributes: [s],
                            ClassSyntax: (ClassDeclarationSyntax)context.TargetNode,
                            ClassSymbol: (INamedTypeSymbol)context.TargetSymbol
                        ))
            )
            .SelectMany((s, _) => s);
    }
}