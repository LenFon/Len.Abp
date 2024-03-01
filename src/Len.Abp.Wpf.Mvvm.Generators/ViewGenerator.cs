using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;
using System.Threading;
using System.Collections.Immutable;

namespace Len.Abp.Wpf.Mvvm.Generators;

[Generator]
public class ViewGenerator : IIncrementalGenerator
{
    private const string _viewAttributeName = "Len.Abp.Wpf.Mvvm.ViewAttribute";
    private static readonly string _version = System
        .Reflection.Assembly.GetExecutingAssembly()
        .GetName()
        .Version.ToString(3);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        //Debugger.Launch();
        var viewInfos = context
            .SyntaxProvider.ForAttributeWithMetadataName(_viewAttributeName, CouldBeView, GetViewInfoOrNull)
            .Where(static w => w is not null)
            .Select(static (s, _) => s!.Value)
            .Collect();

        context.RegisterSourceOutput(viewInfos, GenerateCode);
    }

    private void GenerateCode(SourceProductionContext context, ImmutableArray<ViewInfo> viewInfos)
    {
        context.CancellationToken.ThrowIfCancellationRequested();

        ViewCodeGenerator.Instance.Excute(viewInfos, context, _version);
    }

    private static bool CouldBeView(SyntaxNode syntaxNode, CancellationToken cancellationToken)
    {
        if (
            syntaxNode
            is not ClassDeclarationSyntax
            {
                TypeParameterList: null, //非泛型
                Parent: BaseNamespaceDeclarationSyntax, //非嵌套类型，并且有命名空间
                Modifiers: var modifiers and not [],
            }
        )
        {
            return false;
        }

        if (!modifiers.Any(SyntaxKind.PartialKeyword) || modifiers.Any(SyntaxKind.AbstractKeyword))
        {
            return false;
        }

        return true;
    }

    private static ViewInfo? GetViewInfoOrNull(
        GeneratorAttributeSyntaxContext context,
        CancellationToken cancellationToken
    )
    {
        if (context.TargetSymbol is INamedTypeSymbol symbol)
        {
            var viewAttribute = context.Attributes.FirstOrDefault(w => w.AttributeClass?.ToDisplayString() == _viewAttributeName);
            var viewModelArg = viewAttribute?.NamedArguments.FirstOrDefault(w => w.Key == "ViewModel");
            var lifetimeArg = viewAttribute?.NamedArguments.FirstOrDefault(w => w.Key == "Lifetime");

            return new(symbol, viewModelArg?.Value.Value?.ToString(), (int?)lifetimeArg?.Value.Value);
        }

        return default;
    }
}
