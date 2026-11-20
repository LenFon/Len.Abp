using Len.Abp.Analyzer;
using Microsoft.CodeAnalysis;

[Generator]
internal class AttachedDependencyPropertyGenerator()
    : DependencyPropertyGeneratorBase(
        "Len.Abp.Wpf.AttachedDependencyPropertyAttribute",
        "Len.Abp.Wpf.AttachedDependencyPropertyAttribute`1"
    )
{
    /// <inheritdoc/>
    protected override string GeneratorName => nameof(AttachedDependencyPropertyGenerator);

    /// <inheritdoc/>
    /// <remarks>
    /// 附加版本只生成静态的 <c>Get</c>/<c>Set</c> 方法与静态分部钩子，不产出实例成员，
    /// 因此静态类宿主能通过编译。注册走 <c>RegisterAttached</c>：
    /// 它允许 <c>ownerType</c> 是静态类，XAML 也才能把属性识别为附加属性。
    /// </remarks>
    protected override bool GeneratesInstanceMembers => false;

    /// <inheritdoc/>
    protected override string RegisterMethod => "RegisterAttached";

    /// <inheritdoc/>
    protected override string GenerateMemberDeclarations(ClassData @class, DependencyPropertyData property, string version)
    {
        // 静态类不能包含保护成员（CS1057），密封类不能声明新的保护成员（CS0628）。
        // 两种宿主上只读附加属性都退化为 private：Set 只给同类型的静态方法用，
        // 不需要跨类型可见性。
        var identifiers = property.IsReadOnly ? (@class.IsStatic || @class.IsSealed ? "private" : "protected") : "public";
        var key = property.IsReadOnly ? "Key" : string.Empty;
        var documentation = GenerateDocumentation(property, version);

        return GeneratedCode.Join(
            documentation + GeneratedCode.Lines(
                $"public static {property.Type} Get{property.Name}(global::System.Windows.DependencyObject element)",
                "{",
                "    global::System.ArgumentNullException.ThrowIfNull(element);",
                $"    return ({property.Type})element.GetValue({property.Name}Property);",
                "}"
            ),
            documentation + GeneratedCode.Lines(
                $"{identifiers} static void Set{property.Name}(global::System.Windows.DependencyObject element, {property.Type} value)",
                "{",
                "    global::System.ArgumentNullException.ThrowIfNull(element);",
                $"    element.SetValue({property.Name}Property{key}, value);",
                "}"
            ),
            GeneratedCode.Lines(
                GeneratedCode.GeneratedMemberAttributes(GeneratorName, version),
                $"static partial void Coerce{property.Name}(global::System.Windows.DependencyObject element, {property.Type} value, ref {property.Type} result);"
            ),
            GeneratedCode.Lines(
                GeneratedCode.GeneratedMemberAttributes(GeneratorName, version),
                $"static partial void Is{property.Name}Valid({property.Type} value, ref bool result);"
            ),
            GeneratedCode.Lines(
                GeneratedCode.GeneratedMemberAttributes(GeneratorName, version),
                $"static partial void On{property.Name}Changed(global::System.Windows.DependencyObject element, {property.Type} oldValue, {property.Type} newValue);"
            )
        );
    }

    /// <summary>
    /// 生成 Get/Set 方法共用的 XML 文档与特性。
    /// </summary>
    private string GenerateDocumentation(DependencyPropertyData property, string version) =>
        GeneratedCode.Lines(
            "/// <summary>",
            property.Description is { } description ? $"/// {description.ToXmlCommentText()}<br/>" : null,
            $"/// 默认值：{GenerateDefaultValueDocumentation(property)}",
            "/// </summary>",
            GeneratedCode.GeneratedMemberAttributes(GeneratorName, version),
            "[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]",
            property.Category is { } category ? $"[global::System.ComponentModel.Category(\"{category.ToCSharpStringLiteral()}\")]" : null
        );

    /// <inheritdoc/>
    protected override string CoerceArguments(DependencyPropertyData property) => $"s, ({property.Type})value";

    /// <inheritdoc/>
    protected override string? PropertyChangedGuard(ClassData @class) => null;

    /// <inheritdoc/>
    protected override string PropertyChangedInvocation(
        ClassData @class,
        DependencyPropertyData property,
        string methodName
    ) => $"{methodName}(s, ({property.Type})e.OldValue, ({property.Type})e.NewValue);";
}
