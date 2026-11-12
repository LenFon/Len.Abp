using Len.Abp.Analyzer;

namespace Microsoft.CodeAnalysis;

/// <summary>
/// <see cref="INamedTypeSymbol"/> 的扩展方法。
/// </summary>
public static class NamedTypeSymbolExtensions
{
    /// <summary>
    /// 从类型符号提取生成器所需的类数据。
    /// </summary>
    /// <param name="classSymbol">类的类型符号。</param>
    /// <returns>提取出的类数据。</returns>
    public static ClassData GetClassData(this INamedTypeSymbol classSymbol)
    {
        classSymbol = classSymbol ?? throw new ArgumentNullException(nameof(classSymbol));

        // 嵌套类型的外层类型必须逐层展开成 partial 声明，不能并入命名空间。
        // 外层可能是类、记录或结构体，声明时必须保留原有关键字，否则会生成冲突的类型。
        var containingTypes = new List<string>();
        for (
            var containingType = classSymbol.ContainingType;
            containingType is not null;
            containingType = containingType.ContainingType
        )
        {
            var keyword = containingType switch
            {
                { IsRecord: true } => "record",
                { TypeKind: TypeKind.Struct } => "struct",
                _ => "class",
            };

            // 静态外层类同样要保留 static，且修饰符顺序必须是 `static partial`。
            var modifier = containingType.IsStatic ? "static " : string.Empty;

            containingTypes.Insert(
                0,
                $"{modifier}partial {keyword} {containingType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}"
            );
        }

        var methodSymbols = classSymbol.GetMembers().OfType<IMethodSymbol>().ToArray();
        var methods = methodSymbols
            .Select(static value => value.ToDisplayString().Replace("?", string.Empty).TrimStart('.'))
            .ToArray();
        var methodNames = methodSymbols.Select(static value => value.Name).ToArray();

        // 编译器合成的无参构造函数不在 GetMembers 中，因此这里看到的都是显式声明。
        var constructors = classSymbol.InstanceConstructors
            .Where(static constructor => !constructor.IsImplicitlyDeclared)
            .ToArray();

        return new ClassData(
            Namespace: classSymbol.ContainingNamespace?.IsGlobalNamespace == false
                ? classSymbol.ContainingNamespace.ToDisplayString()
                : string.Empty,
            Name: classSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
            SimpleName: classSymbol.Name,
            FullName: classSymbol.ToDisplayString(),
            Type: classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            Methods: [.. methods],
            MethodNames: [.. methodNames],
            ContainingTypes: containingTypes,
            IsRecord: classSymbol.IsRecord,
            HasAnyConstructor: constructors.Length > 0,
            HasParameterlessConstructor: constructors.Any(static constructor => constructor.Parameters.Length == 0),
            IsStatic: classSymbol.IsStatic
        );
    }
}
