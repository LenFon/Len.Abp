using Len.Abp.Analyzer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Diagnostics;
using System.Net;

[Generator]
internal class DependencyPropertyGenerator()
    : BasicGenerator<DependencyPropertyGenerator.DependencyPropertyData>(
        "DPG",
        "Len.Abp.Wpf.DependencyPropertyAttribute",
        "Len.Abp.Wpf.DependencyPropertyAttribute`1"
    )
{
    protected override string CreateSourceCode((ClassData Class, DependencyPropertyData Data, string Version) context) =>
        GenerateDependencyProperty(context.Class, context.Data, context.Version);

    protected override string CreateFileName((ClassData Class, DependencyPropertyData Data, string Version) context) =>
        $"{context.Class.FullName}.{context.Data.Name}.g.cs";

    protected override (ClassData Class, DependencyPropertyData Data, string Version)? PrepareData((ClassContext, string) context)
    {
        var (classContext, version) = context;
        if (classContext.Attributes.FirstOrDefault() is not { } attribute)
        {
            return null;
        }

        var classData = classContext.ClassSymbol.GetClassData();
        return (classData, classContext, version);
    }

    #region CreateDependencyPropertyData

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
        string UpdateSourceTrigger
    )
    {
        public static implicit operator DependencyPropertyData(ClassContext context)
        {
            //Debugger.Launch();
            var attribute = context.Attributes.First();
            var attributeSyntax = context.ClassSyntax.TryFindAttributeSyntax(attribute);
            var name = attribute.ConstructorArguments.ElementAtOrDefault(0).Value?.ToString() ?? string.Empty;
            var typeSymbol =
                attribute.GetGenericTypeArgument(0) ?? attribute.ConstructorArguments.ElementAtOrDefault(1).Value as ITypeSymbol;
            var type = typeSymbol?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ?? string.Empty;
            var shortType = typeSymbol?.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat) ?? string.Empty;
            var isValueType = typeSymbol?.IsValueType ?? true;
            var isSpecialType = typeSymbol.IsSpecialType() ?? false;
            var defaultValue =
                attribute.GetNamedArgument("DefaultValueExpression").Value?.ToString()
                ?? attribute.GetNamedArgument("DefaultValue").Value?.ToString();
            var defaultValueDocumentation =
                attribute.GetNamedArgument("DefaultValueExpression").Value?.ToString()
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

                flags = string.Join(" | ", arr);
            }

            temp = attribute.GetNamedArgument("UpdateSourceTrigger");
            var updateSourceTrigger = temp.IsNull
                ? "global::System.Windows.Data.UpdateSourceTrigger.Default"
                : $"global::{attribute.GetNamedArgument("UpdateSourceTrigger").ToCSharpString()}";

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
                updateSourceTrigger
            );
        }
    }

    #endregion CreateDependencyPropertyData

    #region Generate Code

    private static string GenerateDefaultValue(DependencyPropertyData property)
    {
        //Debugger.Launch();
        var type = property.Type;
        if (property is { IsSpecialType: true, DefaultValueDocumentation: { } })
        {
            return property.DefaultValueDocumentation;
        }

        return property.DefaultValue ?? $"default({type})";
    }

    private static string GenerateDependencyProperty(ClassData @class, DependencyPropertyData property, string version)
    {
        var identifiers = property.IsReadOnly ? "protected " : string.Empty;
        var key = property.IsReadOnly ? "Key" : string.Empty;
        var code = $$"""
            // <auto-generated />

            #nullable enable

            namespace {{@class.Namespace}};

            partial class {{@class.Name}}
            {
                {{GenerateDependencyPropertyDefinition(@class, property, version)}}
                /// <summary>
                {{(property.Description is { } ? $"/// {property.Description}<br/>" : "")}}
                /// Default value: {{GenerateDefaultValue(property)}}
                /// </summary>
                [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]
                [global::System.CodeDom.Compiler.GeneratedCode("{{nameof(DependencyPropertyGenerator)}}", "{{version}}")]
                [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
                {{(property.Category is { } ? $"[global::System.ComponentModel.Category(\"{property.Category}\")]" : string.Empty)}}
                public {{property.Type}} {{property.Name}}
                {
                    get { return ({{property.Type}})GetValue({{property.Name}}Property); }
                    {{identifiers}}set { SetValue({{property.Name}}Property{{key}}, value); }
                }

                [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]
                [global::System.CodeDom.Compiler.GeneratedCode("{{nameof(DependencyPropertyGenerator)}}", "{{version}}")]
                static partial void Coerce{{property.Name}}({{property.Type}} value, ref {{property.Type}} result);

                [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]
                [global::System.CodeDom.Compiler.GeneratedCode("{{nameof(DependencyPropertyGenerator)}}", "{{version}}")]
                static partial void Is{{property.Name}}Valid({{property.Type}} value, ref bool result);

                [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]
                [global::System.CodeDom.Compiler.GeneratedCode("{{nameof(DependencyPropertyGenerator)}}", "{{version}}")]
                partial void On{{property.Name}}Changed({{property.Type}} oldValue, {{property.Type}} newValue);
            }
            """;

        return code.RemoveBlankLines();
    }

    private static string GenerateDependencyPropertyDefinition(ClassData @class, DependencyPropertyData property, string version)
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
                    /// Identifies the <see cref="{{property.Name}}"/> dependency property.<br/>
                    /// Default value: {{defaultValue}}
                    /// </summary>
                    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]
                    [global::System.CodeDom.Compiler.GeneratedCode("{{nameof(DependencyPropertyGenerator)}}", "{{version}}")]
                    public static readonly global::System.Windows.DependencyProperty {{property.Name}}Property = {{property.Name}}PropertyKey.DependencyProperty;
                
                """;
        var typeMetadata = property.Flags is null ? $$"""
            new global::System.Windows.PropertyMetadata(
                        defaultValue: {{GenerateDefaultValue(property)}},
                        propertyChangedCallback: {{GeneratePropertyChangedCallback(@class, property)}},
                        coerceValueCallback: {{GenerateCoerceValueCallback(@class, property)}}
                    )
            """ : $$"""
            new global::System.Windows.FrameworkPropertyMetadata(
                        defaultValue: {{GenerateDefaultValue(property)}},
                        flags: {{property.Flags}},
                        propertyChangedCallback: {{GeneratePropertyChangedCallback(@class, property)}},
                        coerceValueCallback: {{GenerateCoerceValueCallback(@class, property)}},
                        isAnimationProhibited: false,
                        defaultUpdateSourceTrigger: {{property.UpdateSourceTrigger}}
                    )
            """;
        var code = $$"""
            /// <summary>
                /// Identifies the <see cref="{{property.Name}}"/> dependency property.<br/>
                /// Default value: {{defaultValue}}
                /// </summary>
                [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]
                [global::System.CodeDom.Compiler.GeneratedCode("{{nameof(DependencyPropertyGenerator)}}", "{{version}}")]
                {{identifiers}} static readonly global::System.Windows.DependencyProperty{{key}} {{property.Name}}Property{{key}} = global::System.Windows.DependencyProperty.Register{{readOnly}}(
                    name: "{{property.Name}}",
                    propertyType: typeof({{property.Type}}),
                    ownerType: typeof({{@class.Type}}),
                    typeMetadata: {{typeMetadata}},
                    validateValueCallback: {{GenerateValidateValueCallback(@class, property)}}
                );
            {{definition}}
            """;
        return code;
    }

    private static string GenerateValidateValueCallback(ClassData @class, DependencyPropertyData property)
    {
        var methodName = $"Is{property.Name}Valid";
        var isExist = @class.Methods.Any(w => w.Contains(methodName));

        if (!isExist)
            return "null";

        var code = $$"""
            static value =>
                    {
                        var result = true;
                        {{methodName}}(({{property.Type}})value, ref result);
                        return result;
                    }
            """;
        return code;
    }

    private static string GenerateCoerceValueCallback(ClassData @class, DependencyPropertyData property)
    {
        var methodName = $"Coerce{property.Name}";
        var isExist = @class.Methods.Any(w => w.Contains(methodName));

        if (!isExist)
            return "null";

        var code = $$"""
            static (s, value) =>
                        {
                            {{property.Type}} result = default;
                            {{methodName}}(({{property.Type}})value, ref result);
                            return result;
                        }
            """;
        return code;
    }

    private static string GeneratePropertyChangedCallback(ClassData @class, DependencyPropertyData property)
    {
        var methodName = $"On{property.Name}Changed";
        var isExist = @class.Methods.Any(w => w.Contains(methodName));

        if (!isExist)
            return "null";

        var code = $$"""
            static (s, e) =>
                        {
                            if (s is not {{@class.Type}} control)
                                return;

                            control.{{methodName}}(({{property.Type}})e.OldValue, ({{property.Type}})e.NewValue);
                        }
            """;
        return code;
    }

    #endregion Generate Code
}
