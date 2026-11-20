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
            // 关键字必须是三态：record struct 同时满足 IsRecord 与 Struct，
            // 只判 IsRecord 会生成 `partial record`，与 `record struct` 冲突（CS0261）。
            var keyword = containingType switch
            {
                { IsRecord: true, TypeKind: TypeKind.Struct } => "record struct",
                { IsRecord: true } => "record",
                { TypeKind: TypeKind.Struct } => "struct",
                _ => "class",
            };

            // 静态外层类同样要保留 static，且修饰符顺序必须是 `static partial`。
            var modifier = containingType.IsStatic ? "static " : string.Empty;

            containingTypes.Insert(0, $"{modifier}partial {keyword} {GetDeclarationName(containingType)}");
        }

        var methodNames = classSymbol
            .GetMembers()
            .OfType<IMethodSymbol>()
            .Select(static value => value.Name)
            .ToArray();

        // 编译器合成的无参构造函数不在 GetMembers 中，因此这里看到的都是显式声明。
        var constructors = classSymbol.InstanceConstructors
            .Where(static constructor => !constructor.IsImplicitlyDeclared)
            .ToArray();

        return new ClassData(
            Namespace: classSymbol.ContainingNamespace?.IsGlobalNamespace == false
                ? classSymbol.ContainingNamespace.ToDisplayString()
                : string.Empty,
            // 必须用未闭合的类型参数：DeclarationName 只用于分部声明，
            // 闭合泛型（PageViewModel<int>）与开放泛型（PageViewModel<>）都不能用于分部声明
            // （CS1001/CS0264）。注册特性里的 typeof() 用 Type（已闭合的完全限定名），
            // 两者不能互换。
            DeclarationName: GetDeclarationName(classSymbol),
            SimpleName: classSymbol.Name,
            FullName: classSymbol.ToDisplayString(),
            Type: classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            MethodNames: [.. methodNames],
            ContainingTypes: containingTypes,
            IsRecord: classSymbol.IsRecord,
            HasAnyConstructor: constructors.Length > 0,
            HasParameterlessConstructor: constructors.Any(static constructor => constructor.Parameters.Length == 0),
            IsStatic: classSymbol.IsStatic,
            // 密封类不能声明新的保护成员（CS0628）。生成器据此把 protected 降级为 public。
            IsSealed: classSymbol.IsSealed
        );
    }

    /// <summary>
    /// 取类型在 partial 声明中使用的名称，泛型类型带未闭合的类型参数列表。
    /// </summary>
    /// <param name="type">待取名的类型。</param>
    /// <returns>形如 <c>Host</c> 或 <c>Host&lt;T&gt;</c> 的名称。</returns>
    /// <remarks>
    /// 不能直接用 <see cref="ISymbol.Name"/>：闭合泛型（<c>Host&lt;int&gt;</c>）的
    /// <c>Name</c> 只有 <c>Host</c>，类型参数会丢失，生成的 partial 声明无法与
    /// 用户声明匹配（CS0264）。也不能用显示名：它给出闭合实参 <c>Host&lt;int&gt;</c>，
    /// 而 partial 声明只能用未闭合的类型参数（CS1001）。
    /// 因此从 <see cref="INamedTypeSymbol.OriginalDefinition"/> 取参数名。
    /// </remarks>
    private static string GetDeclarationName(INamedTypeSymbol type)
    {
        var definition = type.OriginalDefinition;

        if (definition.TypeParameters.Length == 0)
        {
            return definition.Name;
        }

        return $"{definition.Name}<{string.Join(", ", definition.TypeParameters.Select(parameter => parameter.Name))}>";
    }
}
