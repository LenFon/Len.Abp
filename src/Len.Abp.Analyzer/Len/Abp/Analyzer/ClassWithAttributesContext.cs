using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;

namespace Len.Abp.Analyzer;

/// <summary>
/// 带有特性的类的上下文。
/// </summary>
/// <param name="SemanticModel">与包含类声明的语法树关联的语义模型。</param>
/// <param name="Attributes">应用于该类的特性。</param>
/// <param name="ClassSyntax">表示类型声明的语法节点，可能是类或记录。</param>
/// <param name="ClassSymbol">语义模型中表示该类的符号。</param>
public readonly record struct ClassContext(
    SemanticModel SemanticModel,
    ImmutableArray<AttributeData> Attributes,
    TypeDeclarationSyntax ClassSyntax,
    INamedTypeSymbol ClassSymbol);