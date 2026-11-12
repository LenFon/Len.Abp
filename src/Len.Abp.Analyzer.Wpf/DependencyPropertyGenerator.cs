using Len.Abp.Analyzer;
using Microsoft.CodeAnalysis;

[Generator]
internal class DependencyPropertyGenerator()
    : DependencyPropertyGeneratorBase(
        "DPG",
        "Len.Abp.Wpf.DependencyPropertyAttribute",
        "Len.Abp.Wpf.DependencyPropertyAttribute`1"
    )
{
    /// <inheritdoc/>
    protected override string GeneratorName => nameof(DependencyPropertyGenerator);

    /// <inheritdoc/>
    protected override string GenerateMemberDeclarations(ClassData @class, DependencyPropertyData property, string version)
    {
        var identifiers = property.IsReadOnly ? "protected " : string.Empty;
        var key = property.IsReadOnly ? "Key" : string.Empty;

        return GeneratedCode.Join(
            GeneratedCode.Lines(
                "/// <summary>",
                property.Description is { } description ? $"/// {description.ToXmlCommentText()}<br/>" : null,
                $"/// 默认值：{GenerateDefaultValue(property)}",
                "/// </summary>",
                "[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]",
                $"[global::System.CodeDom.Compiler.GeneratedCode(\"{GeneratorName}\", \"{version}\")]",
                "[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]",
                property.Category is { } category ? $"[global::System.ComponentModel.Category(\"{category.ToCSharpStringLiteral()}\")]" : null,
                $"public {property.Type} {property.Name}",
                "{",
                $"    get {{ return ({property.Type})GetValue({property.Name}Property); }}",
                $"    {identifiers}set {{ SetValue({property.Name}Property{key}, value); }}",
                "}"
            ),
            GeneratedCode.Lines(
                "[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]",
                $"[global::System.CodeDom.Compiler.GeneratedCode(\"{GeneratorName}\", \"{version}\")]",
                $"static partial void Coerce{property.Name}({property.Type} value, ref {property.Type} result);"
            ),
            GeneratedCode.Lines(
                "[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]",
                $"[global::System.CodeDom.Compiler.GeneratedCode(\"{GeneratorName}\", \"{version}\")]",
                $"static partial void Is{property.Name}Valid({property.Type} value, ref bool result);"
            ),
            GeneratedCode.Lines(
                "[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]",
                $"[global::System.CodeDom.Compiler.GeneratedCode(\"{GeneratorName}\", \"{version}\")]",
                $"partial void On{property.Name}Changed({property.Type} oldValue, {property.Type} newValue);"
            )
        );
    }

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
            $"    {methodName}(({property.Type})value, ref result);",
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
            $"    if (s is not {@class.Type} control)",
            "        return;",
            string.Empty,
            $"    control.{methodName}(({property.Type})e.OldValue, ({property.Type})e.NewValue);",
            "}"
        );
    }
}
