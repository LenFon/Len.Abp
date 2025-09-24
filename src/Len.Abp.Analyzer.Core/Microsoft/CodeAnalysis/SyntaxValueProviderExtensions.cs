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
    /// <param name="fullyQualifiedMetadataName">The fully qualified metadata name of the attribute. </param>
    /// <returns></returns>
    public static IncrementalValuesProvider<GeneratorAttributeSyntaxContext> ForAttributeWithMetadataNameOfClassesAndRecords(
        this SyntaxValueProvider source,
        string fullyQualifiedMetadataName)
    {
        return source.ForAttributeWithMetadataName(
            fullyQualifiedMetadataName: fullyQualifiedMetadataName,
            predicate: static (node, _) =>
                node is ClassDeclarationSyntax
                {
                    AttributeLists.Count: > 0,
                } or RecordDeclarationSyntax
                {
                    AttributeLists.Count: > 0,
                },
            transform: static (context, _) => context);
    }
}