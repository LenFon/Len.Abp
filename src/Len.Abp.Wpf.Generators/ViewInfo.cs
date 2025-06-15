using Microsoft.CodeAnalysis;

internal readonly record struct ViewInfo
{
    public ViewInfo(ITypeSymbol type, string viewModelFullName, string? viewName, bool? isShell)
    {
        FullyQualifiedNamespace = type.ContainingNamespace.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        Namespace = FullyQualifiedNamespace["global::".Length..];
        Name = type.Name;
        FullyQualifiedName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        FullName = type.ToDisplayString();
        ViewModelFullyQualifiedName = $"global::{viewModelFullName}";
        ViewName = viewName ?? FullName;
        IsShell = isShell ?? false;
    }

    /// <summary>
    /// The namespace of a strongly typed id.
    /// </summary>
    public string Namespace { get; }

    /// <summary>
    /// The fully qualified namespace of a strongly typed id.
    /// </summary>
    public string FullyQualifiedNamespace { get; }

    /// <summary>
    /// The name of a strongly typed id.
    /// </summary>
    /// <remarks>
    /// This is a short strongly typed id type name that does not include a namespace.
    /// </remarks>
    public string Name { get; }

    /// <summary>
    /// The fully qualified name of a strongly typed id.
    /// </summary>
    public string FullyQualifiedName { get; }

    /// <summary>
    /// The fully qualified name of a strongly typed id.
    /// </summary>
    /// <remarks>
    /// The fully qualified name of a strongly typed id does not include the word “global::”.
    /// </remarks>
    public string FullName { get; }

    /// <summary>
    /// The fully qualified name of the view model associated with a view.
    /// </summary>
    public string ViewModelFullyQualifiedName { get; }

    public string ViewName { get; }

    public bool IsShell { get; }
}