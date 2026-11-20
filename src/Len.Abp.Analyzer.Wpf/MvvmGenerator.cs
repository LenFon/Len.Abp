using Len.Abp.Analyzer;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

[Generator]
internal class MvvmGenerator()
    : BasicGenerator<MvvmData>("Len.Abp.Wpf.Mvvm.ViewAttribute", "Len.Abp.Wpf.Mvvm.ViewAttribute`1")
{
    /// <inheritdoc/>
    /// <remarks>
    /// 文件名带生成器标识：同一类型可能同时被多个生成器处理（例如视图上既有
    /// <c>[View]</c> 又有 <c>[DependencyProperty]</c>），不带标识会产出同名 hint name，
    /// Roslyn 抛异常导致该类一个源文件都不产出。
    /// </remarks>
    protected override string CreateFileName((ClassData Class, MvvmData Data, string Version) context) =>
        $"{context.Class.HintName}.{nameof(MvvmGenerator)}.g.cs";

    /// <inheritdoc/>
    protected override string CreateSourceCode((ClassData Class, MvvmData Data, string Version) context) =>
        ViewSourceWriter.CreateSourceCode(context);

    /// <inheritdoc/>
    protected override (ClassData Class, MvvmData Data, string Version)? PrepareData((ClassContext, string) context)
    {
        var ((semanticModel, attributes, _, classSymbol), version) = context;
        if (attributes.FirstOrDefault() is not { } attribute)
        {
            return null;
        }

        // 参数非法或类型未声明 partial 时不生成代码：
        // 前者会产出无法编译的源文件，后者会与编译器的 CS0260 重复报错。
        if (!GenerationPreconditions.IsSatisfied(context.Item1))
        {
            return null;
        }

        // 静态类与结构体无法承载 IView 的实例成员：静态类报 CS0708/CS0710/CS0714，
        // 结构体报 CS0261（生成器写 partial class）且 DataContext 不存在。
        // 不生成代码，由 ReportDiagnostics 报告 LENABP012。
        if (HostTypeDiagnostics.GetViewProblem(classSymbol) != HostTypeProblem.None)
        {
            return null;
        }

        // 泛型视图无法生成注册特性：特性参数不能使用类型参数（CS8968/CS0416）。
        // 不生成代码，由 ReportDiagnostics 报告 LENABP008。
        // 外层类型是泛型时同样无法生成：注册特性里会出现 `Outer<T>.PageView`，
        // 其中的 T 是类型参数，编译器同样拒绝（CS0416）。
        if (MvvmDiagnostics.IsGenericOrNestedInGeneric(classSymbol))
        {
            return null;
        }

        var classData = classSymbol.GetClassData();
        var data = MvvmData.Parse(attribute);

        // 值类型视图模型无法满足 IView<TViewModel> 的 where T : class 约束（CS0452）。
        // 不生成代码，由 ReportDiagnostics 报告 LENABP010。
        if (!MvvmDiagnostics.IsReferenceTypeViewModel(attribute))
        {
            return null;
        }

        // 开放泛型视图模型无法用作 IView<TViewModel> 的类型参数：生成的基类型会变成
        // 未绑定泛型名（CS7003），分部声明也无法与用户声明匹配。
        // 不生成代码，由 ReportDiagnostics 报告 LENABP011。
        if (MvvmDiagnostics.IsOpenGenericViewModel(attribute))
        {
            return null;
        }

        // 用户可能已经手写了接收视图模型的构造函数，此时不再生成，避免重复定义。
        data = data with
        {
            HasViewModelConstructor = ViewModelRegistrationAnalyzer.HasViewModelConstructor(classSymbol, data.ViewModel),
            RequiredParameters = RequiredParameterParser.Parse(attribute, semanticModel, classSymbol),
            ViewModelRegistration = ViewModelRegistrationAnalyzer.Analyze(attribute),
            ViewModelContract = ViewModelRegistrationAnalyzer.AnalyzeContract(attribute),
        };

        if (!data.HasViewModelConstructor && !classData.CanChainToParameterlessConstructor)
        {
            // 生成的构造函数依赖 : this()，没有可链式调用的无参构造函数时编译报
            // CS1729 或 CS7036（取决于视图声明了几个构造函数），错误位置指向生成文件，
            // 很难反推到视图声明。不生成代码，由诊断报告 LENABP001。
            return null;
        }

        return (classData, data, version);
    }

    /// <inheritdoc/>
    protected override void ReportDiagnostics(SourceProductionContext context, ClassContext classContext) =>
        MvvmDiagnostics.Report(context, classContext);

    /// <inheritdoc/>
    /// <remarks>
    /// 视图模型的分部声明单独成文件，与视图分开。必须聚合后按视图模型去重：
    /// 一个视图模型可能被多个视图共用，逐个产出会得到同名 hint name（CS8785）。
    /// 去重键用视图模型的完全限定名，多个视图指向同一视图模型时只保留第一份。
    /// </remarks>
    protected override void RegisterAdditionalSources(
        IncrementalGeneratorInitializationContext context,
        IncrementalValueProvider<ImmutableArray<(ClassData Class, MvvmData Data, string Version)>> prepared)
    {
        context.RegisterSourceOutput(
            prepared,
            static (ctx, items) =>
            {
                foreach (var file in ViewModelSourceWriter.CreateFiles(items))
                {
                    ctx.AddSource(file.Name, file.Text);
                }
            }
        );
    }

    /// <inheritdoc/>
    protected override void RegisterAdditionalOutputs(IncrementalGeneratorInitializationContext context)
    {
        var version = context.GetVersion(GetType());

        // 必须同时取两个特性名：实际用法几乎都是 [View<T>]，只取非泛型版本会漏掉绝大多数视图。
        var nonGeneric = context.SyntaxProvider
            .GetClasses("Len.Abp.Wpf.Mvvm.ViewAttribute")
            .Combine(version)
            .Select(static (pair, _) => pair.Left);

        var generic = context.SyntaxProvider
            .GetClasses("Len.Abp.Wpf.Mvvm.ViewAttribute`1")
            .Combine(version)
            .Select(static (pair, _) => pair.Left);

        context.RegisterSourceOutput(
            nonGeneric.Collect().Combine(generic.Collect()),
            static (ctx, pair) => ViewNameDiagnostics.ReportDuplicateViewNames(ctx, [.. pair.Left, .. pair.Right])
        );
    }
}
