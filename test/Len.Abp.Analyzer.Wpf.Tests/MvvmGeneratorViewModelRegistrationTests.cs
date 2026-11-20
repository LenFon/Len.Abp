using Len.Abp.Analyzer.TestSupport;
using Xunit;

namespace Len.Abp.Analyzer.Wpf.Tests;

/// <summary>
/// 覆盖 <c>MvvmGenerator</c> 为视图模型生成注册声明的行为。
/// </summary>
public class MvvmGeneratorViewModelRegistrationTests
{
    /// <summary>
    /// 可编译的最小桩：只含生成器读取的类型与特性。
    /// </summary>
    /// <remarks>
    /// <see cref="GeneratorTestSources.Stubs"/> 覆盖全部生成器，其中
    /// <c>System.Windows.DependencyProperty</c> 同时声明为类与静态类，本身无法编译，
    /// 因此需要编译验证的用例改用这份最小桩。
    /// </remarks>
    private const string CompilableStubs = """
        namespace Microsoft.Extensions.DependencyInjection
        {
            public enum ServiceLifetime { Singleton = 0, Scoped = 1, Transient = 2 }
        }

        namespace Len.Abp.Wpf.Mvvm
        {
            public interface IView
            {
                object ViewModel { get; }
                string ViewName { get; }
            }

            public interface IView<out TViewModel> : IView where TViewModel : class
            {
                new TViewModel ViewModel { get; }
            }

            [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
            public class ViewAttribute : System.Attribute
            {
                public ViewAttribute(System.Type viewModel) { }
                public bool IsShell { get; set; }
                public System.Type ViewModel { get; set; } = null!;
                public string? ViewName { get; set; }
                public string[]? RequiredParameters { get; set; }
                public Microsoft.Extensions.DependencyInjection.ServiceLifetime ViewModelLifetime { get; set; }
                public bool HasViewModelLifetime { get; set; }
                public Microsoft.Extensions.DependencyInjection.ServiceLifetime Lifetime { get; set; }
            }

            [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
            public class ViewAttribute<T> : ViewAttribute { public ViewAttribute() : base(typeof(T)) { } }
        }

        namespace Len.Abp.Wpf
        {
            public class FrameworkElement { public object? DataContext { get; set; } }
        }

        namespace Len.Abp.Wpf.Navigation
        {
            public interface INavigationContract
            {
                System.Collections.Generic.IReadOnlyDictionary<string, System.Type?> RequiredNavigationParameters =>
                    EmptyParameters;

                private static readonly System.Collections.Generic.IReadOnlyDictionary<string, System.Type?> EmptyParameters =
                    new System.Collections.Generic.Dictionary<string, System.Type?>(System.StringComparer.OrdinalIgnoreCase);
            }
        }

        namespace Volo.Abp.DependencyInjection
        {
            public interface ITransientDependency { }
            public interface ISingletonDependency { }
            public interface IScopedDependency { }

            [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true)]
            public class ExposeServicesAttribute : System.Attribute
            {
                public ExposeServicesAttribute(params System.Type[] serviceTypes) { }
            }

            [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true)]
            public class ExposeKeyedServiceAttribute<T> : System.Attribute
            {
                public ExposeKeyedServiceAttribute(string key) { }
            }
        }
        """;

    /// <summary>
    /// 声明 partial 且无参构造的视图模型生成生命周期接口。
    /// </summary>
    [Fact]
    public void PartialViewModel_GeneratesLifetimeInterface()
    {
        var generated = Generate("public partial class PageViewModel { }");

        Assert.Contains("partial class PageViewModel : global::Volo.Abp.DependencyInjection.ITransientDependency", generated);
    }

    /// <summary>
    /// 视图的 Lifetime 传递到视图模型。
    /// </summary>
    [Fact]
    public void ViewLifetime_IsInheritedByViewModel()
    {
        var generated = Generate("public partial class PageViewModel { }", "Lifetime = Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton");

        Assert.Contains("partial class PageViewModel : global::Volo.Abp.DependencyInjection.ISingletonDependency", generated);
    }

    /// <summary>
    /// 显式指定 ViewModelLifetime 时覆盖视图的 Lifetime。
    /// </summary>
    [Fact]
    public void ViewModelLifetime_OverridesViewLifetime()
    {
        var generated = Generate(
            "public partial class PageViewModel { }",
            "Lifetime = Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton, "
                + "ViewModelLifetime = Microsoft.Extensions.DependencyInjection.ServiceLifetime.Scoped, "
                + "HasViewModelLifetime = true"
        );

        Assert.Contains("partial class PageViewModel : global::Volo.Abp.DependencyInjection.IScopedDependency", generated);
    }

    /// <summary>
    /// 已手工声明生命周期接口时不重复生成。
    /// </summary>
    [Fact]
    public void ViewModelWithLifetimeInterface_IsNotRegistered()
    {
        var generated = Generate(
            "public partial class PageViewModel : Volo.Abp.DependencyInjection.ISingletonDependency { }"
        );

        Assert.DoesNotContain("partial class PageViewModel :", generated);
    }

    /// <summary>
    /// 未声明 partial 时不生成注册。
    /// </summary>
    [Fact]
    public void NonPartialViewModel_IsNotRegistered()
    {
        var generated = Generate("public class PageViewModel { }");

        Assert.Null(generated);
    }

    /// <summary>
    /// 构造函数参数全为接口时正常注册。
    /// </summary>
    [Fact]
    public void InjectableConstructor_IsRegistered()
    {
        var generated = Generate(
            "public partial class PageViewModel { public PageViewModel(System.IDisposable dependency) { } }"
        );

        Assert.Contains("partial class PageViewModel :", generated);
    }

    /// <summary>
    /// 构造函数含值类型参数时不生成注册。
    /// </summary>
    [Fact]
    public void NonInjectableConstructor_IsNotRegistered()
    {
        var generated = Generate("public partial class PageViewModel { public PageViewModel(int depth) { } }");

        Assert.DoesNotContain("partial class PageViewModel :", generated);
    }

    /// <summary>
    /// <c>string</c> 参数虽然类型种类是类，容器同样无法提供，不生成注册。
    /// </summary>
    [Fact]
    public void StringConstructorParameter_IsNotRegistered()
    {
        var generated = Generate("public partial class PageViewModel { public PageViewModel(string title) { } }");

        Assert.DoesNotContain("partial class PageViewModel :", generated);
    }

    /// <summary>
    /// 构造函数含数组参数时不生成注册。
    /// </summary>
    [Fact]
    public void ArrayConstructorParameter_IsNotRegistered()
    {
        var generated = Generate("public partial class PageViewModel { public PageViewModel(int[] ids) { } }");

        Assert.DoesNotContain("partial class PageViewModel :", generated);
    }

    /// <summary>
    /// 抽象视图模型不生成注册。
    /// </summary>
    [Fact]
    public void AbstractViewModel_IsNotRegistered()
    {
        var generated = Generate("public abstract partial class PageViewModel { }");

        Assert.Null(generated);
    }

    /// <summary>
    /// 视图模型与视图不同命名空间时，用块作用域单独声明。
    /// </summary>
    [Fact]
    public void ViewModelInDifferentNamespace_UsesBlockScope()
    {
        var source = GeneratorTestSources.With(
            "namespace Demo.ViewModels\n"
                + "{\n"
                + "    public partial class PageViewModel { }\n"
                + "}\n\n"
                + "namespace Demo.Views\n"
                + "{\n"
                + "    [Len.Abp.Wpf.Mvvm.View<Demo.ViewModels.PageViewModel>(ViewName = \"page\")]\n"
                + "    public partial class PageView { }\n"
                + "}\n"
        );

        var generated = GeneratorTestHost.Run(new MvvmGenerator(), source).ViewModelSource().Text;

        Assert.Contains("namespace Demo.ViewModels", generated);
        Assert.Contains("partial class PageViewModel :", generated);
    }

    /// <summary>
    /// 视图模型与视图同命名空间时，声明仍必须包在命名空间块内。
    /// </summary>
    /// <remarks>
    /// 裸声明会被编译器放进全局命名空间，生成的是 <c>global::PageViewModel</c>，
    /// 与真正的 <c>Demo.PageViewModel</c> 是两个类型：编译不报错，接口实现静默丢失。
    /// </remarks>
    [Fact]
    public void ViewModelInSameNamespace_IsStillWrappedInNamespaceBlock()
    {
        var source = CompilableStubs
            + """

              namespace Demo
              {
                  public partial class PageViewModel { }

                  [Len.Abp.Wpf.Mvvm.View<PageViewModel>(ViewName = "page")]
                  public partial class PageView : Len.Abp.Wpf.FrameworkElement
                  {
                      public PageView() { }
                  }
              }
              """;

        var (sources, errors) = GeneratorTestHost.RunAndCompile(new MvvmGenerator(), source);
        var generated = sources.ViewModelSource().Text;

        Assert.True(errors.Count == 0, string.Join("\n", errors));
        Assert.Contains("    partial class PageViewModel :", generated);
        Assert.DoesNotContain("\npartial class PageViewModel", generated);
    }

    /// <summary>
    /// 视图模型未声明 partial 时报告 LENABP006。
    /// </summary>
    [Fact]
    public void NonPartialViewModel_ReportsDiagnostic()
    {
        var diagnostics = GetDiagnostics("public class PageViewModel { }");

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "LENABP006");
        Assert.Equal(Microsoft.CodeAnalysis.DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.NotEqual(Microsoft.CodeAnalysis.Location.None, diagnostic.Location);
    }

    /// <summary>
    /// 声明了 RequiredParameters 但视图模型不是 partial 时报告 LENABP009（错误级）。
    /// </summary>
    /// <remarks>
    /// 契约成员必须生成到视图模型上，生成不了就意味着参数校验静默失效——
    /// 这比"不能自动注册"严重，因此是错误而不是警告。
    /// </remarks>
    [Fact]
    public void NonPartialViewModelWithRequiredParameters_ReportsLENABP009()
    {
        var diagnostics = GetErrorDiagnostics(
            "public class PageViewModel { }",
            """RequiredParameters = new[] { "Id" }"""
        );

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "LENABP009");
        Assert.Equal(Microsoft.CodeAnalysis.DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("PageViewModel", diagnostic.GetMessage());
        Assert.DoesNotContain(diagnostics, d => d.Id == "LENABP006");
    }

    /// <summary>
    /// 视图模型构造函数含不可注入参数时报告 LENABP007。
    /// </summary>
    [Fact]
    public void NonInjectableConstructor_ReportsDiagnostic()
    {
        var diagnostics = GetDiagnostics("public partial class PageViewModel { public PageViewModel(int depth) { } }");

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "LENABP007");
        Assert.Contains("depth", diagnostic.GetMessage());
    }

    /// <summary>
    /// 已手工声明生命周期接口时不报告诊断。
    /// </summary>
    [Fact]
    public void ViewModelWithLifetimeInterface_ReportsNoDiagnostic()
    {
        var diagnostics = GetDiagnostics(
            "public class PageViewModel : Volo.Abp.DependencyInjection.ISingletonDependency { }"
        );

        Assert.DoesNotContain(diagnostics, d => d.Id is "LENABP006" or "LENABP007");
    }

    /// <summary>
    /// 抽象视图模型不报告诊断。
    /// </summary>
    [Fact]
    public void AbstractViewModel_ReportsNoDiagnostic()
    {
        var diagnostics = GetDiagnostics("public abstract class PageViewModel { }");

        Assert.DoesNotContain(diagnostics, d => d.Id is "LENABP006" or "LENABP007");
    }

    /// <summary>
    /// 已声明生命周期接口但未声明 partial 且声明了必需参数时，仍报告 LENABP009。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 回归：生命周期接口只说明"不需要自动注册"，与"能否生成契约成员"是两回事。
    /// 原先 <c>ReportDiagnostics</c> 在入口处把"已实现生命周期接口"当作
    /// "不需要任何诊断"，契约诊断也被跳过——参数校验静默失效。
    /// </para>
    /// <para>
    /// 这条路径尤其危险：<c>LENABP006</c> 的提示正是"手动声明生命周期接口"，
    /// 使用者按提示操作反而让更严重的 <c>LENABP009</c> 也消失了。
    /// </para>
    /// </remarks>
    [Fact]
    public void NonPartialViewModelWithLifetimeInterfaceAndRequiredParameters_ReportsLENABP009()
    {
        var diagnostics = GetErrorDiagnostics(
            "public class PageViewModel : Volo.Abp.DependencyInjection.ITransientDependency { }",
            """RequiredParameters = new[] { "Id" }"""
        );

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "LENABP009");
        Assert.Equal(Microsoft.CodeAnalysis.DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("PageViewModel", diagnostic.GetMessage());
        Assert.DoesNotContain(diagnostics, d => d.Id == "LENABP006");
    }

    /// <summary>
    /// 已声明生命周期接口、未声明 partial、未声明必需参数时不报告诊断。
    /// </summary>
    /// <remarks>
    /// 保持既有行为：手工声明接口即视为自行管理注册，无需 <c>LENABP006</c>。
    /// 契约诊断只在声明了 <c>RequiredParameters</c> 时才需要。
    /// </remarks>
    [Fact]
    public void NonPartialViewModelWithLifetimeInterface_ReportsNoDiagnostic()
    {
        var diagnostics = GetDiagnostics(
            "public class PageViewModel : Volo.Abp.DependencyInjection.ITransientDependency { }"
        );

        Assert.DoesNotContain(diagnostics, d => d.Id is "LENABP006" or "LENABP009");
    }

    /// <summary>
    /// 已声明生命周期接口、partial、构造函数不可注入时不报告 LENABP007。
    /// </summary>
    /// <remarks>
    /// 构造函数可注入性只影响自动注册。已手工声明接口时容器注册由使用者负责，
    /// 框架不应再提示"无法自动注册"。
    /// </remarks>
    [Fact]
    public void PartialViewModelWithLifetimeInterfaceAndNonInjectableConstructor_ReportsNoDiagnostic()
    {
        var diagnostics = GetDiagnostics(
            "public partial class PageViewModel : Volo.Abp.DependencyInjection.ITransientDependency { public PageViewModel(int depth) { } }"
        );

        Assert.DoesNotContain(diagnostics, d => d.Id is "LENABP006" or "LENABP007" or "LENABP009");
    }

    /// <summary>
    /// 三种生命周期接口都触发契约诊断，不只 <c>ITransientDependency</c>。
    /// </summary>
    [Theory]
    [InlineData("ITransientDependency")]
    [InlineData("IScopedDependency")]
    [InlineData("ISingletonDependency")]
    public void NonPartialViewModelWithAnyLifetimeInterface_ReportsLENABP009(string interfaceName)
    {
        var diagnostics = GetErrorDiagnostics(
            $"public class PageViewModel : Volo.Abp.DependencyInjection.{interfaceName} {{ }}",
            """RequiredParameters = new[] { "Id" }"""
        );

        Assert.Single(diagnostics, d => d.Id == "LENABP009");
    }

    private static IReadOnlyList<Microsoft.CodeAnalysis.Diagnostic> GetDiagnostics(
        string viewModelDeclaration,
        string? attributeArguments = null
    )
    {
        var (_, diagnostics) = GeneratorTestHost.RunWithDiagnostics(
            new MvvmGenerator(),
            BuildSource(viewModelDeclaration, attributeArguments)
        );

        return diagnostics;
    }

    /// <summary>
    /// 与 <see cref="GetDiagnostics"/> 相同，但允许生成器报告错误级诊断。
    /// </summary>
    /// <remarks>
    /// <c>LENABP009</c> 是错误级，<see cref="GeneratorTestHost.RunWithDiagnostics"/>
    /// 会把它当作"生成器产生错误"直接抛出，因此需要走 <c>RunWithErrors</c>。
    /// </remarks>
    private static IReadOnlyList<Microsoft.CodeAnalysis.Diagnostic> GetErrorDiagnostics(
        string viewModelDeclaration,
        string? attributeArguments = null
    )
    {
        var (_, diagnostics) = GeneratorTestHost.RunWithErrors(
            new MvvmGenerator(),
            BuildSource(viewModelDeclaration, attributeArguments)
        );

        return diagnostics;
    }

    private static string BuildSource(string viewModelDeclaration, string? attributeArguments)
    {
        var arguments = attributeArguments is null
            ? "ViewName = \"page\""
            : $"ViewName = \"page\", {attributeArguments}";

        return GeneratorTestSources.With(
            "namespace Demo\n"
                + "{\n"
                + "    " + viewModelDeclaration + "\n\n"
                + $"    [Len.Abp.Wpf.Mvvm.View<PageViewModel>({arguments})]\n"
                + "    public partial class PageView { }\n"
                + "}\n"
        );
    }

    private static string? Generate(string viewModelDeclaration, string? extraArguments = null)
    {
        var arguments = extraArguments is null ? "ViewName = \"page\"" : $"ViewName = \"page\", {extraArguments}";

        var source = GeneratorTestSources.With(
            "namespace Demo\n"
                + "{\n"
                + "    " + viewModelDeclaration + "\n\n"
                + "    [Len.Abp.Wpf.Mvvm.View<PageViewModel>(" + arguments + ")]\n"
                + "    public partial class PageView { }\n"
                + "}\n"
        );

        return GeneratorTestHost.Run(new MvvmGenerator(), source).FindViewModelSource()?.Text;
    }

    /// <summary>
    /// 多个视图共用同一个视图模型时，视图模型声明只生成一次。
    /// </summary>
    /// <remarks>
    /// 视图与视图模型分成两个源文件后，按视图逐个产出视图模型文件会得到同名 hint name，
    /// Roslyn 抛异常导致零产出（CS8785）。示例里确实存在这种用法
    /// （三个选项卡页面共用一个视图模型），因此必须按视图模型去重。
    /// </remarks>
    [Fact]
    public void SharedViewModel_GeneratesSingleViewModelSource()
    {
        var source = CompilableStubs
            + """

              namespace Demo
              {
                  public partial class PageViewModel { }

                  [Len.Abp.Wpf.Mvvm.View<PageViewModel>(ViewName = "page-1")]
                  public partial class PageView1 : Len.Abp.Wpf.FrameworkElement { }

                  [Len.Abp.Wpf.Mvvm.View<PageViewModel>(ViewName = "page-2")]
                  public partial class PageView2 : Len.Abp.Wpf.FrameworkElement { }

                  [Len.Abp.Wpf.Mvvm.View<PageViewModel>(ViewName = "page-3")]
                  public partial class PageView3 : Len.Abp.Wpf.FrameworkElement { }
              }
              """;

        var (sources, errors) = GeneratorTestHost.RunAndCompile(new MvvmGenerator(), source);

        Assert.True(errors.Count == 0, string.Join("\n", errors));

        // 三个视图各一份文件，视图模型只有一份。
        Assert.Equal(3, sources.Count(file => !IsViewModelFile(file)));
        Assert.Single(sources, IsViewModelFile);
    }

    /// <summary>
    /// 两个视图分别指向同一泛型的两个闭合实例时，视图模型声明仍只生成一份。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 回归：<c>PageViewModel&lt;int&gt;</c> 与 <c>PageViewModel&lt;string&gt;</c> 是同一个分部声明
    /// <c>partial class PageViewModel&lt;T&gt;</c> 的两个闭合实例。按闭合类型去重会让两者各产出一份，
    /// 撞 CS0102 重复成员；按 <c>HintName</c> 去重则会产出同名 hint name（CS8785），零产出。
    /// </para>
    /// <para>
    /// 正确做法是按声明目标（未闭合的声明名 + 外层类型链）去重。
    /// </para>
    /// </remarks>
    [Fact]
    public void ClosedGenericViewModels_GenerateSingleDeclaration()
    {
        var source = CompilableStubs
            + """

              namespace Demo
              {
                  public partial class PageViewModel<T> { }

                  [Len.Abp.Wpf.Mvvm.View<PageViewModel<int>>(ViewName = "int-page")]
                  public partial class IntPageView : Len.Abp.Wpf.FrameworkElement { }

                  [Len.Abp.Wpf.Mvvm.View<PageViewModel<string>>(ViewName = "string-page")]
                  public partial class StringPageView : Len.Abp.Wpf.FrameworkElement { }
              }
              """;

        var (sources, errors) = GeneratorTestHost.RunAndCompile(new MvvmGenerator(), source);

        Assert.True(errors.Count == 0, string.Join("\n", errors));

        // 两个视图各一份文件，视图模型只有一份。
        Assert.Equal(2, sources.Count(file => !IsViewModelFile(file)));
        Assert.Single(sources, IsViewModelFile);
    }

    /// <summary>
    /// 共用视图模型时，各视图声明的必需参数取并集，不因处理顺序丢失。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 回归：原实现按视图模型去重后只保留第一份数据，若先处理到不带参数的视图，
    /// 共享视图模型就永远不会实现 <c>INavigationContract</c>，另一个视图声明的
    /// <c>["id:int"]</c> 静默失效。
    /// </para>
    /// <para>
    /// 这里把不带参数的视图写在前面，正是原来会丢失契约的顺序。
    /// </para>
    /// </remarks>
    [Fact]
    public void SharedViewModel_MergesRequiredParametersFromAllViews()
    {
        var source = CompilableStubs
            + """

              namespace Demo
              {
                  public partial class SharedViewModel { }

                  [Len.Abp.Wpf.Mvvm.View<SharedViewModel>(ViewName = "plain")]
                  public partial class PlainView : Len.Abp.Wpf.FrameworkElement { }

                  [Len.Abp.Wpf.Mvvm.View<SharedViewModel>(ViewName = "with-id", RequiredParameters = new[] { "id:int" })]
                  public partial class IdView : Len.Abp.Wpf.FrameworkElement { }
              }
              """;

        var (sources, errors) = GeneratorTestHost.RunAndCompile(new MvvmGenerator(), source);

        Assert.True(errors.Count == 0, string.Join("\n", errors));

        var viewModel = Assert.Single(sources, IsViewModelFile);

        Assert.Contains("global::Len.Abp.Wpf.Navigation.INavigationContract", viewModel.Text);
        Assert.Contains("[\"id\"] = typeof(int)", viewModel.Text);
    }

    private static bool IsViewModelFile(GeneratedSource source) =>
        source.HintName.EndsWith(".ViewModel.g.cs", StringComparison.Ordinal);
}
