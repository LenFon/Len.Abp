namespace Microsoft.CodeAnalysis;

/// <summary>
/// <see cref="ITypeSymbol"/> 的扩展方法。
/// </summary>
public static class TypeSymbolExtensions
{
    /// <summary>
    /// 判断类型是否为特殊类型，包括数组、BCL 内置类型以及基础类型为内置类型的可空类型。
    /// </summary>
    /// <param name="symbol">要判断的类型符号。</param>
    /// <returns>是特殊类型返回 true，否则返回 false；符号为 null 时返回 null。</returns>
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