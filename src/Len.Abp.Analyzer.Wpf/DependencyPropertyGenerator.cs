using Len.Abp.Analyzer;
using Microsoft.CodeAnalysis;

[Generator]
internal class DependencyPropertyGenerator()
    : DependencyPropertyGeneratorBase(
        "Len.Abp.Wpf.DependencyPropertyAttribute",
        "Len.Abp.Wpf.DependencyPropertyAttribute`1"
    )
{
    /// <inheritdoc/>
    protected override string GeneratorName => nameof(DependencyPropertyGenerator);

    /// <inheritdoc/>
    protected override bool GeneratesInstanceMembers => true;

    /// <inheritdoc/>
    protected override string RegisterMethod => "Register";

    /// <inheritdoc/>
    protected override string GenerateMemberDeclarations(ClassData @class, DependencyPropertyData property, string version)
    {
        // 只读属性的 setter 在非密封宿主上是 protected（派生类可改，外部只读）。
        // 密封类不能声明新的保护成员（CS0628），必须去掉修饰符；
        // 不能降级为 public——属性本身已是 public，访问器可访问性必须更严格（CS0273）。
        var setterModifier = property.IsReadOnly && !@class.IsSealed ? "protected " : string.Empty;
        var key = property.IsReadOnly ? "Key" : string.Empty;

        return GeneratedCode.Join(
            GeneratedCode.Lines(
                "/// <summary>",
                property.Description is { } description ? $"/// {description.ToXmlCommentText()}<br/>" : null,
                $"/// 默认值：{GenerateDefaultValueDocumentation(property)}",
                "/// </summary>",
                GeneratedCode.GeneratedMemberAttributes(GeneratorName, version),
                "[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]",
                property.Category is { } category ? $"[global::System.ComponentModel.Category(\"{category.ToCSharpStringLiteral()}\")]" : null,
                $"public {property.Type} {property.Name}",
                "{",
                $"    get {{ return ({property.Type})GetValue({property.Name}Property); }}",
                $"    {setterModifier}set {{ SetValue({property.Name}Property{key}, value); }}",
                "}"
            ),
            GeneratedCode.Lines(
                GeneratedCode.GeneratedMemberAttributes(GeneratorName, version),
                $"static partial void Coerce{property.Name}({property.Type} value, ref {property.Type} result);"
            ),
            GeneratedCode.Lines(
                GeneratedCode.GeneratedMemberAttributes(GeneratorName, version),
                $"static partial void Is{property.Name}Valid({property.Type} value, ref bool result);"
            ),
            GeneratedCode.Lines(
                GeneratedCode.GeneratedMemberAttributes(GeneratorName, version),
                $"partial void On{property.Name}Changed({property.Type} oldValue, {property.Type} newValue);"
            )
        );
    }

    /// <inheritdoc/>
    protected override string CoerceArguments(DependencyPropertyData property) => $"({property.Type})value";

    /// <inheritdoc/>
    protected override string? PropertyChangedGuard(ClassData @class) =>
        $"    if (s is not {@class.Type} control)\n        return;";

    /// <inheritdoc/>
    protected override string PropertyChangedInvocation(
        ClassData @class,
        DependencyPropertyData property,
        string methodName
    ) => $"control.{methodName}(({property.Type})e.OldValue, ({property.Type})e.NewValue);";
}
