namespace Microsoft.CodeAnalysis;

public static class TypeSymbolExtensions
{
    public static bool? IsSpecialType(this ITypeSymbol? symbol)
    {
        if (symbol == null) return null;

        return
            symbol is IArrayTypeSymbol ||
            symbol.SpecialType != SpecialType.None ||
            (symbol.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T &&
             symbol.BaseType != null &&
             symbol.BaseType.SpecialType != SpecialType.None);
    }
}