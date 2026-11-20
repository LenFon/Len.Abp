using Len.Abp.Analyzer.TestSupport;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Len.Abp.Analyzer.Wpf.Tests;

/// <summary>
/// 覆盖 <c>MvvmGenerator</c> 为视图生成的成员：<c>IView</c> 实现、视图名、键控注册与生命周期接口。
/// </summary>
/// <remarks>
/// 视图模型侧的注册与契约成员由 <see cref="MvvmGeneratorViewModelRegistrationTests"/> 与
/// <see cref="MvvmGeneratorRequiredParametersTests"/> 覆盖。
/// </remarks>
public class MvvmGeneratorTests
{
    private const string Stubs = """
        namespace Microsoft.Extensions.DependencyInjection
        {
            public enum ServiceLifetime
            {
                Singleton = 0,
                Scoped = 1,
                Transient = 2,
            }
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
            public interface IShell { }

            public class FrameworkElement
            {
                public object? DataContext { get; set; }
            }
        }

        namespace Volo.Abp.DependencyInjection
        {
            public interface ITransientDependency { }
            public interface ISingletonDependency { }

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
    /// Shell 视图不会声明 ITransientDependency。
    /// </summary>
    [Fact]
    public void ShellView_DoesNotDeclareTransientDependency()
    {
        var sources = GeneratorTestHost.Run(
            new MvvmGenerator(),
            Stubs
                + """

                namespace Demo
                {
                    public class ShellViewModel { }

                    [Len.Abp.Wpf.Mvvm.View<ShellViewModel>(ViewName = "main", IsShell = true)]
                    public partial class ShellView { }
                }
                """
        );

        var generated = sources.ViewSource();

        Assert.Contains("global::Len.Abp.Wpf.IShell", generated.Text);
        Assert.DoesNotContain("ITransientDependency", generated.Text);
    }

    /// <summary>
    /// 普通视图默认声明 ITransientDependency。
    /// </summary>
    [Fact]
    public void NonShellView_DeclaresTransientDependency()
    {
        var sources = GeneratorTestHost.Run(
            new MvvmGenerator(),
            Stubs
                + """

                namespace Demo
                {
                    public class PageViewModel { }

                    [Len.Abp.Wpf.Mvvm.View<PageViewModel>(ViewName = "page")]
                    public partial class PageView { }
                }
                """
        );

        var generated = sources.ViewSource();

        Assert.Contains("global::Volo.Abp.DependencyInjection.ITransientDependency", generated.Text);
        Assert.DoesNotContain("global::Len.Abp.Wpf.IShell", generated.Text);
    }

    /// <summary>
    /// 指定 Scoped 生命周期的视图声明 IScopedDependency。
    /// </summary>
    [Fact]
    public void View_WithScopedLifetime_DeclaresScopedDependency()
    {
        var sources = GeneratorTestHost.Run(
            new MvvmGenerator(),
            Stubs
                + """

                namespace Demo
                {
                    public class PageViewModel { }

                    [Len.Abp.Wpf.Mvvm.View<PageViewModel>(
                        ViewName = "page",
                        Lifetime = Microsoft.Extensions.DependencyInjection.ServiceLifetime.Scoped)]
                    public partial class PageView { }
                }
                """
        );

        var generated = sources.ViewSource();

        Assert.Contains("global::Volo.Abp.DependencyInjection.IScopedDependency", generated.Text);
        Assert.DoesNotContain("ITransientDependency", generated.Text);
    }

    /// <summary>
    /// 指定 Singleton 生命周期的视图声明 ISingletonDependency。
    /// </summary>
    [Fact]
    public void View_WithSingletonLifetime_DeclaresSingletonDependency()
    {
        var sources = GeneratorTestHost.Run(
            new MvvmGenerator(),
            Stubs
                + """

                namespace Demo
                {
                    public class PageViewModel { }

                    [Len.Abp.Wpf.Mvvm.View<PageViewModel>(
                        ViewName = "page",
                        Lifetime = Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton)]
                    public partial class PageView { }
                }
                """
        );

        var generated = sources.ViewSource();

        Assert.Contains("global::Volo.Abp.DependencyInjection.ISingletonDependency", generated.Text);
        Assert.DoesNotContain("ITransientDependency", generated.Text);
    }

    /// <summary>
    /// 显式指定 Transient 与默认行为一致。
    /// </summary>
    [Fact]
    public void View_WithExplicitTransientLifetime_DeclaresTransientDependency()
    {
        var sources = GeneratorTestHost.Run(
            new MvvmGenerator(),
            Stubs
                + """

                namespace Demo
                {
                    public class PageViewModel { }

                    [Len.Abp.Wpf.Mvvm.View<PageViewModel>(
                        ViewName = "page",
                        Lifetime = Microsoft.Extensions.DependencyInjection.ServiceLifetime.Transient)]
                    public partial class PageView { }
                }
                """
        );

        var generated = sources.ViewSource();

        Assert.Contains("global::Volo.Abp.DependencyInjection.ITransientDependency", generated.Text);
    }

    /// <summary>
    /// Shell 视图固定为单例，显式指定的生命周期被忽略。
    /// </summary>
    [Fact]
    public void ShellView_IgnoresLifetimeSetting()
    {
        var sources = GeneratorTestHost.Run(
            new MvvmGenerator(),
            Stubs
                + """

                namespace Demo
                {
                    public class ShellViewModel { }

                    [Len.Abp.Wpf.Mvvm.View<ShellViewModel>(
                        ViewName = "main",
                        IsShell = true,
                        Lifetime = Microsoft.Extensions.DependencyInjection.ServiceLifetime.Transient)]
                    public partial class ShellView { }
                }
                """
        );

        var generated = sources.ViewSource();

        Assert.Contains("global::Len.Abp.Wpf.IShell", generated.Text);
        Assert.DoesNotContain("ITransientDependency", generated.Text);
        Assert.DoesNotContain("IScopedDependency", generated.Text);
    }

    /// <summary>
    /// 指定 ViewName 的视图会注册键控服务。
    /// </summary>
    [Fact]
    public void ViewWithName_RegistersKeyedServices()
    {
        var sources = GeneratorTestHost.Run(
            new MvvmGenerator(),
            Stubs
                + """

                namespace Demo
                {
                    public class PageViewModel { }

                    [Len.Abp.Wpf.Mvvm.View<PageViewModel>(ViewName = "page")]
                    public partial class PageView { }
                }
                """
        );

        var generated = sources.ViewSource();

        Assert.Contains("ExposeKeyedService<global::Len.Abp.Wpf.Mvvm.IView>(\"page\")", generated.Text);
        Assert.Contains("public string ViewName => \"page\";", generated.Text);
    }

    /// <summary>
    /// 注册特性里的视图类型用完全限定名，不是声明名。
    /// </summary>
    /// <remarks>
    /// 回归：<c>ClassData</c> 原先只有一个 <c>Name</c> 字段，
    /// 既用于分部声明（需要未闭合类型参数），又用于 <c>typeof()</c>（需要闭合实参）。
    /// 拆成 <c>DeclarationName</c> 与 <c>Type</c> 后，注册特性必须用后者：
    /// 用前者会让嵌套视图变成 <c>typeof(PageView)</c>，在别的命名空间里解析不到。
    /// </remarks>
    [Fact]
    public void RegistrationAttributes_UseFullyQualifiedViewType()
    {
        var sources = GeneratorTestHost.Run(
            new MvvmGenerator(),
            Stubs
                + """

                namespace Demo
                {
                    public class PageViewModel { }

                    [Len.Abp.Wpf.Mvvm.View<PageViewModel>(ViewName = "page")]
                    public partial class PageView { }
                }
                """
        );

        var generated = sources.ViewSource().Text;

        Assert.Contains("typeof(global::Demo.PageView)", generated);
        Assert.Contains("ExposeKeyedService<global::Demo.PageView>(\"page\")", generated);
        Assert.DoesNotContain("typeof(PageView)", generated);
    }

    /// <summary>
    /// 未指定 ViewName 的视图不注册键控服务。
    /// </summary>
    [Fact]
    public void ViewWithoutName_DoesNotRegisterKeyedServices()
    {
        var sources = GeneratorTestHost.Run(
            new MvvmGenerator(),
            Stubs
                + """

                namespace Demo
                {
                    public class PageViewModel { }

                    [Len.Abp.Wpf.Mvvm.View<PageViewModel>]
                    public partial class PageView { }
                }
                """
        );

        var generated = sources.ViewSource();

        Assert.DoesNotContain("ExposeKeyedService", generated.Text);
    }

    /// <summary>
    /// 生成的构造函数与只读属性是合法 C#，能通过编译。
    /// </summary>
    /// <remarks>
    /// 只断言文本片段时，非法代码（例如缺少无参构造函数导致的 CS1729）会漏到用户项目里。
    /// </remarks>
    [Fact]
    public void View_GeneratedCode_Compiles()
    {
        var (_, errors) = GeneratorTestHost.RunAndCompile(
            new MvvmGenerator(),
            Stubs
                + """

                namespace Demo
                {
                    public class PageViewModel { }

                    [Len.Abp.Wpf.Mvvm.View<PageViewModel>(ViewName = "page")]
                    public partial class PageView : Len.Abp.Wpf.FrameworkElement
                    {
                        public PageView() { }
                    }
                }
                """
        );

        Assert.Empty(errors);
    }

    /// <summary>
    /// 视图通过构造函数注入视图模型并写入 DataContext。
    /// </summary>
    [Fact]
    public void View_InjectsViewModelThroughConstructor()
    {
        var sources = GeneratorTestHost.Run(
            new MvvmGenerator(),
            Stubs
                + """

                namespace Demo
                {
                    public class PageViewModel { }

                    [Len.Abp.Wpf.Mvvm.View<PageViewModel>(ViewName = "page")]
                    public partial class PageView { }
                }
                """
        );

        var generated = sources.ViewSource();

        Assert.Contains("public PageView(global::Demo.PageViewModel viewModel) : this()", generated.Text);
        Assert.Contains("DataContext = viewModel;", generated.Text);
        Assert.Contains("global::Len.Abp.Wpf.Mvvm.IView<global::Demo.PageViewModel>", generated.Text);
    }

    /// <summary>
    /// 视图模型属性只读，不再依赖属性注入写入。
    /// </summary>
    [Fact]
    public void View_ViewModelProperty_IsReadOnly()
    {
        var sources = GeneratorTestHost.Run(
            new MvvmGenerator(),
            Stubs
                + """

                namespace Demo
                {
                    public class PageViewModel { }

                    [Len.Abp.Wpf.Mvvm.View<PageViewModel>(ViewName = "page")]
                    public partial class PageView { }
                }
                """
        );

        var generated = sources.ViewSource();

        Assert.Contains("public global::Demo.PageViewModel ViewModel => (global::Demo.PageViewModel)DataContext;", generated.Text);
        Assert.DoesNotContain("required", generated.Text);
        Assert.DoesNotContain("init => DataContext = value;", generated.Text);
    }

    /// <summary>
    /// 用户已手写接收视图模型的构造函数时，生成器不再重复生成。
    /// </summary>
    [Fact]
    public void View_WithExistingViewModelConstructor_DoesNotGenerateAnother()
    {
        var sources = GeneratorTestHost.Run(
            new MvvmGenerator(),
            Stubs
                + """

                namespace Demo
                {
                    public class PageViewModel { }

                    [Len.Abp.Wpf.Mvvm.View<PageViewModel>(ViewName = "page")]
                    public partial class PageView
                    {
                        public PageView(PageViewModel viewModel) { }
                    }
                }
                """
        );

        var generated = sources.ViewSource();

        Assert.DoesNotContain("public PageView(global::Demo.PageViewModel viewModel)", generated.Text);
    }

    /// <summary>
    /// 视图只声明带参构造函数时报告 <c>LENABP001</c>，且不生成源文件。
    /// </summary>
    /// <remarks>
    /// 旧实现是在生成文件里输出 <c>#error</c>，位置指向生成文件、无法跳转。
    /// 改为诊断后位置指向视图类型标识符。
    /// </remarks>
    [Fact]
    public void View_WithoutParameterlessConstructor_ReportsLENABP001()
    {
        var (sources, diagnostics) = GeneratorTestHost.RunWithErrors(
            new MvvmGenerator(),
            Stubs
                + """

                namespace Demo
                {
                    public class PageViewModel { }

                    [Len.Abp.Wpf.Mvvm.View<PageViewModel>(ViewName = "page")]
                    public partial class PageView
                    {
                        public PageView(int value) { }
                    }
                }
                """
        );

        Assert.Empty(sources);

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "LENABP001");
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.NotEqual(Location.None, diagnostic.Location);
        Assert.Contains("无参构造函数", diagnostic.GetMessage());
    }
}
