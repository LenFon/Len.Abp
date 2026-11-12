using Len.Abp.Analyzer;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis;

/// <summary>
/// <see cref="SyntaxValueProvider"/> 的扩展方法。
/// </summary>
public static class SyntaxValueProviderExtensions
{
    /// <summary>
    /// 查找所有应用了指定特性的类和记录。
    /// </summary>
    /// <param name="source">语法值提供程序。</param>
    /// <param name="attributeFullyName">特性的完全限定元数据名称。</param>
    /// <returns>匹配的类上下文集合。</returns>
    public static IncrementalValuesProvider<ClassContext> GetClasses(this SyntaxValueProvider source, string attributeFullyName)
    {
        return source
            .ForAttributeWithMetadataName(
                fullyQualifiedMetadataName: attributeFullyName,
                predicate: static (node, _) =>
                    node is TypeDeclarationSyntax { AttributeLists.Count: > 0, },
                transform: static (context, _) =>
                    context
                        .Attributes.Where(w => w.GetSyntax() is not null)
                        .Select(s => new ClassContext(
                            SemanticModel: context.SemanticModel,
                            Attributes: [s],
                            ClassSyntax: (TypeDeclarationSyntax)context.TargetNode,
                            ClassSymbol: (INamedTypeSymbol)context.TargetSymbol
                        ))
            )
            .SelectMany((s, _) => s);
    }
}