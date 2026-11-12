using Len.Abp.Analyzer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Net;

/// <summary>
/// 从特性生成 WPF 依赖属性的生成器共享流程。
/// 派生生成器只提供成员声明和回调体，这两部分在普通属性和附加属性之间不同。
/// </summary>
internal abstract class DependencyPropertyGeneratorBase(string id, params HashSet<string> attributeFullyNames)
    : BasicGenerator<DependencyPropertyGeneratorBase.DependencyPropertyData>(id, attributeFullyNames)
{
    /// <summary>
    /// 生成成员上 <c>GeneratedCode</c> 特性所标注的生成器名称。
    /// </summary>
    protected abstract string GeneratorName { get; }

    /// <inheritdoc/>
    protected override string CreateFileName((ClassData Class, DependencyPropertyData Data, string Version) context) =>
        $"{context.Class.HintName}.{context.Data.Name}.g.cs";

    /// <inheritdoc/>
    protected override string CreateSourceCode((ClassData Class, DependencyPropertyData Data, string Version) context) =>
        GenerateDependencyProperty(context.Class, context.Data, context.Version);

    /// <inheritdoc/>
    protected override (ClassData Class, DependencyPropertyData Data, string Version)? PrepareData((ClassContext, string) context)
    {
        var (classContext, version) = context;
        if (classContext.Attributes.FirstOrDefault() is null)
        {
            return null;
        }

        var classData = classContext.ClassSymbol.GetClassData();
        return (classData, classContext, version);
    }

    #region CreateDependencyPropertyData

    /// <summary>
    /// 生成依赖属性所需的全部数据。
    /// </summary>
    /// <param name="Name">属性名称。</param>
    /// <param name="Type">属性的完全限定类型名。</param>
    /// <param name="ShortType">属性的简短类型名。</param>
    /// <param name="IsValueType">属性类型是否为值类型。</param>
    /// <param name="IsSpecialType">属性类型是否为特殊类型。</param>
    /// <param name="DefaultValue">默认值表达式，已保证与属性类型一致。</param>
    /// <param name="DefaultValueDocumentation">默认值的文档表达式。</param>
    /// <param name="IsReadOnly">是否为只读属性。</param>
    /// <param name="Category">属性的分类。</param>
    /// <param name="Description">属性的描述。</param>
    /// <param name="Flags">元数据标志。</param>
    /// <param name="UpdateSourceTrigger">更新源触发器。</param>
    /// <param name="HasUpdateSourceTrigger">是否显式指定了更新源触发器。</param>
    public readonly record struct DependencyPropertyData(
        string Name,
        string Type,
        string ShortType,
        bool IsValueType,
        bool IsSpecialType,
        string? DefaultValue,
        string? DefaultValueDocumentation,
        bool IsReadOnly,
        string? Category,
        string? Description,
        string? Flags,
        string UpdateSourceTrigger,
        bool HasUpdateSourceTrigger
    )
    {
        /// <summary>
        /// 从类上下文中的特性数据构造依赖属性数据。
        /// </summary>
        /// <param name="context">类上下文。</param>
        public static implicit operator DependencyPropertyData(ClassContext context)
        {
            var attribute = context.Attributes.First();
            var attributeSyntax = attribute.GetSyntax();
            var name = attribute.ConstructorArguments.ElementAtOrDefault(0).Value?.ToString() ?? string.Empty;
            var typeSymbol =
                attribute.GetGenericTypeArgument(0) ?? attribute.ConstructorArguments.ElementAtOrDefault(1).Value as ITypeSymbol;
            var type = typeSymbol?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ?? string.Empty;
            var shortType = typeSymbol?.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat) ?? string.Empty;
            var isValueType = typeSymbol?.IsValueType ?? true;
            var isSpecialType = typeSymbol.IsSpecialType() ?? false;
            var defaultValueExpression = attribute.GetNamedArgument("DefaultValueExpression");
            var defaultValueConstant = attribute.GetNamedArgument("DefaultValue");
            var defaultValue = !defaultValueExpression.IsNull
                ? defaultValueExpression.Value?.ToString()
                : CreateDefaultValueExpression(defaultValueConstant, typeSymbol);
            var defaultValueDocumentation =
                defaultValueExpression.Value?.ToString()
                ?? attributeSyntax?.GetNamedArgumentExpression("DefaultValue");
            var isReadOnly = attribute.GetNamedArgument("IsReadOnly").To<bool>();
            var description = attribute.GetNamedArgument("Description").Value?.ToString();
            var category = attribute.GetNamedArgument("Category").Value?.ToString();
            var temp = attribute.GetNamedArgument("Flags");
            string? flags = null;
            if (!temp.IsNull)
            {
                var arr = temp.ToCSharpString()
                    .Split(['|'], StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(w => w != "System.Windows.FrameworkPropertyMetadataOptions.None")
                    .Select(s => $"global::{s}")
                    .ToArray();

                // 显式写出 Flags = None 时过滤后为空，此时应回退到普通 PropertyMetadata，
                // 否则会生成 `flags: ,` 这样的非法代码。
                if (arr.Length > 0)
                {
                    flags = string.Join(" | ", arr);
                }
            }

            temp = attribute.GetNamedArgument("UpdateSourceTrigger");
            var hasUpdateSourceTrigger = !temp.IsNull;
            var updateSourceTrigger = hasUpdateSourceTrigger
                ? $"global::{temp.ToCSharpString()}"
                : "global::System.Windows.Data.UpdateSourceTrigger.Default";

            return new DependencyPropertyData(
                name,
                type,
                shortType,
                isValueType,
                isSpecialType,
                defaultValue,
                defaultValueDocumentation,
                isReadOnly,
                category,
                description,
                flags,
                updateSourceTrigger,
                hasUpdateSourceTrigger
            );
        }

        /// <summary>
        /// 生成与属性类型精确匹配的默认值表达式。
        /// </summary>
        /// <param name="constant">特性中的默认值常量。</param>
        /// <param name="typeSymbol">属性类型符号。</param>
        /// <returns>默认值表达式；未指定默认值时返回 null。</returns>
        /// <remarks>
        /// 不能直接用 <c>Value.ToString()</c>：枚举会退化成数字，<c>float</c>/<c>decimal</c> 会丢掉后缀，
        /// 生成的值类型与属性类型不一致时，WPF 在注册依赖属性时会抛
        /// “默认值类型与属性类型不匹配”。这里统一按目标类型生成显式表达式。
        /// </remarks>
        private static string? CreateDefaultValueExpression(TypedConstant constant, ITypeSymbol? typeSymbol)
        {
            if (constant.IsNull)
            {
                return null;
            }

            var targetType = typeSymbol is null
                ? null
                : typeSymbol.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
                    ? ((INamedTypeSymbol)typeSymbol).TypeArguments[0]
                    : typeSymbol;

            // 枚举：保留类型名，生成 Mode.B 而不是 1。
            if (constant.Kind == TypedConstantKind.Enum)
            {
                var enumType = constant.Type?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var member = constant.ToCSharpString();

                // ToCSharpString 可能给出 "Demo.Mode.B" 或 "1"（未命名值），前者直接使用。
                if (enumType is not null && !int.TryParse(member, out _))
                {
                    return $"({enumType})({member})";
                }

                return enumType is null ? member : $"({enumType})({constant.Value})";
            }

            if (constant.Kind != TypedConstantKind.Primitive || targetType is null)
            {
                return constant.ToCSharpString();
            }

            var fullyQualifiedType = targetType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var literal = constant.ToCSharpString();

            // 字面量必须与目标类型精确匹配，否则 WPF 注册依赖属性时会抛“默认值类型与属性类型不匹配”。
            // float/decimal 的字面量不带后缀，窄整型会被推断为 int，long/ulong 需要显式后缀。
            return targetType.SpecialType switch
            {
                SpecialType.System_Single => $"({fullyQualifiedType})({literal})",
                SpecialType.System_Decimal => $"({fullyQualifiedType})({literal})",
                SpecialType.System_Byte or SpecialType.System_SByte
                    or SpecialType.System_Int16 or SpecialType.System_UInt16
                    or SpecialType.System_Char => $"({fullyQualifiedType})({literal})",
                SpecialType.System_Int64 => $"{literal}L",
                SpecialType.System_UInt64 => $"{literal}UL",
                _ => literal,
            };
        }
    }

    #endregion CreateDependencyPropertyData

    #region Generate Code

    private string GenerateDependencyProperty(ClassData @class, DependencyPropertyData property, string version)
    {
        var members = GeneratedCode.Join(
            GenerateDependencyPropertyDefinition(@class, property, version),
            GenerateMemberDeclarations(@class, property, version)
        );

        return GeneratedCode.Join(
            "// <auto-generated />",
            "#nullable enable",
            @class.NamespaceDeclaration,
            @class.WrapInTypeDeclarations(members)
        );
    }

    /// <summary>
    /// 生成属性的 CLR 访问器以及分部钩子方法。
    /// </summary>
    protected abstract string GenerateMemberDeclarations(ClassData @class, DependencyPropertyData property, string version);

    /// <summary>
    /// 生成属性元数据中使用的默认值表达式。
    /// </summary>
    /// <param name="property">依赖属性数据。</param>
    /// <returns>默认值表达式。</returns>
    /// <remarks>
    /// 元数据必须使用 <see cref="DependencyPropertyData.DefaultValue"/>：它已按属性类型生成显式表达式。
    /// <see cref="DependencyPropertyData.DefaultValueDocumentation"/> 只是源码原文，用于 XML 注释，
    /// 直接放进元数据会丢失类型信息（例如 <c>float</c> 缺少后缀、枚举退化成数字）。
    /// </remarks>
    protected static string GenerateDefaultValue(DependencyPropertyData property) =>
        property.DefaultValue ?? $"default({property.Type})";

    private string GenerateDependencyPropertyDefinition(ClassData @class, DependencyPropertyData property, string version)
    {
        var defaultValue = !string.IsNullOrEmpty(property.DefaultValueDocumentation)
            ? property.DefaultValueDocumentation
            : $"default({WebUtility.HtmlEncode(property.ShortType)})";
        var identifiers = property.IsReadOnly ? "protected internal" : "public";
        var readOnly = property.IsReadOnly ? "ReadOnly" : string.Empty;
        var key = property.IsReadOnly ? "Key" : string.Empty;
        var definition = !property.IsReadOnly
            ? string.Empty
            : $$"""
            /// <summary>
            /// 标识 <see cref="{{property.Name}}"/> 依赖属性。<br/>
            /// 默认值：{{defaultValue}}
            /// </summary>
            [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]
            [global::System.CodeDom.Compiler.GeneratedCode("{{GeneratorName}}", "{{version}}")]
            public static readonly global::System.Windows.DependencyProperty {{property.Name}}Property = {{property.Name}}PropertyKey.DependencyProperty;
            """;
        // UpdateSourceTrigger 只能通过 FrameworkPropertyMetadata 生效，
        // 因此显式指定它时也必须走该分支，否则设置会被静默丢弃。
        var useFrameworkMetadata = property.Flags is not null || property.HasUpdateSourceTrigger;
        var typeMetadata = useFrameworkMetadata
            ? GeneratedCode.Lines(
                "new global::System.Windows.FrameworkPropertyMetadata(",
                $"    defaultValue: {GenerateDefaultValue(property)},",
                GeneratedCode.Inline("    flags: ", property.Flags ?? "global::System.Windows.FrameworkPropertyMetadataOptions.None", 4) + ",",
                GeneratedCode.Inline("    propertyChangedCallback: ", GeneratePropertyChangedCallback(@class, property), 4) + ",",
                GeneratedCode.Inline("    coerceValueCallback: ", GenerateCoerceValueCallback(@class, property), 4) + ",",
                "    isAnimationProhibited: false,",
                $"    defaultUpdateSourceTrigger: {property.UpdateSourceTrigger}",
                ")"
            )
            : GeneratedCode.Lines(
                "new global::System.Windows.PropertyMetadata(",
                $"    defaultValue: {GenerateDefaultValue(property)},",
                GeneratedCode.Inline("    propertyChangedCallback: ", GeneratePropertyChangedCallback(@class, property), 4) + ",",
                GeneratedCode.Inline("    coerceValueCallback: ", GenerateCoerceValueCallback(@class, property), 4),
                ")"
            );

        var documentation = GeneratedCode.Lines(
            "/// <summary>",
            $"/// 标识 <see cref=\"{property.Name}\"/> 依赖属性。<br/>",
            $"/// 默认值：{defaultValue}",
            "/// </summary>",
            "[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]",
            $"[global::System.CodeDom.Compiler.GeneratedCode(\"{GeneratorName}\", \"{version}\")]"
        );

        var declaration = GeneratedCode.Lines(
            $"{identifiers} static readonly global::System.Windows.DependencyProperty{key} {property.Name}Property{key} = global::System.Windows.DependencyProperty.Register{readOnly}(",
            $"    name: \"{property.Name}\",",
            $"    propertyType: typeof({property.Type}),",
            $"    ownerType: typeof({@class.Type}),",
            GeneratedCode.Inline("    typeMetadata: ", typeMetadata, 4) + ",",
            GeneratedCode.Inline("    validateValueCallback: ", GenerateValidateValueCallback(@class, property), 4),
            ");"
        );

        return GeneratedCode.Join(documentation + declaration, definition);
    }

    private static string GenerateValidateValueCallback(ClassData @class, DependencyPropertyData property)
    {
        var methodName = $"Is{property.Name}Valid";
        var isExist = @class.HasMethod(methodName);

        if (!isExist)
            return "null";

        return GeneratedCode.Lines(
            "static value =>",
            "{",
            "    var result = true;",
            $"    {methodName}(({property.Type})value, ref result);",
            "    return result;",
            "}"
        );
    }

    /// <summary>
    /// 生成强制值回调的委托表达式。
    /// </summary>
    /// <param name="class">目标类数据。</param>
    /// <param name="property">依赖属性数据。</param>
    /// <returns>强制值回调表达式；不存在对应分部方法时返回 <c>null</c>。</returns>
    protected abstract string GenerateCoerceValueCallback(ClassData @class, DependencyPropertyData property);

    /// <summary>
    /// 生成属性变更回调的委托表达式。
    /// </summary>
    /// <param name="class">目标类数据。</param>
    /// <param name="property">依赖属性数据。</param>
    /// <returns>属性变更回调表达式；不存在对应分部方法时返回 <c>null</c>。</returns>
    protected abstract string GeneratePropertyChangedCallback(ClassData @class, DependencyPropertyData property);

    #endregion Generate Code
}
