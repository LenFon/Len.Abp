using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;

namespace Len.Abp.Analyzer;

/// <summary>
/// Context for a class with attributes.
/// </summary>
/// <param name="SemanticModel">The semantic model associated with the syntax tree containing the class declaration.</param>
/// <param name="Attributes">The attributes applied to the class.</param>
/// <param name="ClassSyntax">The syntax node representing the class declaration.</param>
/// <param name="ClassSymbol">The symbol representing the class in the semantic model.</param>
public readonly record struct ClassContext(
    SemanticModel SemanticModel,
    ImmutableArray<AttributeData> Attributes,
    ClassDeclarationSyntax ClassSyntax,
    INamedTypeSymbol ClassSymbol);