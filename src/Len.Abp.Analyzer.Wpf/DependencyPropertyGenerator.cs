using Len.Abp.Analyzer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
        $"{context.Class.FullName}.Properties.{context.Data.Name}.g.cs";

    protected override (ClassData Class, DependencyPropertyData Data, string Version)? PrepareData((ClassContext, string) context)
    {
        var ((_, attributes, classSyntax, classSymbol), version) = context;
        if (attributes.FirstOrDefault() is not { } attribute)
        {
            return null;
        }

        var classData = classSymbol.GetClassData();
        var dependencyPropertyData = CreateDependencyPropertyData(attribute, classSyntax.TryFindAttributeSyntax(attribute));

        return (classData, dependencyPropertyData, version);
    }

    #region CreateDependencyPropertyData

    private static DependencyPropertyData CreateDependencyPropertyData(AttributeData attribute, AttributeSyntax? attributeSyntax = null)
    {
        attribute = attribute ?? throw new ArgumentNullException(nameof(attribute));

        var name = attribute.ConstructorArguments.ElementAtOrDefault(0).Value?.ToString() ?? string.Empty;
        var typeSymbol = attribute.GetGenericTypeArgument(0) ?? attribute.ConstructorArguments.ElementAtOrDefault(1).Value as ITypeSymbol;
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
        var bindable = attribute.GetNamedArgument("Bindable").Value?.To<bool>();
        var browsable = attribute.GetNamedArgument("Browsable").Value?.To<bool>();
        var coerce = attribute.GetNamedArgument("Coerce").To<bool>();
        var validate = attribute.GetNamedArgument("Validate").To<bool>();

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
            bindable,
            browsable,
            coerce,
            validate
        );
    }

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
        bool? Bindable,
        bool? Browsable,
        bool Coerce,
        bool Validate
    );

    #endregion CreateDependencyPropertyData

    #region Generate Code

    private static bool ExistsMethod(ClassData @class, string mehtodSignature) =>
        @class.Methods.Contains($"{@class.FullName}.{mehtodSignature}");

    private static string GenerateCoerce(ClassData @class, DependencyPropertyData property, string version)
    {
        if (!property.Coerce)
            return string.Empty;

        var code = $"""
            [global::System.CodeDom.Compiler.GeneratedCode("{nameof(DependencyPropertyGenerator)}", "{version}")]
                [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]
                private static partial {property.Type} Coerce{property.Name}({property.Type}? value);
            """;

        return code;
    }

    private static string GenerateDefaultValue(DependencyPropertyData property)
    {
        var type = property.Type;
        if (property is { IsSpecialType: true, DefaultValueDocumentation: { } })
        {
            return property.DefaultValueDocumentation;
        }

        return property.DefaultValue != null ? $"({type}){property.DefaultValue}" : $"default({type})";
    }

    private static string GenerateDependencyProperty(ClassData @class, DependencyPropertyData property, string version)
    {
        var code = $$"""
            // <auto-generated />

            #nullable enable

            namespace {{@class.Namespace}};

            partial class {{@class.Name}}
            {
                /// <summary>
                /// Identifies the <see cref="{{property.Name}}"/> dependency property.<br/>
                /// Default value: {{(
                !string.IsNullOrEmpty(property.DefaultValueDocumentation)
                    ? property.DefaultValueDocumentation
                    : $"default({WebUtility.HtmlEncode(property.ShortType)})"
            )}}
                /// </summary>
                [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]
                [global::System.CodeDom.Compiler.GeneratedCode("{{nameof(DependencyPropertyGenerator)}}", "{{version}}")]
                public static readonly global::System.Windows.DependencyProperty {{property.Name}}Property =
                    global::System.Windows.DependencyProperty.Register(
                        "{{property.Name}}",
                        typeof({{property.Type}}),
                        typeof({{@class.Type}}),
                        new global::System.Windows.PropertyMetadata(
                            {{GenerateDefaultValue(property)}},
                            {{GeneratePropertyChangedCallback(@class, property)}}
                            coerceValueCallback: {{(property.Coerce ? $"static (s, value) => Coerce{property.Name}(({property.Type}?)value)" : "null")}}
                        ),
                        validateValueCallback: {{(property.Validate ? $"static value => Is{property.Name}Valid(({property.Type}?)value)" : "null")}}
                    );

                /// <summary>
                {{(property.Description is { } ? $"/// {property.Description}<br/>" : "")}}
                /// Default value: {{GenerateDefaultValue(property)}}
                /// </summary>
                [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]
                [global::System.CodeDom.Compiler.GeneratedCode("{{nameof(DependencyPropertyGenerator)}}", "{{version}}")]
                [global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
                {{(property.Description is { } ? $"[global::System.ComponentModel.Category(\"{property.Description}\")]" : string.Empty)}}
                {{(property.Bindable.HasValue ? $"[global::System.ComponentModel.Bindable({property.Bindable.Value.ToBooleanKeyword()})]" : string.Empty)}}
                {{(property.Browsable.HasValue ? $"[global::System.ComponentModel.Browsable({property.Browsable.Value.ToBooleanKeyword()})]" : string.Empty)}}
                {{(property.Browsable == false ? "[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]" : string.Empty)}}
                public {{property.Type}} {{property.Name}}
                {
                    get { return ({{property.Type}})GetValue({{property.Name}}Property); }
                    set { SetValue({{property.Name}}Property, value); }
                }

                {{GenerateCoerce(@class, property, version)}}

                {{GenerateIsValid(@class, property, version)}}

                [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]
                [global::System.CodeDom.Compiler.GeneratedCode("{{nameof(DependencyPropertyGenerator)}}", "{{version}}")]
                partial void On{{property.Name}}Changed({{property.Type}} oldValue, {{property.Type}} newValue);
            }
            """;

        return code.RemoveBlankLines();
    }

    private static string GenerateIsValid(ClassData @class, DependencyPropertyData property, string version)
    {
        if (!property.Validate)
            return string.Empty;

        var code = $"""
            [global::System.CodeDom.Compiler.GeneratedCode("{nameof(DependencyPropertyGenerator)}", "{version}")]
                [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]
                private static partial bool Is{property.Name}Valid({property.Type}? value);
            """;

        return code;
    }

    private static string GeneratePropertyChangedCallback(ClassData @class, DependencyPropertyData property)
    {
        var methodName = $"On{property.Name}Changed";
        var isChanged = ExistsMethod(@class, $"{methodName}({property.Type}, {property.Type})");

        if (!isChanged)
            return "propertyChangedCallback: null,";

        var code = $$"""
            propertyChangedCallback: static (s, e) =>
                            {
                                if (s is not {{@class.Type}} control)
                                    return;

                                {{(isChanged ? $"control.{methodName}(({property.Type})e.OldValue, ({property.Type})e.NewValue);" : "")}}
                            },
            """;
        return code;
    }

    #endregion Generate Code
}
