using Microsoft.CodeAnalysis;

namespace Len.Abp.Analyzer;

/// <summary>
/// 判断视图模型能否由生成器自动注册，并报告相应诊断。
/// </summary>
/// <remarks>
/// 三个前提：视图模型是具体类型、声明为 <c>partial</c>、构造函数可注入。
/// 任一不满足都不生成注册，由 <see cref="ReportDiagnostics"/> 报告诊断。
/// 已手工声明生命周期接口的视图模型同样跳过，避免重复注册。
/// </remarks>
internal static class ViewModelRegistrationAnalyzer
{
    /// <summary>
    /// 判断视图模型能否自动注册，并计算其生命周期。
    /// </summary>
    /// <param name="attribute">视图特性数据。</param>
    /// <returns>可注册时返回注册信息；否则返回 <see langword="null"/>。</returns>
    public static ViewModelRegistration? Analyze(AttributeData attribute)
    {
        if (GetViewModelSymbol(attribute) is not { } viewModel)
        {
            return null;
        }

        // 已手工声明生命周期接口：尊重使用者的选择，不重复生成。
        if (HasLifetimeInterface(viewModel))
        {
            return null;
        }

        if (!IsPartial(viewModel) || !IsConstructorInjectable(viewModel))
        {
            return null;
        }

        return new ViewModelRegistration(
            Class: viewModel.GetClassData(),
            Lifetime: GetViewModelLifetime(attribute)
        );
    }

    /// <summary>
    /// 报告视图模型的自动注册与契约诊断。
    /// </summary>
    /// <param name="context">源生成上下文。</param>
    /// <param name="classContext">类上下文。</param>
    /// <remarks>
    /// <para>
    /// 非 <c>partial</c> 时按是否声明了 <c>RequiredParameters</c> 区分严重度：
    /// 声明了就是错误（契约成员生成不了，参数校验会静默失效），
    /// 没声明只是警告（不能自动注册，可手动注册）。
    /// </para>
    /// <para>
    /// <b>契约诊断不受生命周期接口影响</b>：已手工声明 <c>ITransientDependency</c> 等接口
    /// 只说明"不需要自动注册"，与"能否生成契约成员"是两回事。契约成员生成不了，
    /// 参数校验就会静默失效——这比不能自动注册严重得多，必须照常报告。
    /// </para>
    /// </remarks>
    public static void ReportDiagnostics(SourceProductionContext context, ClassContext classContext)
    {
        if (classContext.Attributes.FirstOrDefault() is not { } attribute
            || GetViewModelSymbol(attribute) is not { } viewModel)
        {
            return;
        }

        var location = viewModel.Locations.FirstOrDefault() ?? classContext.ClassSyntax.Identifier.GetLocation();

        if (!IsPartial(viewModel))
        {
            // 契约诊断优先且不受生命周期接口影响：LENABP009 是错误级，
            // 而 LENABP006 只是"不能自动注册"的警告。
            if (HasRequiredParameters(attribute))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(DiagnosticDescriptors.ViewModelMustBePartialForContract, location, viewModel.Name)
                );

                return;
            }

            // 注册诊断才受生命周期接口影响：已手工声明接口时不需要自动注册。
            if (!HasLifetimeInterface(viewModel))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(DiagnosticDescriptors.ViewModelMustBePartial, location, viewModel.Name)
                );
            }

            return;
        }

        // 构造函数可注入性只影响自动注册，已手工声明生命周期接口时无需检查。
        if (HasLifetimeInterface(viewModel) || IsConstructorInjectable(viewModel))
        {
            return;
        }

        var parameters = string.Join(
            ", ",
            viewModel.InstanceConstructors
                .Where(constructor => !constructor.IsImplicitlyDeclared)
                .SelectMany(constructor => constructor.Parameters)
                .Select(parameter => $"{parameter.Type.Name} {parameter.Name}")
                .Distinct()
        );

        context.ReportDiagnostic(
            Diagnostic.Create(DiagnosticDescriptors.ViewModelConstructorNotInjectable, location, viewModel.Name, parameters)
        );
    }

    /// <summary>
    /// 判断视图特性是否声明了必需参数。
    /// </summary>
    /// <param name="attribute">视图特性数据。</param>
    /// <returns>声明了至少一个必需参数时返回 <see langword="true"/>。</returns>
    private static bool HasRequiredParameters(AttributeData attribute)
    {
        var constant = attribute.GetNamedArgument("RequiredParameters");

        return constant is { IsNull: false, Kind: TypedConstantKind.Array, Values.Length: > 0 };
    }

    /// <summary>
    /// 判断类型是否已声明接收视图模型的构造函数。
    /// </summary>
    /// <param name="classSymbol">视图类型符号。</param>
    /// <param name="viewModel">视图模型的完全限定类型名。</param>
    /// <returns>存在匹配的构造函数返回 true。</returns>
    public static bool HasViewModelConstructor(INamedTypeSymbol classSymbol, string viewModel) =>
        classSymbol.InstanceConstructors.Any(constructor =>
            !constructor.IsImplicitlyDeclared
            && constructor.Parameters.Length == 1
            && string.Equals(
                constructor.Parameters[0].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                viewModel,
                StringComparison.Ordinal
            )
        );

    /// <summary>
    /// 解析视图模型契约成员的生成位置。
    /// </summary>
    /// <param name="attribute">视图特性数据。</param>
    /// <returns>可生成契约时返回位置信息；否则返回 <see langword="null"/>。</returns>
    /// <remarks>
    /// 只要求视图模型是具体类且声明为 <c>partial</c>——契约成员要追加到它上面。
    /// 不检查构造函数可注入性：那是自动注册的前提，与契约无关。
    /// </remarks>
    public static ViewModelContract? AnalyzeContract(AttributeData attribute)
    {
        if (GetViewModelSymbol(attribute) is not { } viewModel || !IsPartial(viewModel))
        {
            return null;
        }

        return new ViewModelContract(viewModel.GetClassData());
    }

    /// <summary>
    /// 取视图特性指向的、声明为 <c>partial</c> 的视图模型符号。
    /// </summary>
    /// <param name="attribute">视图特性数据。</param>
    /// <returns>视图模型类型符号；不是具体类或未声明 <c>partial</c> 时返回 <see langword="null"/>。</returns>
    /// <remarks>
    /// 供需要视图模型符号本身（而不是生成位置）的检查使用，例如目录约定校验。
    /// 非 <c>partial</c> 时返回 <see langword="null"/>：那种情况生成器不会为视图模型追加任何成员，
    /// 针对它的检查都没有意义。
    /// </remarks>
    public static INamedTypeSymbol? GetPartialViewModel(AttributeData attribute) =>
        GetViewModelSymbol(attribute) is { } viewModel && IsPartial(viewModel) ? viewModel : null;

    /// <summary>
    /// 读取视图特性声明的视图模型类型符号。
    /// </summary>
    /// <param name="attribute">视图特性数据。</param>
    /// <returns>视图模型类型；不是具体类或不是闭合类型时返回 <see langword="null"/>。</returns>
    /// <remarks>
    /// 开放泛型（<c>PageViewModel&lt;&gt;</c>）被排除：它无法满足
    /// <c>IView&lt;TViewModel&gt;</c> 的 <c>where TViewModel : class</c> 约束，
    /// 生成的基类型会变成未绑定泛型名（CS7003），分部声明也无法与用户声明匹配。
    /// 由 <see cref="ReportDiagnostics"/> 报告 <c>LENABP011</c>。
    /// </remarks>
    private static INamedTypeSymbol? GetViewModelSymbol(AttributeData attribute)
    {
        var symbol = attribute.GetTypeArgument(0, 0);

        return symbol as INamedTypeSymbol is { TypeKind: TypeKind.Class, IsAbstract: false, IsUnboundGenericType: false } viewModel
            ? viewModel
            : null;
    }

    /// <summary>
    /// 判断类型是否已声明 ABP 生命周期标记接口。
    /// </summary>
    private static bool HasLifetimeInterface(INamedTypeSymbol type) =>
        type.AllInterfaces.Any(i =>
            i.ToDisplayString() is "Volo.Abp.DependencyInjection.ITransientDependency"
                or "Volo.Abp.DependencyInjection.ISingletonDependency"
                or "Volo.Abp.DependencyInjection.IScopedDependency"
        );

    /// <summary>
    /// 判断类型是否声明为 <c>partial</c>。
    /// </summary>
    /// <remarks>
    /// 只检查类型自身的任一语法声明：分部类只要有一处写了 <c>partial</c>，
    /// 所有声明都必须是 <c>partial</c>，因此任一命中即可。
    /// </remarks>
    private static bool IsPartial(INamedTypeSymbol type) =>
        type.DeclaringSyntaxReferences.Any(reference =>
            reference.GetSyntax() is Microsoft.CodeAnalysis.CSharp.Syntax.TypeDeclarationSyntax declaration
            && declaration.Modifiers.Any(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PartialKeyword)
        );

    /// <summary>
    /// 判断视图模型的构造函数能否由容器构造。
    /// </summary>
    /// <remarks>
    /// 无参构造、或所有参数都是接口 / 类（<c>string</c> 与 <c>object</c> 除外）时视为可注入。
    /// 值类型、数组、枚举与泛型参数没有默认注册，出现时几乎必然是设计问题。
    /// </remarks>
    private static bool IsConstructorInjectable(INamedTypeSymbol type)
    {
        var constructors = type.InstanceConstructors
            .Where(constructor => !constructor.IsImplicitlyDeclared)
            .ToArray();

        // 没有显式构造函数：编译器合成的无参构造可用。
        if (constructors.Length == 0)
        {
            return true;
        }

        return constructors.Any(constructor => constructor.Parameters.All(IsInjectableParameter));
    }

    /// <summary>
    /// 判断单个构造参数能否由容器提供。
    /// </summary>
    /// <remarks>
    /// 接口与具体类都可能被注册到容器；值类型、数组、枚举、泛型参数没有默认注册。
    /// <c>string</c> 与 <c>object</c> 虽然是类，但 MS DI 同样不会为它们提供实例，需单独排除。
    /// </remarks>
    private static bool IsInjectableParameter(IParameterSymbol parameter) =>
        parameter.Type.TypeKind is TypeKind.Interface or TypeKind.Class
        && parameter.Type.SpecialType is not (SpecialType.System_String or SpecialType.System_Object);

    /// <summary>
    /// 读取视图模型的注册生命周期：显式指定 <c>ViewModelLifetime</c> 时用它，否则跟随视图的 <c>Lifetime</c>。
    /// </summary>
    /// <remarks>
    /// 特性命名参数不支持可空枚举，因此用 <c>HasViewModelLifetime</c> 标记是否显式赋值。
    /// </remarks>
    private static ServiceLifetimeKind GetViewModelLifetime(AttributeData attribute)
    {
        var hasExplicit = attribute.GetNamedArgument("HasViewModelLifetime").To<bool>();

        return hasExplicit
            ? attribute.GetNamedArgument("ViewModelLifetime").ToServiceLifetimeKind()
            : attribute.GetNamedArgument("Lifetime").ToServiceLifetimeKind();
    }
}
