using System.Net;
using Len.Abp.Analyzer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

[Generator]
internal class DependencyPropertyGenerator : IIncrementalGenerator
{
    private const string _id = "DPG";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var framework = context.GetFramework();
        var version = context.GetVersion();

        context
            .SyntaxProvider.ForAttributeWithMetadataNameOfClassesAndRecords("Len.Abp.Wpf.DependencyPropertyAttribute")
            .SelectManyAttributes()
            .Combine(framework)
            .Combine(version)
            .SelectAndReportExceptions(PrepareData, context, _id)
            .WhereNotNull()
            .SelectAndReportExceptions(GetSourceCode, context, _id)
            .AddSource(context);

        context
            .SyntaxProvider.ForAttributeWithMetadataNameOfClassesAndRecords("Len.Abp.Wpf.DependencyPropertyAttribute`1")
            .SelectManyAttributes()
            .Combine(framework)
            .Combine(version)
            .SelectAndReportExceptions(PrepareData, context, _id)
            .WhereNotNull()
            .SelectAndReportExceptions(GetSourceCode, context, _id)
            .AddSource(context);
    }

    private static FileWithName GetSourceCode((ClassData Class, DependencyPropertyData DependencyProperty) data)
    {
        //Debugger.Launch();
        return new FileWithName(
            Name: $"{data.Class.FullName}.Properties.{data.DependencyProperty.Name}.g.cs",
            Text: GenerateDependencyProperty(data.Class, data.DependencyProperty)
        );
    }

    #region Prepare Data

    private static (ClassData Class, DependencyPropertyData DependencyProperty)? PrepareData(
        ((ClassWithAttributesContext context, Framework framework) left, string version) tuple
    )
    {
        var (((_, attributes, classSyntax, classSymbol), framework), version) = tuple;
        if (attributes.FirstOrDefault() is not { } attribute)
        {
            return null;
        }

        var classData = classSymbol.GetClassData(framework, version);
        var dependencyPropertyData = CreateDependencyPropertyData(
            attribute,
            framework,
            version,
            classSyntax.TryFindAttributeSyntax(attribute)
        );

        return (classData, dependencyPropertyData);
    }

    #endregion Prepare Data

    #region CreateDependencyPropertyData

    private static DependencyPropertyData CreateDependencyPropertyData(
        AttributeData attribute,
        Framework framework,
        string version,
        AttributeSyntax? attributeSyntax = null
    )
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
            framework,
            version,
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

    readonly record struct DependencyPropertyData(
        Framework Framework,
        string Version,
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

    private static string GenerateCoerce(ClassData @class, DependencyPropertyData property)
    {
        if (!property.Coerce)
            return string.Empty;

        var code = $"""
            [global::System.CodeDom.Compiler.GeneratedCode("{nameof(DependencyPropertyGenerator)}", "{@class.Version}")]
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

    private static string GenerateDependencyProperty(ClassData @class, DependencyPropertyData property)
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
                [global::System.CodeDom.Compiler.GeneratedCode("{{nameof(DependencyPropertyGenerator)}}", "{{@class.Version}}")]
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
                [global::System.CodeDom.Compiler.GeneratedCode("{{nameof(DependencyPropertyGenerator)}}", "{{@class.Version}}")]
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

                {{GenerateCoerce(@class, property)}}

                {{GenerateIsValid(@class, property)}}

                [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]
                [global::System.CodeDom.Compiler.GeneratedCode("{{nameof(DependencyPropertyGenerator)}}", "{{@class.Version}}")]
                partial void On{{property.Name}}Changed({{property.Type}} newValue);

                [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]
                [global::System.CodeDom.Compiler.GeneratedCode("{{nameof(DependencyPropertyGenerator)}}", "{{@class.Version}}")]
                partial void On{{property.Name}}Changed({{property.Type}} oldValue, {{property.Type}} newValue);
            }
            """;

        return code.RemoveBlankLines();
    }

    private static string GenerateIsValid(ClassData @class, DependencyPropertyData property)
    {
        if (!property.Validate)
            return string.Empty;

        var code = $"""
            [global::System.CodeDom.Compiler.GeneratedCode("{nameof(DependencyPropertyGenerator)}", "{@class.Version}")]
                [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]
                private static partial bool Is{property.Name}Valid({property.Type}? value);
            """;

        return code;
    }

    private static string GeneratePropertyChangedCallback(ClassData @class, DependencyPropertyData property)
    {
        var methodName = $"On{property.Name}Changed";
        var isChanged0 = ExistsMethod(@class, $"{methodName}()");
        var isChanged1 = ExistsMethod(@class, $"{methodName}({property.Type})");
        var isChanged2 = ExistsMethod(@class, $"{methodName}({property.Type}, {property.Type})");

        if (!isChanged0 && !isChanged1 && !isChanged2)
            return "propertyChangedCallback: null,";

        var code = $$"""
            propertyChangedCallback: static (s, e) =>
                            {
                                if (s is not {{@class.Type}} control)
                                    return;

                                {{(isChanged0 ? $"control.{methodName}();" : "")}}
                                {{(isChanged1 ? $"control.{methodName}(({property.Type})e.NewValue);" : "")}}
                                {{(isChanged2 ? $"control.{methodName}(({property.Type})e.OldValue, ({property.Type})e.NewValue);" : "")}}
                            },
            """;
        return code;
    }

    #endregion Generate Code
}
