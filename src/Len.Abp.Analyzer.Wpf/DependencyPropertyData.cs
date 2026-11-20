using Len.Abp.Analyzer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Len.Abp.Analyzer;

/// <summary>
/// 生成依赖属性所需的全部数据。
/// </summary>
/// <param name="Name">属性名称。</param>
/// <param name="Type">属性的完全限定类型名。</param>
/// <param name="DefaultValue">默认值表达式，已保证与属性类型一致。</param>
/// <param name="DefaultValueDocumentation">默认值的文档表达式。</param>
/// <param name="IsReadOnly">是否为只读属性。</param>
/// <param name="Category">属性的分类。</param>
/// <param name="Description">属性的描述。</param>
/// <param name="Flags">元数据标志。</param>
/// <param name="UpdateSourceTrigger">更新源触发器。</param>
/// <param name="HasUpdateSourceTrigger">是否显式指定了更新源触发器。</param>
/// <param name="IsReferenceType">属性类型是否为引用类型。</param>
internal readonly record struct DependencyPropertyData(
    string Name,
    string Type,
    string? DefaultValue,
    string? DefaultValueDocumentation,
    bool IsReadOnly,
    string? Category,
    string? Description,
    string? Flags,
    string UpdateSourceTrigger,
    bool HasUpdateSourceTrigger,
    bool IsReferenceType
) : INamedMemberData
{
    /// <summary>
    /// 从类上下文中的特性数据解析依赖属性数据。
    /// </summary>
    /// <param name="context">类上下文。</param>
    /// <returns>解析后的依赖属性数据。</returns>
    public static DependencyPropertyData Parse(ClassContext context)
    {
        var attribute = context.Attributes.First();
        var attributeSyntax = attribute.GetSyntax();
        var name = attribute.GetNameArgument() ?? string.Empty;
        var typeSymbol =
            attribute.GetTypeArgument(0, 1);
        var type = typeSymbol?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ?? string.Empty;
        var defaultValueExpression = attribute.GetNamedArgument("DefaultValueExpression");
        var defaultValueConstant = attribute.GetNamedArgument("DefaultValue");

        // 空字符串不是"未指定"：直接取 Value 会把 "" 当成表达式，生成 defaultValue: ,
        // 编译报 CS0839。这里统一按"空白即未指定"处理。
        var expressionText = defaultValueExpression.Value?.ToString();
        var hasExpression = !defaultValueExpression.IsNull && !string.IsNullOrWhiteSpace(expressionText);

        var defaultValue = hasExpression
            ? WrapExpression(expressionText!, type)
            : CreateDefaultValueExpression(defaultValueConstant, typeSymbol);
        var defaultValueDocumentation = hasExpression
            ? expressionText
            : attributeSyntax?.GetNamedArgumentExpression("DefaultValue");
        var isReadOnly = attribute.GetNamedArgument("IsReadOnly").To<bool>();
        var description = attribute.GetNamedArgumentString("Description");
        var category = attribute.GetNamedArgumentString("Category");

        return new DependencyPropertyData(
            name,
            type,
            defaultValue,
            defaultValueDocumentation,
            isReadOnly,
            category,
            description,
            ParseFlags(attribute),
            ParseUpdateSourceTrigger(attribute, out var hasUpdateSourceTrigger),
            hasUpdateSourceTrigger,
            typeSymbol?.IsReferenceType ?? false
        );
    }

    /// <summary>
    /// 解析元数据标志。
    /// </summary>
    /// <param name="attribute">依赖属性特性数据。</param>
    /// <returns>标志表达式；未指定或只指定 <c>None</c> 时返回 <see langword="null"/>。</returns>
    /// <remarks>
    /// 显式写出 <c>Flags = None</c> 时过滤后为空，此时必须回退到普通 <c>PropertyMetadata</c>，
    /// 否则会生成 <c>flags: ,</c> 这样的非法代码。
    /// </remarks>
    private static string? ParseFlags(AttributeData attribute)
    {
        var constant = attribute.GetNamedArgument("Flags");
        if (constant.IsNull)
        {
            return null;
        }

        var enumType = constant.Type?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        var flags = constant
            .ToCSharpString()
            .Split(['|'], StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(w => w != "System.Windows.FrameworkPropertyMetadataOptions.None")
            .Select(member => FormatEnumMember(member, enumType))
            .ToArray();

        return flags.Length > 0 ? string.Join(" | ", flags) : null;
    }

    /// <summary>
    /// 解析更新源触发器。
    /// </summary>
    /// <param name="attribute">依赖属性特性数据。</param>
    /// <param name="hasUpdateSourceTrigger">
    /// 是否需要在元数据里写出该参数：显式指定且值不是 <c>Default</c> 时为 <see langword="true"/>。
    /// </param>
    /// <returns>触发器表达式。</returns>
    /// <remarks>
    /// <para>
    /// <b>显式写 <c>Default</c> 等同于未指定。</b>WPF 的
    /// <c>FrameworkPropertyMetadata</c> 构造函数会拒绝 <c>Default</c>：
    /// 「不能将 DefaultUpdateSourceTrigger 设置为 UpdateSourceTrigger.Default；这样会创建循环定义」。
    /// 该异常发生在静态字段初始化阶段，表现为 <see cref="TypeInitializationException"/>，
    /// 堆栈里看不到生成器。
    /// </para>
    /// <para>
    /// 因此判定不能只看"命名参数是否被赋值"——那会把显式写的 <c>Default</c> 也当成需要写出。
    /// 按底层值判断而不是按名称：<c>(UpdateSourceTrigger)0</c> 与 <c>Default</c> 是同一个值，
    /// 同样会让 WPF 抛异常。
    /// </para>
    /// </remarks>
    private static string ParseUpdateSourceTrigger(AttributeData attribute, out bool hasUpdateSourceTrigger)
    {
        var constant = attribute.GetNamedArgument("UpdateSourceTrigger");

        if (constant.IsNull || constant.To<int>() == 0)
        {
            hasUpdateSourceTrigger = false;

            return DefaultUpdateSourceTrigger;
        }

        hasUpdateSourceTrigger = true;

        return FormatEnumConstant(constant);
    }

    /// <summary>
    /// 未指定更新源触发器时使用的表达式。
    /// </summary>
    private const string DefaultUpdateSourceTrigger = "global::System.Windows.Data.UpdateSourceTrigger.Default";

    /// <summary>
    /// 把枚举常量格式化为可直接写入生成代码的表达式。
    /// </summary>
    /// <param name="constant">枚举常量。</param>
    /// <returns>形如 <c>global::Ns.Enum.Member</c> 或 <c>(global::Ns.Enum)(9)</c> 的表达式。</returns>
    private static string FormatEnumConstant(TypedConstant constant) =>
        FormatEnumMember(
            constant.ToCSharpString(),
            constant.Type?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
        );

    /// <summary>
    /// 把枚举成员名格式化为可直接写入生成代码的表达式。
    /// </summary>
    /// <param name="member">成员名，或未定义值的十进制数字。</param>
    /// <param name="enumType">枚举类型的完全限定名。</param>
    /// <returns>可直接嵌入生成代码的表达式。</returns>
    /// <remarks>
    /// <c>TypedConstant.ToCSharpString()</c> 对命名成员给出
    /// <c>System.Windows.Data.UpdateSourceTrigger.PropertyChanged</c>，对未定义值给出纯数字
    /// （如 <c>(UpdateSourceTrigger)9</c> 得到 <c>"9"</c>）。后者拼上 <c>global::</c>
    /// 会变成 <c>global::9</c>，编译报 CS1001，因此必须改成强制转换。
    /// </remarks>
    private static string FormatEnumMember(string member, string? enumType) =>
        long.TryParse(member, out _) && enumType is not null
            ? $"({enumType})({member})"
            : $"global::{member}";

    /// <summary>
    /// 把使用者写的默认值表达式包成与属性类型一致的强制转换。
    /// </summary>
    /// <param name="expression">使用者写的表达式原文。</param>
    /// <param name="type">属性的完全限定类型名。</param>
    /// <returns>形如 <c>(global::Ns.T)(expr)</c> 的表达式。</returns>
    /// <remarks>
    /// <para>
    /// <b>必须包转换。</b>特性命名参数的类型是 <see cref="object"/>，
    /// 因此 <c>DefaultValueExpression = "new()"</c> 里的 <c>new()</c> 会被推断成
    /// <see cref="object"/> 而不是属性类型。WPF 在注册时校验默认值类型，
    /// 不匹配就抛「默认值类型与属性类型不匹配」，表现为类型初始化异常。
    /// </para>
    /// <para>
    /// 包上转换后目标类型由属性类型决定，<c>new()</c>、集合表达式这类依赖目标类型的写法
    /// 都能正确推断。代价是表达式本身的类型不再参与推断——但它本来就该是属性类型。
    /// </para>
    /// <para>
    /// 属性类型解析不出来时原样返回：那种情况由其它诊断负责，这里不应再制造语法错误。
    /// </para>
    /// </remarks>
    private static string WrapExpression(string expression, string type) =>
        string.IsNullOrEmpty(type) ? expression : $"({type})({expression})";

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

        // 非有限值必须走静态成员：ToCSharpString 给出 "NaN" / "Infinity" / "-Infinity"，
        // 按下面的规则加后缀会得到 NaND、InfinityD 这种不存在的标识符（CS0103）。
        if (IsNonFiniteLiteral(literal, out var nonFiniteMember))
        {
            return $"{fullyQualifiedType}.{nonFiniteMember}";
        }

        // 字面量必须与目标类型精确匹配，否则 WPF 注册依赖属性时会抛“默认值类型与属性类型不匹配”。
        // ToCSharpString 只保证"文本能表示这个值"，不保证"文本的类型就是目标类型"：
        // double 的 1.0 会输出成 1，uint 的 1 不带后缀，两者都是 int 字面量。
        // 因此每个需要后缀或转换的类型都必须显式列出，不能落到 _ 分支。
        return targetType.SpecialType switch
        {
            // 浮点与 decimal：字面量不带后缀，用强制转换保证类型。
            SpecialType.System_Single => $"({fullyQualifiedType})({literal})",
            SpecialType.System_Double => $"{literal}D",
            SpecialType.System_Decimal => $"({fullyQualifiedType})({literal})",
            // 窄整型与 char：字面量会被推断为 int，用强制转换收窄。
            SpecialType.System_Byte or SpecialType.System_SByte
                or SpecialType.System_Int16 or SpecialType.System_UInt16
                or SpecialType.System_Char => $"({fullyQualifiedType})({literal})",
            // 无符号 32 位：字面量是 int，必须加 U 后缀。
            SpecialType.System_UInt32 => $"{literal}U",
            SpecialType.System_Int64 => $"{literal}L",
            SpecialType.System_UInt64 => $"{literal}UL",
            // int 与 nint 的字面量本身就是正确类型，无需处理。
            _ => literal,
        };
    }

    /// <summary>
    /// 判断字面量是否表示非有限浮点值，并给出对应的静态成员名。
    /// </summary>
    /// <param name="literal">字面量文本。</param>
    /// <param name="member">静态成员名；不是非有限值时为空字符串。</param>
    /// <returns>是非有限值时返回 <see langword="true"/>。</returns>
    /// <remarks>
    /// <para>
    /// <c>TypedConstant.ToCSharpString()</c> 对 <c>double.NaN</c> 返回 <c>"NaN"</c>、
    /// 对 <c>double.PositiveInfinity</c> 返回 <c>"Infinity"</c>、
    /// 对 <c>double.NegativeInfinity</c> 返回 <c>"-Infinity"</c>。
    /// 这些都不是合法字面量，必须改写成 <c>double.NaN</c> 这类静态成员访问。
    /// </para>
    /// <para>
    /// 只认这三种写法：其余文本原样返回，交给调用方按类型加后缀。
    /// 用精确匹配而不是 <c>Contains</c>，避免把使用者写的 <c>MyNaN</c> 之类误判。
    /// </para>
    /// </remarks>
    private static bool IsNonFiniteLiteral(string literal, out string member)
    {
        member = literal switch
        {
            "NaN" => "NaN",
            "Infinity" => "PositiveInfinity",
            "-Infinity" => "NegativeInfinity",
            _ => string.Empty,
        };

        return member.Length > 0;
    }
}
