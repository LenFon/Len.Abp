using Len.Abp.Analyzer.TestSupport;
using Xunit;

namespace Len.Abp.Analyzer.Wpf.Tests;

/// <summary>
/// 覆盖视图构造函数生成：无参构造的补齐与 <c>: this()</c> 链式调用。
/// </summary>
/// <remarks>
/// 这些用例必须编译验证：只断言生成文本无法发现"<c>: this()</c> 找不到目标"，
/// 那正是该缺陷的形态——文本看起来完全正常，编译报 CS7036。
/// </remarks>
public class MvvmGeneratorConstructorTests
{
    /// <summary>
    /// 可编译的最小桩：只含生成器读取的类型与特性。
    /// </summary>
    /// <remarks>
    /// 不能用 <see cref="GeneratorTestSources.Stubs"/>：其中
    /// <c>System.Windows.DependencyProperty</c> 同时声明为类与静态类，本身无法编译。
    /// </remarks>
    private const string Stubs = """
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
                System.Collections.Generic.IReadOnlyDictionary<string, System.Type?> RequiredNavigationParameters =>
                    new System.Collections.Generic.Dictionary<string, System.Type?>(System.StringComparer.OrdinalIgnoreCase);
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
    /// 视图不声明任何构造函数时，生成器补一个无参构造函数，代码可编译。
    /// </summary>
    /// <remarks>
    /// 回归：分部类一旦有构造函数（包括生成器自己加的），编译器就不再合成无参构造，
    /// <c>: this()</c> 找不到目标，报 CS7036。WPF 里"不写构造函数"是常见写法。
    /// </remarks>
    [Fact]
    public void ViewWithoutConstructor_GeneratesParameterlessConstructor()
    {
        var (sources, errors) = GeneratorTestHost.RunAndCompile(
            new MvvmGenerator(),
            Stubs
                + """

                  namespace Demo
                  {
                      public partial class PageViewModel { }

                      [Len.Abp.Wpf.Mvvm.View<PageViewModel>(ViewName = "page")]
                      public partial class PageView : Len.Abp.Wpf.FrameworkElement { }
                  }
                  """
        );

        var generated = sources.ViewSource().Text;

        Assert.True(errors.Count == 0, string.Join("\n", errors));
        Assert.Contains("public PageView()", generated);
        Assert.Contains("public PageView(global::Demo.PageViewModel viewModel) : this()", generated);
    }

    /// <summary>
    /// 视图有 <c>InitializeComponent</c>（即带 XAML）时，补的无参构造必须调用它。
    /// </summary>
    /// <remarks>
    /// 不调用 <c>InitializeComponent</c> 的视图没有视觉树，界面会是空白。
    /// </remarks>
    [Fact]
    public void ViewWithoutConstructor_WithInitializeComponent_CallsIt()
    {
        var (sources, errors) = GeneratorTestHost.RunAndCompile(
            new MvvmGenerator(),
            Stubs
                + """

                  namespace Demo
                  {
                      public partial class PageViewModel { }

                      [Len.Abp.Wpf.Mvvm.View<PageViewModel>(ViewName = "page")]
                      public partial class PageView : Len.Abp.Wpf.FrameworkElement
                      {
                          private void InitializeComponent() { }
                      }
                  }
                  """
        );

        var generated = sources.ViewSource().Text;

        Assert.True(errors.Count == 0, string.Join("\n", errors));
        Assert.Contains("InitializeComponent();", generated);
    }

    /// <summary>
    /// 视图已声明无参构造函数时，生成器不重复生成。
    /// </summary>
    [Fact]
    public void ViewWithParameterlessConstructor_DoesNotGenerateAnother()
    {
        var (sources, errors) = GeneratorTestHost.RunAndCompile(
            new MvvmGenerator(),
            Stubs
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
                  """
        );

        var generated = sources.ViewSource().Text;

        Assert.True(errors.Count == 0, string.Join("\n", errors));
        Assert.DoesNotContain("public PageView()", generated);
        Assert.Contains("public PageView(global::Demo.PageViewModel viewModel) : this()", generated);
    }

    /// <summary>
    /// 视图只有带参构造函数时仍报告 <c>LENABP001</c>，且不生成代码。
    /// </summary>
    /// <remarks>
    /// 生成器不能替用户决定带参构造函数该传什么，只能要求补一个无参版本。
    /// </remarks>
    [Fact]
    public void ViewWithOnlyParameterizedConstructor_ReportsLENABP001()
    {
        var (sources, diagnostics) = GeneratorTestHost.RunWithErrors(
            new MvvmGenerator(),
            Stubs
                + """

                  namespace Demo
                  {
                      public partial class PageViewModel { }

                      [Len.Abp.Wpf.Mvvm.View<PageViewModel>(ViewName = "page")]
                      public partial class PageView : Len.Abp.Wpf.FrameworkElement
                      {
                          public PageView(int value) { }
                      }
                  }
                  """
        );

        Assert.Empty(sources);
        Assert.Contains(diagnostics, d => d.Id == "LENABP001");
    }

    /// <summary>
    /// 用户手写接收视图模型的构造函数时，不生成任何构造函数。
    /// </summary>
    [Fact]
    public void ViewWithViewModelConstructor_GeneratesNothing()
    {
        var (sources, errors) = GeneratorTestHost.RunAndCompile(
            new MvvmGenerator(),
            Stubs
                + """

                  namespace Demo
                  {
                      public partial class PageViewModel { }

                      [Len.Abp.Wpf.Mvvm.View<PageViewModel>(ViewName = "page")]
                      public partial class PageView : Len.Abp.Wpf.FrameworkElement
                      {
                          public PageView(PageViewModel viewModel) { DataContext = viewModel; }
                      }
                  }
                  """
        );

        var generated = sources.ViewSource().Text;

        Assert.True(errors.Count == 0, string.Join("\n", errors));
        Assert.DoesNotContain("public PageView()", generated);
        Assert.DoesNotContain(": this()", generated);
    }
}
