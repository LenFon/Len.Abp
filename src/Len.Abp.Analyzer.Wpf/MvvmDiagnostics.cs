using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Len.Abp.Analyzer;

/// <summary>
/// 视图生成器的诊断报告：宿主类型、泛型、视图模型种类、构造函数与目录约定。
/// </summary>
/// <remarks>
/// <para>
/// 全部诊断集中在这里而不是散在生成器里：<see cref="MvvmGenerator.PrepareData"/>
/// 返回 <see langword="null"/> 时管线会跳过该类，诊断就再也没机会报告，
/// 因此诊断必须与生成逻辑分开注册。
/// </para>
/// <para>
/// 每条诊断都先经 <see cref="AttributeDeduplication"/> 去重：
/// <c>GetClasses</c> 为每个特性产出一个条目，同一个类可能进来多次。
/// </para>
/// </remarks>
internal static class MvvmDiagnostics
{
    /// <summary>
    /// 报告视图生成器的全部诊断。
    /// </summary>
    /// <param name="context">源生成上下文。</param>
    /// <param name="classContext">类上下文。</param>
    public static void Report(SourceProductionContext context, ClassContext classContext)
    {
        PartialTypeDiagnostics.Report(context, classContext);
        AttributeArgumentDiagnostics.Report(context, classContext);
        ReportInvalidHostType(context, classContext);
        ReportGenericView(context, classContext);
        ReportValueTypeViewModel(context, classContext);
        ReportOpenGenericViewModel(context, classContext);
        ReportMissingParameterlessConstructor(context, classContext);
        ReportUnresolvedRequiredParameterTypes(context, classContext);
        ViewModelRegistrationAnalyzer.ReportDiagnostics(context, classContext);
        ReportFolderConvention(context, classContext);
    }

    /// <summary>
    /// 报告视图与视图模型没有按约定分目录存放。
    /// </summary>
    /// <param name="context">源生成上下文。</param>
    /// <param name="classContext">类上下文。</param>
    /// <remarks>
    /// 视图与视图模型必须分别在 <c>Views</c> 与 <c>ViewModels</c> 目录下。
    /// 视图模型只在它是 <c>partial</c> 时检查：非 <c>partial</c> 时生成器不会为它追加成员，
    /// 目录问题与生成器无关，报告只会增加噪音。
    /// </remarks>
    private static void ReportFolderConvention(SourceProductionContext context, ClassContext classContext)
    {
        FolderConventionDiagnostics.ReportViewFolder(context, classContext);

        if (classContext.Attributes.FirstOrDefault() is not { } attribute
            || ViewModelRegistrationAnalyzer.GetPartialViewModel(attribute) is not { } viewModel)
        {
            return;
        }

        FolderConventionDiagnostics.ReportViewModelFolder(context, classContext, viewModel);
    }

    /// <summary>
    /// 报告无法解析的导航必需参数类型。
    /// </summary>
    /// <param name="context">源生成上下文。</param>
    /// <param name="classContext">类上下文。</param>
    /// <remarks>
    /// <para>
    /// 解析失败时生成器把字典值写成 <see langword="null"/>，退化为"只校验存在性"。
    /// 这与"使用者没写类型"的产出完全相同，但前者几乎总是笔误，
    /// 不报告的话类型校验会静默失效。
    /// </para>
    /// <para>
    /// 按特性去重：一个视图只应用一个视图特性（双特性由 <c>LENABP004</c> 拦截），
    /// 这里仍用 <see cref="AttributeDeduplication.IsFirstOfType"/> 保持与其它诊断一致。
    /// </para>
    /// </remarks>
    private static void ReportUnresolvedRequiredParameterTypes(SourceProductionContext context, ClassContext classContext)
    {
        if (!AttributeDeduplication.IsFirstOfType(classContext))
        {
            return;
        }

        if (classContext.Attributes.FirstOrDefault() is not { } attribute)
        {
            return;
        }

        var parameters = RequiredParameterParser.Parse(attribute, classContext.SemanticModel, classContext.ClassSymbol);

        foreach (var parameter in parameters)
        {
            // 没写类型是合法用法（只校验存在性），只有"写了却解析不出来"才报告。
            if (parameter.Type is not null || parameter.DeclaredType is not { Length: > 0 } declared)
            {
                continue;
            }

            context.ReportDiagnostic(
                Diagnostic.Create(
                    DiagnosticDescriptors.RequiredParameterTypeUnresolved,
                    GetRequiredParameterLocation(classContext, parameter.Name),
                    classContext.ClassSymbol.Name,
                    parameter.Name,
                    declared
                )
            );
        }
    }

    /// <summary>
    /// 取必需参数声明在源码中的位置。
    /// </summary>
    /// <param name="classContext">类上下文。</param>
    /// <param name="name">参数名。</param>
    /// <returns>该参数所在的字符串字面量位置；取不到时退化为类型标识符。</returns>
    /// <remarks>
    /// 定位到具体那一条声明，而不是整个特性：一个视图可能声明多个参数，
    /// 只报其中一条时使用者需要知道是哪一条。
    /// </remarks>
    private static Location GetRequiredParameterLocation(ClassContext classContext, string name)
    {
        var fallback = classContext.ClassSyntax.Identifier.GetLocation();

        if (classContext.Attributes.FirstOrDefault()?.ApplicationSyntaxReference?.GetSyntax() is not { } syntax)
        {
            return fallback;
        }

        // 在特性语法里找包含该参数名的字符串字面量。参数名可能被写成 "Id:int"，
        // 因此用"字面量文本以名称开头"匹配，而不是精确相等。
        foreach (var literal in syntax.DescendantNodes().OfType<LiteralExpressionSyntax>())
        {
            if (literal.IsKind(SyntaxKind.StringLiteralExpression)
                && literal.Token.ValueText.StartsWith(name, StringComparison.Ordinal))
            {
                return literal.GetLocation();
            }
        }

        return fallback;
    }

    /// <summary>
    /// 报告不合法的视图宿主类型，替代生成无法编译的代码。
    /// </summary>
    /// <param name="context">源生成上下文。</param>
    /// <param name="classContext">类上下文。</param>
    /// <remarks>
    /// 静态类不能声明实例成员（CS0708/CS0710/CS0714），
    /// 结构体不具备引用语义且生成器会写出 <c>partial class</c>（CS0261）。
    /// 两种情况都只报一条可操作诊断，而不是让使用者面对一堆分散的编译错误。
    /// </remarks>
    private static void ReportInvalidHostType(SourceProductionContext context, ClassContext classContext)
    {
        if (!AttributeDeduplication.IsFirstOfType(classContext))
        {
            return;
        }

        var problem = HostTypeDiagnostics.GetViewProblem(classContext.ClassSymbol);

        if (problem == HostTypeProblem.None)
        {
            return;
        }

        context.ReportDiagnostic(
            Diagnostic.Create(
                DiagnosticDescriptors.ViewHostTypeInvalid,
                classContext.ClassSyntax.Identifier.GetLocation(),
                classContext.ClassSymbol.Name,
                HostTypeDiagnostics.Describe(problem)
            )
        );
    }

    /// <summary>
    /// 报告值类型视图模型，替代生成违反泛型约束的代码。
    /// </summary>
    /// <param name="context">源生成上下文。</param>
    /// <param name="classContext">类上下文。</param>
    /// <remarks>
    /// 视图基类型固定为 <c>IView&lt;TViewModel&gt;</c>，该接口声明了
    /// <c>where TViewModel : class</c>。值类型视图模型会报 CS0452，
    /// 且值类型每次装箱都是独立副本，导航回调间的状态无法保持。
    /// </remarks>
    private static void ReportValueTypeViewModel(SourceProductionContext context, ClassContext classContext)
    {
        if (!AttributeDeduplication.IsFirstOfType(classContext))
        {
            return;
        }

        if (classContext.Attributes.FirstOrDefault() is not { } attribute || IsReferenceTypeViewModel(attribute))
        {
            return;
        }

        var viewModel = attribute.GetTypeArgument(0, 0);

        context.ReportDiagnostic(
            Diagnostic.Create(
                DiagnosticDescriptors.ViewModelMustBeReferenceType,
                classContext.ClassSyntax.Identifier.GetLocation(),
                viewModel?.ToDisplayString() ?? "（未知）"
            )
        );
    }

    /// <summary>
    /// 判断视图特性指定的视图模型是否为引用类型。
    /// </summary>
    /// <param name="attribute">视图特性数据。</param>
    /// <returns>视图模型是引用类型时返回 <see langword="true"/>。</returns>
    /// <remarks>
    /// 解析不到视图模型时返回 <see langword="true"/>：那种情况由其它诊断负责，
    /// 这里不应重复报告。
    /// </remarks>
    public static bool IsReferenceTypeViewModel(AttributeData attribute) =>
        attribute.GetTypeArgument(0, 0) is not { IsValueType: true };

    /// <summary>
    /// 判断视图特性指定的视图模型是否为开放泛型。
    /// </summary>
    /// <param name="attribute">视图特性数据。</param>
    /// <returns>视图模型是开放泛型时返回 <see langword="true"/>。</returns>
    /// <remarks>
    /// 只判 <see cref="INamedTypeSymbol.IsUnboundGenericType"/>：它精确对应
    /// <c>typeof(PageViewModel&lt;&gt;)</c> 这种写法。闭合泛型（<c>PageViewModel&lt;int&gt;</c>）
    /// 是合法用法，由 <see cref="ClassData.DeclarationName"/> 生成未闭合的类型参数列表来支持。
    /// </remarks>
    public static bool IsOpenGenericViewModel(AttributeData attribute) =>
        attribute.GetTypeArgument(0, 0) is INamedTypeSymbol { IsUnboundGenericType: true };

    /// <summary>
    /// 报告开放泛型视图模型，替代生成无法编译的代码。
    /// </summary>
    /// <param name="context">源生成上下文。</param>
    /// <param name="classContext">类上下文。</param>
    /// <remarks>
    /// <c>IView&lt;TViewModel&gt;</c> 声明了 <c>where TViewModel : class</c>，
    /// 开放泛型无法作为类型实参，生成的基类型会变成未绑定泛型名（CS7003）。
    /// </remarks>
    private static void ReportOpenGenericViewModel(SourceProductionContext context, ClassContext classContext)
    {
        if (!AttributeDeduplication.IsFirstOfType(classContext))
        {
            return;
        }

        if (classContext.Attributes.FirstOrDefault() is not { } attribute || !IsOpenGenericViewModel(attribute))
        {
            return;
        }

        var viewModel = attribute.GetTypeArgument(0, 0);

        context.ReportDiagnostic(
            Diagnostic.Create(
                DiagnosticDescriptors.ViewModelMustNotBeOpenGeneric,
                classContext.ClassSyntax.Identifier.GetLocation(),
                viewModel?.OriginalDefinition.ToDisplayString() ?? "（未知）"
            )
        );
    }

    /// <summary>
    /// 报告泛型视图，替代生成无法编译的代码。
    /// </summary>
    /// <param name="context">源生成上下文。</param>
    /// <param name="classContext">类上下文。</param>
    /// <remarks>
    /// 生成的注册特性形如 <c>[ExposeKeyedService&lt;Page&lt;T&gt;&gt;]</c>，
    /// 而 C# 不允许特性参数使用类型参数（CS8968 / CS0416）。
    /// 视图自身泛型、或嵌套在泛型外层类型中，都会让注册特性带上类型参数，
    /// 两种情况都提前报告可操作的诊断，比让用户看到晦涩的编译错误更好。
    /// </remarks>
    private static void ReportGenericView(SourceProductionContext context, ClassContext classContext)
    {
        if (!AttributeDeduplication.IsFirstOfType(classContext))
        {
            return;
        }

        if (!IsGenericOrNestedInGeneric(classContext.ClassSymbol))
        {
            return;
        }

        context.ReportDiagnostic(
            Diagnostic.Create(
                DiagnosticDescriptors.ViewMustNotBeGeneric,
                classContext.ClassSyntax.Identifier.GetLocation(),
                classContext.ClassSymbol.Name
            )
        );
    }

    /// <summary>
    /// 判断类型自身或其任一外层类型是否带类型参数。
    /// </summary>
    /// <param name="classSymbol">待判断的类型符号。</param>
    /// <returns>自身或外层类型泛型时返回 <see langword="true"/>。</returns>
    /// <remarks>
    /// 注册特性里引用 <c>Outer&lt;T&gt;.PageView</c> 时，<c>T</c> 是类型参数，
    /// 编译器拒绝把它作为特性参数（CS0416）。因此外层泛型与自身泛型等价。
    /// </remarks>
    public static bool IsGenericOrNestedInGeneric(INamedTypeSymbol classSymbol)
    {
        for (var current = classSymbol; current is not null; current = current.ContainingType)
        {
            if (current.TypeParameters.Length > 0)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 报告缺少无参构造函数：生成的构造函数依赖 <c>: this()</c>，链式调用会失败。
    /// </summary>
    /// <param name="context">源生成上下文。</param>
    /// <param name="classContext">类上下文。</param>
    /// <remarks>
    /// 生成的构造函数依赖 <c>: this()</c>。用户已手写接收视图模型的构造函数时不会生成新构造函数，
    /// 也就不需要链式调用，此时不报告。
    /// </remarks>
    private static void ReportMissingParameterlessConstructor(SourceProductionContext context, ClassContext classContext)
    {
        if (classContext.Attributes.FirstOrDefault() is not { } attribute)
        {
            return;
        }

        var classData = classContext.ClassSymbol.GetClassData();
        var data = MvvmData.Parse(attribute);

        if (data.HasViewModelConstructor || classData.CanChainToParameterlessConstructor)
        {
            return;
        }

        // 视图模型构造函数是否已手写需要按符号判断，隐式转换只拿到特性数据。
        if (ViewModelRegistrationAnalyzer.HasViewModelConstructor(classContext.ClassSymbol, data.ViewModel))
        {
            return;
        }

        context.ReportDiagnostic(
            Diagnostic.Create(
                DiagnosticDescriptors.MissingParameterlessConstructor,
                classContext.ClassSyntax.Identifier.GetLocation(),
                classContext.ClassSymbol.Name
            )
        );
    }
}
