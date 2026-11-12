using Len.Abp.Analyzer;
using Microsoft.CodeAnalysis;

[Generator]
internal class AttachedDependencyPropertyGenerator()
    : DependencyPropertyGeneratorBase(
        "ADPG",
        "Len.Abp.Wpf.AttachedDependencyPropertyAttribute",
        "Len.Abp.Wpf.AttachedDependencyPropertyAttribute`1"
    )
{
    /// <inheritdoc/>
    protected override string GeneratorName => nameof(AttachedDependencyPropertyGenerator);

    /// <inheritdoc/>
    protected override string GenerateMemberDeclarations(ClassData @class, DependencyPropertyData property, string version)
    {
        // 静态类不能包含保护成员（CS1057），只读附加属性在静态宿主上退化为 private。
        var identifiers = property.IsReadOnly ? (@class.IsStatic ? "private" : "protected") : "public";
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
                "[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]",
                $"[global::System.CodeDom.Compiler.GeneratedCode(\"{GeneratorName}\", \"{version}\")]",
                $"static partial void Coerce{property.Name}(global::System.Windows.DependencyObject element, {property.Type} value, ref {property.Type} result);"
            ),
            GeneratedCode.Lines(
                "[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]",
                $"[global::System.CodeDom.Compiler.GeneratedCode(\"{GeneratorName}\", \"{version}\")]",
                $"static partial void Is{property.Name}Valid({property.Type} value, ref bool result);"
            ),
            GeneratedCode.Lines(
                "[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]",
                $"[global::System.CodeDom.Compiler.GeneratedCode(\"{GeneratorName}\", \"{version}\")]",
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
            $"/// 默认值：{GenerateDefaultValue(property)}",
            "/// </summary>",
            "[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]",
            $"[global::System.CodeDom.Compiler.GeneratedCode(\"{GeneratorName}\", \"{version}\")]",
            "[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]",
            property.Category is { } category ? $"[global::System.ComponentModel.Category(\"{category.ToCSharpStringLiteral()}\")]" : null
        );

    /// <inheritdoc/>
    protected override string GenerateCoerceValueCallback(ClassData @class, DependencyPropertyData property)
    {
        var methodName = $"Coerce{property.Name}";
        var isExist = @class.HasMethod(methodName);

        if (!isExist)
            return "null";

        return GeneratedCode.Lines(
            "static (s, value) =>",
            "{",
            $"    {property.Type} result = default;",
            $"    {methodName}(s, ({property.Type})value, ref result);",
            "    return result;",
            "}"
        );
    }

    /// <inheritdoc/>
    protected override string GeneratePropertyChangedCallback(ClassData @class, DependencyPropertyData property)
    {
        var methodName = $"On{property.Name}Changed";
        var isExist = @class.HasMethod(methodName);

        if (!isExist)
            return "null";

        return GeneratedCode.Lines(
            "static (s, e) =>",
            "{",
            $"    {methodName}(s, ({property.Type})e.OldValue, ({property.Type})e.NewValue);",
            "}"
        );
    }
}
