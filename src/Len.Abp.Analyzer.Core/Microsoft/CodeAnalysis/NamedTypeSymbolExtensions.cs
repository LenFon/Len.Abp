using Len.Abp.Analyzer;

namespace Microsoft.CodeAnalysis;

public static class NamedTypeSymbolExtensions
{
    public static ClassData GetClassData(this INamedTypeSymbol classSymbol, Framework framework, string version)
    {
        classSymbol = classSymbol ?? throw new ArgumentNullException(nameof(classSymbol));

        var fullClassName = classSymbol.ToString() ?? string.Empty;
        var type = classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var @namespace = fullClassName[..fullClassName.LastIndexOf('.')];
        var className = fullClassName[(fullClassName.LastIndexOf('.') + 1)..];
        var isStaticClass = classSymbol.IsStatic;
        var classModifiers = classSymbol.IsStatic ? "public static " : string.Empty;
        var methods = classSymbol
            .GetMembers()
            .OfType<IMethodSymbol>()
            .Select(static value => value.ToDisplayString().Replace("?", string.Empty).TrimStart('.'))
            .ToArray();

        return new ClassData(
            Namespace: @namespace,
            Name: className,
            FullName: fullClassName,
            Type: type,
            Modifiers: classModifiers,
            Version: version,
            IsStatic: isStaticClass,
            Framework: framework,
            Methods: [.. methods]
        );
    }
}