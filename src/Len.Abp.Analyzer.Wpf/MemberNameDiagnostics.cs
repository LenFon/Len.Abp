using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Len.Abp.Analyzer;

/// <summary>
/// 校验生成器特性声明的成员名称：能否作为 C# 标识符，以及是否与同类型上的其它特性冲突。
/// </summary>
/// <remarks>
/// <para>
/// 名称会出现在三处：成员声明（<c>public string Title</c>）、支撑字段
/// （<c>TitleProperty</c> / <c>TitleEvent</c>）与 <c>Register(name: "Title")</c> 的字面量。
/// 名称非法时产出的是十余条互相矛盾的语法错误，没有一条指向真正的问题，
/// 因此必须在生成之前校验。
/// </para>
/// <para>
/// 冲突判定按"成员空间"而不是"特性类"：依赖属性与路由事件生成到不同后缀的支撑字段，
/// 但非附加版本都会生成与名称同名的实例成员，两者落在同一空间（<c>CS0102</c>）。
/// </para>
/// </remarks>
internal static class MemberNameDiagnostics
{
    /// <summary>
    /// 校验名称能否作为 C# 标识符用于生成成员。
    /// </summary>
    /// <param name="name">特性声明的名称。</param>
    /// <returns>合法时返回 <see langword="null"/>；否则返回可读的错误说明。</returns>
    /// <remarks>
    /// <para>
    /// 关键字（<c>class</c>、<c>int</c> 等）单独提示：<c>@class</c> 在生成代码里是合法的，
    /// 但支撑字段会变成 <c>@classProperty</c> 这种可读性很差的名字，建议换一个。
    /// </para>
    /// <para>
    /// <see cref="SyntaxFacts.IsValidIdentifier"/> 允许 Unicode 标识符（如中文名），
    /// 与 C# 语言规则一致，不做额外限制。
    /// </para>
    /// </remarks>
    public static string? ValidateIdentifier(string name)
    {
        if (!SyntaxFacts.IsValidIdentifier(name))
        {
            return $"名称 \"{name}\" 不是合法的 C# 标识符，无法生成成员。"
                + "请使用字母或下划线开头、只含字母、数字与下划线的名称。";
        }

        return SyntaxFacts.GetKeywordKind(name) != SyntaxKind.None
            ? $"名称 \"{name}\" 是 C# 关键字，生成的成员与支撑字段会变成 @{name} 形式，可读性很差。请换一个名称。"
            : null;
    }

    /// <summary>
    /// 判断同一类型上是否存在同名的冲突特性。
    /// </summary>
    /// <param name="classContext">类上下文。</param>
    /// <param name="name">当前特性的名称。</param>
    /// <returns>存在同名冲突特性时返回可读的错误说明；否则返回 <see langword="null"/>。</returns>
    /// <remarks>
    /// <para>
    /// 冲突判定见 <see cref="IsSameMemberSpace"/>。这里只负责把冲突翻译成
    /// 使用者能直接照做的说明：实例版本与附加版本并存时，问题不是"名称重复"
    /// 而是"两者根本无法共存"，需要给出不同的提示。
    /// </para>
    /// <para>
    /// 按不区分大小写比较：生成的文件名含名称，而 Roslyn 要求 hintName
    /// 大小写不敏感唯一。若把 <c>Title</c> 与 <c>title</c> 当成两个成员，
    /// 生成器会因 hintName 冲突整体抛异常（CS8785），一个源文件都不产出。
    /// 因此这里把仅大小写不同的名称视为重复，提前报告 <c>LENABP004</c>。
    /// </para>
    /// </remarks>
    public static string? GetDuplicateNameMessage(ClassContext classContext, string name)
    {
        var current = classContext.Attributes.First();
        var attributeClass = current.AttributeClass;

        var duplicates = classContext.ClassSymbol
            .GetAttributes()
            .Where(candidate =>
                IsSameMemberSpace(candidate.AttributeClass, attributeClass)
                && string.Equals(candidate.GetNameArgument(), name, StringComparison.OrdinalIgnoreCase)
            )
            .ToArray();

        if (duplicates.Length <= 1)
        {
            return null;
        }

        var attributeName = attributeClass?.Name ?? "特性";

        // 实例版本与附加版本并存：支撑字段同名，且运行时注册必然冲突。
        if (duplicates.Any(candidate => AttributeFamily.IsAttached(candidate.AttributeClass))
            && duplicates.Any(candidate => ProducesBareMember(candidate.AttributeClass)))
        {
            return $"{attributeName} 的名称 \"{name}\" 在类型 {classContext.ClassSymbol.Name} 上同时用于实例成员与附加成员。"
                + "两者生成同名支撑字段，且会以相同名称注册到 WPF，无法共存。请改用不同的名称。";
        }

        return $"{attributeName} 的名称 \"{name}\" 在类型 {classContext.ClassSymbol.Name} 上重复。";
    }

    /// <summary>
    /// 判断两个特性是否生成到同一成员空间。
    /// </summary>
    /// <param name="candidate">待比较的特性类型。</param>
    /// <param name="current">当前特性类型。</param>
    /// <returns>同名成员会冲突时返回 <see langword="true"/>。</returns>
    /// <remarks>
    /// <para>
    /// 冲突有两种来源，任一成立即冲突：
    /// </para>
    /// <list type="number">
    /// <item>
    /// <b>支撑字段同名</b>：依赖属性族（含附加版本）都生成 <c>{名称}Property</c>，
    /// 路由事件族（含附加版本）都生成 <c>{名称}Event</c>。
    /// 附加版本并不改变字段名，因此 <c>[DependencyProperty("X")]</c> 与
    /// <c>[AttachedDependencyProperty("X")]</c> 会报 <c>CS0102</c>，
    /// <c>[RoutedEvent("X")]</c> 与 <c>[AttachedRoutedEvent("X")]</c> 同理。
    /// 这类组合还会以相同名称、相同 ownerType 向 WPF 注册，运行时同样失败。
    /// </item>
    /// <item>
    /// <b>裸名成员同名</b>：只有非附加的依赖属性生成名为 <c>X</c> 的属性，
    /// 只有非附加的路由事件生成名为 <c>X</c> 的事件，两者落在同一成员空间（<c>CS0102</c>）。
    /// </item>
    /// </list>
    /// <para>
    /// 附加版本之间不冲突：附加属性生成 <c>Get{名称}</c> / <c>Set{名称}</c>，
    /// 附加事件生成 <c>Add{名称}Handler</c> / <c>Raise{名称}Event</c>，
    /// 成员名与支撑字段后缀都不同。附加属性与非附加路由事件、
    /// 非附加依赖属性与附加路由事件同样不冲突。
    /// </para>
    /// </remarks>
    private static bool IsSameMemberSpace(INamedTypeSymbol? candidate, INamedTypeSymbol? current)
    {
        if (SymbolEqualityComparer.Default.Equals(candidate, current))
        {
            return true;
        }

        // 支撑字段同名：{名称}Property 与 {名称}Property，或 {名称}Event 与 {名称}Event。
        if (GetSupportFieldSuffix(candidate) is { } candidateSuffix
            && string.Equals(candidateSuffix, GetSupportFieldSuffix(current), StringComparison.Ordinal))
        {
            return true;
        }

        // 裸名成员同名：非附加依赖属性的属性 X 与非附加路由事件的事件 X。
        return ProducesBareMember(candidate) && ProducesBareMember(current);
    }

    /// <summary>
    /// 取特性生成的支撑字段后缀。
    /// </summary>
    /// <param name="attributeClass">特性类型。</param>
    /// <returns><c>Property</c> 或 <c>Event</c>；不是生成器特性时返回 <see langword="null"/>。</returns>
    /// <remarks>
    /// 附加版本与非附加版本的后缀相同——这正是它们冲突的原因。
    /// </remarks>
    private static string? GetSupportFieldSuffix(INamedTypeSymbol? attributeClass) =>
        AttributeFamily.GetFamilyName(attributeClass) switch
        {
            AttributeFamily.DependencyProperty => "Property",
            AttributeFamily.RoutedEvent => "Event",
            _ => null,
        };

    /// <summary>
    /// 判断特性是否生成与名称同名的实例成员。
    /// </summary>
    /// <param name="attributeClass">特性类型。</param>
    /// <returns>生成同名实例成员时返回 <see langword="true"/>。</returns>
    /// <remarks>
    /// 只有非附加版本生成裸名成员：<c>[DependencyProperty("X")]</c> 生成属性 <c>X</c>，
    /// <c>[RoutedEvent("X")]</c> 生成事件 <c>X</c>。附加版本生成的都是带前缀的静态方法。
    /// </remarks>
    private static bool ProducesBareMember(INamedTypeSymbol? attributeClass) =>
        !AttributeFamily.IsAttached(attributeClass) && AttributeFamily.GetFamilyName(attributeClass) is not null;
}
