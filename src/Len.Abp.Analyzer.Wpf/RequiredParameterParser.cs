using Microsoft.CodeAnalysis;

namespace Len.Abp.Analyzer;

/// <summary>
/// 解析 <c>ViewAttribute.RequiredParameters</c> 中的 <c>"名称"</c> 与 <c>"名称:类型"</c> 两种写法。
/// </summary>
/// <remarks>
/// 无法解析的类型名会退化为"只校验存在性"（类型为 null），
/// 而不是让整个生成过程失败——参数名仍然有效，只是少了类型校验。
/// </remarks>
internal static class RequiredParameterParser
{
    /// <summary>
    /// 解析必需参数声明。
    /// </summary>
    /// <param name="attribute">视图特性数据。</param>
    /// <param name="semanticModel">语义模型，用于解析类型名。</param>
    /// <param name="classSymbol">视图类型符号。</param>
    /// <returns>解析后的必需参数集合；无声明时返回空集合。</returns>
    public static IReadOnlyList<RequiredParameter> Parse(
        AttributeData attribute,
        SemanticModel semanticModel,
        INamedTypeSymbol classSymbol
    )
    {
        var constant = attribute.GetNamedArgument("RequiredParameters");
        if (constant.IsNull || constant.Kind != TypedConstantKind.Array)
        {
            return [];
        }

        var parameters = new List<RequiredParameter>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in constant.Values)
        {
            if (item.Value?.ToString() is not { Length: > 0 } raw || string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            var separatorIndex = raw.IndexOf(":", StringComparison.Ordinal);
            var name = (separatorIndex < 0 ? raw : raw[..separatorIndex]).Trim();
            var typeName = separatorIndex < 0 ? null : raw[(separatorIndex + 1)..].Trim();

            if (string.IsNullOrEmpty(name) || !seen.Add(name))
            {
                continue;
            }

            parameters.Add(
                new RequiredParameter(name, ResolveType(typeName, semanticModel, classSymbol), typeName)
            );
        }

        return parameters;
    }

    /// <summary>
    /// 把类型名解析为可直接写入生成代码的完全限定类型名。
    /// </summary>
    /// <param name="typeName">类型名；为 null 或空时表示不校验类型。</param>
    /// <param name="semanticModel">语义模型。</param>
    /// <param name="classSymbol">视图类型符号，用于解析相对类型名。</param>
    /// <returns>完全限定的类型名；无法解析时返回 null。</returns>
    private static string? ResolveType(string? typeName, SemanticModel semanticModel, INamedTypeSymbol classSymbol)
    {
        if (typeName is null || string.IsNullOrWhiteSpace(typeName))
        {
            return null;
        }

        // 去掉可空引用类型标记：string? 与 string 在运行时是同一个 Type。
        var normalized = typeName.TrimEnd('?');

        var symbol = ResolveSymbol(normalized, semanticModel.Compilation, classSymbol);

        // 解析失败时退化为"只校验存在性"，而不是生成 typeof() 这种非法代码。
        if (symbol is null)
        {
            return null;
        }

        var display = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        // 可空值类型（int?）需要保留 Nullable<T>，否则类型校验会漏掉 null 的情况。
        // 按解析出的符号判定而不是按类型名清单：清单只能覆盖常用别名，
        // 使用者写 System.DateTime? 或自定义结构体时会被当成引用类型而丢掉可空性。
        // 引用类型的 ? 在运行时与不带 ? 完全相同，因此只需处理值类型。
        return typeName.EndsWith("?", StringComparison.Ordinal) && symbol.IsValueType
            ? $"global::System.Nullable<{display}>"
            : display;
    }

    /// <summary>
    /// 把类型名解析为符号，支持关键字别名、完全限定名与嵌套类型名。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 不用 <c>GetSpeculativeTypeInfo</c>：它需要真实的语法位置，
    /// 在生成器里用位置 0 解析不到用户命名空间中的类型，且无法解析时返回 error type
    /// 而非 null，会产出 <c>typeof()</c> 这种非法代码。
    /// </para>
    /// <para>
    /// <b>嵌套类型必须按 <c>+</c> 拼接元数据名</b>：<c>GetTypeByMetadataName</c> 只认
    /// 元数据形式（<c>Demo.Outer+Mode</c>），而使用者写的是源码形式（<c>Demo.Outer.Mode</c>）。
    /// 直接拿点号形式去查会返回 null，连完全限定名也解析不了，
    /// 于是静默退化成"只校验存在性"——类型写错和使用者不写类型看起来一模一样。
    /// </para>
    /// </remarks>
    private static ITypeSymbol? ResolveSymbol(string typeName, Compilation compilation, INamedTypeSymbol classSymbol)
    {
        var specialType = typeName switch
        {
            "int" => SpecialType.System_Int32,
            "long" => SpecialType.System_Int64,
            "short" => SpecialType.System_Int16,
            "byte" => SpecialType.System_Byte,
            "sbyte" => SpecialType.System_SByte,
            "uint" => SpecialType.System_UInt32,
            "ulong" => SpecialType.System_UInt64,
            "ushort" => SpecialType.System_UInt16,
            "float" => SpecialType.System_Single,
            "double" => SpecialType.System_Double,
            "decimal" => SpecialType.System_Decimal,
            "bool" => SpecialType.System_Boolean,
            "char" => SpecialType.System_Char,
            "string" => SpecialType.System_String,
            "object" => SpecialType.System_Object,
            _ => SpecialType.None,
        };

        if (specialType != SpecialType.None)
        {
            return compilation.GetSpecialType(specialType);
        }

        // 相对类型名：先按视图所在命名空间拼接，再按视图的外层类型链拼接，
        // 最后按全局命名空间查找。
        var containingNamespace = classSymbol.ContainingNamespace?.ToDisplayString();

        var candidates = new List<string> { typeName };

        if (!string.IsNullOrEmpty(containingNamespace))
        {
            candidates.Add($"{containingNamespace}.{typeName}");
        }

        candidates.Add($"System.{typeName}");

        foreach (var candidate in candidates)
        {
            if (FindType(candidate, compilation) is { } symbol)
            {
                return symbol;
            }
        }

        // 视图嵌套在某个类型里时，使用者通常只写最内层类型名（"Mode"），
        // 而该类型是外层类型的成员。由内向外逐层尝试，最贴近使用者的书写位置。
        // 这里直接拼元数据名而不是复用 FindType：外层类型可能带泛型参数，
        // 显示名会给出 `Outer<T>`，而元数据名需要 `Outer`1`。
        for (var containing = classSymbol; containing is not null; containing = containing.ContainingType)
        {
            var metadataName = GetMetadataName(containing) + "+" + typeName.Replace('.', '+');

            if (compilation.GetTypeByMetadataName(metadataName) is { } nested)
            {
                return nested;
            }
        }

        return null;
    }

    /// <summary>
    /// 取类型的元数据名，嵌套类型用 <c>+</c> 分隔，泛型类型带反引号参数个数。
    /// </summary>
    /// <param name="type">类型符号。</param>
    /// <returns>形如 <c>Demo.Outer`1+Inner</c> 的元数据名。</returns>
    private static string GetMetadataName(INamedTypeSymbol type)
    {
        if (type.ContainingType is { } containing)
        {
            return $"{GetMetadataName(containing)}+{type.MetadataName}";
        }

        return type.ContainingNamespace is { IsGlobalNamespace: false } ns
            ? $"{ns.ToDisplayString()}.{type.MetadataName}"
            : type.MetadataName;
    }

    /// <summary>
    /// 按源码形式的类型名查找符号，逐层尝试把点号换成嵌套类型分隔符。
    /// </summary>
    /// <param name="typeName">源码形式的类型名，例如 <c>Demo.Outer.Mode</c>。</param>
    /// <param name="compilation">编译对象。</param>
    /// <returns>找到的类型符号；找不到时返回 <see langword="null"/>。</returns>
    /// <remarks>
    /// <para>
    /// 源码里的 <c>A.B.C</c> 有两种可能：命名空间 <c>A.B</c> 中的类型 <c>C</c>，
    /// 或类型 <c>A</c> 中的嵌套类型 <c>B.C</c>。元数据名只认后者的 <c>+</c> 形式，
    /// 因此从右往左逐个把点号换成 <c>+</c> 尝试，先命中者胜。
    /// </para>
    /// <para>
    /// 从右往左而不是从左往右：嵌套层级通常比命名空间层级少，
    /// 先试"最内层是嵌套类型"能更早命中，也避免把 <c>System.Windows.Media.Color</c>
    /// 这种纯命名空间路径误拆成嵌套类型。
    /// </para>
    /// </remarks>
    private static ITypeSymbol? FindType(string typeName, Compilation compilation)
    {
        if (compilation.GetTypeByMetadataName(typeName) is { } direct)
        {
            return direct;
        }

        var metadataName = typeName;

        for (var index = metadataName.LastIndexOf('.'); index > 0; index = metadataName.LastIndexOf('.', index - 1))
        {
            metadataName = metadataName[..index] + "+" + metadataName[(index + 1)..];

            if (compilation.GetTypeByMetadataName(metadataName) is { } nested)
            {
                return nested;
            }
        }

        return null;
    }
}
