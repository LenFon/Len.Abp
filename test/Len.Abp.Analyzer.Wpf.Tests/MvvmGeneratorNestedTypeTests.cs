using Len.Abp.Analyzer.TestSupport;
using Xunit;

namespace Len.Abp.Analyzer.Wpf.Tests;

/// <summary>
/// 覆盖嵌套类型与 record 视图模型的声明位置。
/// </summary>
/// <remarks>
/// 这些用例断言的是**生成代码编译后的元数据**，而不是生成文本：
/// 文本断言无法区分"契约成员生成到真正的嵌套类型"与"生成到同名的幽灵类型"，
/// 也无法确认特性挂在哪一层类型上。
/// </remarks>
public class MvvmGeneratorNestedTypeTests
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
    /// 嵌套视图模型的契约与生命周期接口必须落在真正的嵌套类型上。
    /// </summary>
    /// <remarks>
    /// 回归：位置信息曾只取"简单名 + 命名空间"，外层类型被丢弃，
    /// 生成到 <c>Demo.PageViewModel</c> 这个幽灵类型上。编译通过、零警告，
    /// 但真正的 <c>Demo.Outer.PageViewModel</c> 两个接口都没实现。
    /// </remarks>
    [Fact]
    public void NestedViewModel_ContractAndLifetimeLandOnNestedType()
    {
        var source = Stubs
            + """

              namespace Demo
              {
                  public partial class Outer
                  {
                      public partial class PageViewModel { }
                  }

                  [Len.Abp.Wpf.Mvvm.View<Outer.PageViewModel>(ViewName = "page", RequiredParameters = new[] { "Id:int" })]
                  public partial class PageView : Len.Abp.Wpf.FrameworkElement
                  {
                      public PageView() { }
                  }
              }
              """;

        var (sources, errors) = GeneratorTestHost.RunAndCompile(new MvvmGenerator(), source);
        Assert.True(errors.Count == 0, string.Join("\n", errors));

        var compilation = GeneratorTestHost.RunToCompilation(new MvvmGenerator(), source);
        var nested = compilation.GetTypeByMetadataName("Demo.Outer+PageViewModel");

        Assert.NotNull(nested);
        Assert.Contains(nested!.AllInterfaces, i => i.Name == "INavigationContract");
        Assert.Contains(nested.AllInterfaces, i => i.Name == "ITransientDependency");
        Assert.Null(compilation.GetTypeByMetadataName("Demo.PageViewModel"));
    }

    /// <summary>
    /// 嵌套视图的注册特性必须贴在最内层类型上，而不是外层类型。
    /// </summary>
    /// <remarks>
    /// 回归：特性曾拼在整个类型块之前，编译器把它绑定到第一个类型声明（外层）。
    /// 外层不实现 <c>IView</c>，真正的视图从未注册，导航必然失败。
    /// </remarks>
    [Fact]
    public void NestedView_RegistrationAttributesTargetInnerType()
    {
        var source = Stubs
            + """

              namespace Demo
              {
                  public partial class PageViewModel { }

                  public partial class Outer
                  {
                      [Len.Abp.Wpf.Mvvm.View<PageViewModel>(ViewName = "page")]
                      public partial class Inner : Len.Abp.Wpf.FrameworkElement
                      {
                          public Inner() { }
                      }
                  }
              }
              """;

        var (sources, errors) = GeneratorTestHost.RunAndCompile(new MvvmGenerator(), source);
        Assert.True(errors.Count == 0, string.Join("\n", errors));

        var generated = sources.ViewSource().Text;
        var outerIndex = generated.IndexOf("partial class Outer", StringComparison.Ordinal);
        var attributeIndex = generated.IndexOf("ExposeKeyedService", StringComparison.Ordinal);
        var innerIndex = generated.IndexOf("partial class Inner", StringComparison.Ordinal);

        Assert.True(outerIndex >= 0 && attributeIndex >= 0 && innerIndex >= 0, generated);
        Assert.True(
            outerIndex < attributeIndex && attributeIndex < innerIndex,
            $"注册特性必须位于外层声明之后、内层声明之前。实际：Outer@{outerIndex} Attribute@{attributeIndex} Inner@{innerIndex}\n{generated}"
        );
    }

    /// <summary>
    /// record 视图模型必须生成 <c>partial record</c>，否则编译报 CS0261。
    /// </summary>
    /// <remarks>
    /// 回归：生成器曾把 <c>partial class</c> 写死，视图侧已处理 record、视图模型侧没有。
    /// </remarks>
    [Fact]
    public void RecordViewModel_GeneratesPartialRecord()
    {
        var source = Stubs
            + """

              namespace Demo
              {
                  public partial record PageViewModel;

                  [Len.Abp.Wpf.Mvvm.View<PageViewModel>(ViewName = "page", RequiredParameters = new[] { "Id:int" })]
                  public partial class PageView : Len.Abp.Wpf.FrameworkElement
                  {
                      public PageView() { }
                  }
              }
              """;

        var (sources, errors) = GeneratorTestHost.RunAndCompile(new MvvmGenerator(), source);
        var generated = sources.ViewModelSource().Text;

        Assert.True(errors.Count == 0, string.Join("\n", errors));
        Assert.Contains("partial record PageViewModel", generated);
        Assert.DoesNotContain("partial class PageViewModel", generated);
    }

    /// <summary>
    /// 嵌套的 record 视图模型同时保留外层类型链与 <c>record</c> 关键字。
    /// </summary>
    [Fact]
    public void NestedRecordViewModel_PreservesBothContainingTypeAndRecordKeyword()
    {
        var source = Stubs
            + """

              namespace Demo
              {
                  public partial class Outer
                  {
                      public partial record PageViewModel;
                  }

                  [Len.Abp.Wpf.Mvvm.View<Outer.PageViewModel>(ViewName = "page", RequiredParameters = new[] { "Id:int" })]
                  public partial class PageView : Len.Abp.Wpf.FrameworkElement
                  {
                      public PageView() { }
                  }
              }
              """;

        var (sources, errors) = GeneratorTestHost.RunAndCompile(new MvvmGenerator(), source);
        Assert.True(errors.Count == 0, string.Join("\n", errors));

        var compilation = GeneratorTestHost.RunToCompilation(new MvvmGenerator(), source);
        var nested = compilation.GetTypeByMetadataName("Demo.Outer+PageViewModel");

        Assert.NotNull(nested);
        Assert.Contains(nested!.AllInterfaces, i => i.Name == "INavigationContract");
        Assert.Null(compilation.GetTypeByMetadataName("Demo.PageViewModel"));
    }

    /// <summary>
    /// 视图模型嵌套在 <c>record struct</c> 外层类型中时，外层声明保留 <c>record struct</c>。
    /// </summary>
    /// <remarks>
    /// 回归：关键字选择原先只判 <c>IsRecord</c>，<c>record struct</c> 同时满足
    /// <c>IsRecord</c> 与 <c>TypeKind.Struct</c>，被误判成 <c>record</c>，
    /// 生成 <c>partial record Host</c> 与用户的 <c>record struct</c> 冲突（CS0261）。
    /// </remarks>
    [Fact]
    public void ViewModelNestedInRecordStructOuter_GeneratesRecordStructDeclaration()
    {
        var source = Stubs
            + """

              namespace Demo
              {
                  public partial record struct Host
                  {
                      public partial class PageViewModel { }
                  }

                  [Len.Abp.Wpf.Mvvm.View<Host.PageViewModel>(ViewName = "page")]
                  public partial class PageView : Len.Abp.Wpf.FrameworkElement
                  {
                      public PageView() { }
                  }
              }
              """;

        var (sources, errors) = GeneratorTestHost.RunAndCompile(new MvvmGenerator(), source);
        var generated = sources.ViewModelSource().Text;

        Assert.True(errors.Count == 0, string.Join("\n", errors));
        Assert.Contains("partial record struct Host", generated);
        Assert.DoesNotContain("partial record Host", generated);
    }

    /// <summary>
    /// 视图模型嵌套在泛型外层类型中时，外层声明使用未闭合的类型参数。
    /// </summary>
    /// <remarks>
    /// 回归：外层声明原先用 <c>MinimallyQualifiedFormat</c> 的显示名，
    /// 闭合泛型被写成 <c>partial class Host&lt;int&gt;</c>（CS1001/CS0264）。
    /// partial 声明只能用未闭合的类型参数。
    /// </remarks>
    [Fact]
    public void ViewModelNestedInGenericOuter_UsesUnclosedTypeParameters()
    {
        var source = Stubs
            + """

              namespace Demo
              {
                  public partial class Host<T>
                  {
                      public partial class PageViewModel { }
                  }

                  [Len.Abp.Wpf.Mvvm.View<Host<int>.PageViewModel>(ViewName = "page")]
                  public partial class PageView : Len.Abp.Wpf.FrameworkElement
                  {
                      public PageView() { }
                  }
              }
              """;

        var (sources, errors) = GeneratorTestHost.RunAndCompile(new MvvmGenerator(), source);
        var generated = string.Concat(sources.Select(s => s.Text));

        Assert.True(errors.Count == 0, string.Join("\n", errors));
        Assert.Contains("partial class Host<T>", generated);
        Assert.DoesNotContain("partial class Host<int>", generated);
    }

    /// <summary>
    /// 闭合泛型视图模型生成未闭合的类型参数列表，编译通过。
    /// </summary>
    /// <remarks>
    /// 回归：<c>ClassData.Name</c> 原用 <c>MinimallyQualifiedFormat</c> 的显示名，
    /// 闭合泛型被写成 <c>partial class PageViewModel&lt;int&gt;</c>（CS1001/CS0264）。
    /// 此前只修了外层类型，最内层仍用显示名。
    /// </remarks>
    [Fact]
    public void ClosedGenericViewModel_GeneratesUnclosedTypeParameters()
    {
        var source = Stubs
            + """

              namespace Demo
              {
                  public partial class PageViewModel<T> { }

                  [Len.Abp.Wpf.Mvvm.View<PageViewModel<int>>(ViewName = "page")]
                  public partial class PageView : Len.Abp.Wpf.FrameworkElement
                  {
                      public PageView() { }
                  }
              }
              """;

        var (sources, errors) = GeneratorTestHost.RunAndCompile(new MvvmGenerator(), source);
        var generated = string.Concat(sources.Select(s => s.Text));

        Assert.True(errors.Count == 0, string.Join("\n", errors));
        Assert.Contains("partial class PageViewModel<T>", generated);
        Assert.DoesNotContain("partial class PageViewModel<int>", generated);

        // 注册特性必须用闭合实参，不能用类型参数（CS0416）。
        Assert.Contains("IView<global::Demo.PageViewModel<int>>", generated);
    }

    /// <summary>
    /// 嵌套在泛型外层中的闭合泛型视图模型同时保留两层未闭合参数。
    /// </summary>
    [Fact]
    public void ClosedGenericViewModelInGenericOuter_GeneratesBothUnclosed()
    {
        var source = Stubs
            + """

              namespace Demo
              {
                  public partial class Host<T>
                  {
                      public partial class PageViewModel<U> { }
                  }

                  [Len.Abp.Wpf.Mvvm.View<Host<int>.PageViewModel<string>>(ViewName = "page")]
                  public partial class PageView : Len.Abp.Wpf.FrameworkElement
                  {
                      public PageView() { }
                  }
              }
              """;

        var (sources, errors) = GeneratorTestHost.RunAndCompile(new MvvmGenerator(), source);
        var generated = string.Concat(sources.Select(s => s.Text));

        Assert.True(errors.Count == 0, string.Join("\n", errors));
        Assert.Contains("partial class Host<T>", generated);
        Assert.Contains("partial class PageViewModel<U>", generated);
        Assert.DoesNotContain("partial class Host<int>", generated);
        Assert.DoesNotContain("partial class PageViewModel<string>", generated);
    }

    /// <summary>
    /// 多类型参数的闭合泛型视图模型保留全部参数名。
    /// </summary>
    [Fact]
    public void ClosedGenericViewModelWithTwoParameters_KeepsBothParameterNames()
    {
        var source = Stubs
            + """

              namespace Demo
              {
                  public partial class PageViewModel<TKey, TValue> { }

                  [Len.Abp.Wpf.Mvvm.View<PageViewModel<int, string>>(ViewName = "page")]
                  public partial class PageView : Len.Abp.Wpf.FrameworkElement
                  {
                      public PageView() { }
                  }
              }
              """;

        var (sources, errors) = GeneratorTestHost.RunAndCompile(new MvvmGenerator(), source);
        var generated = string.Concat(sources.Select(s => s.Text));

        Assert.True(errors.Count == 0, string.Join("\n", errors));
        Assert.Contains("partial class PageViewModel<TKey, TValue>", generated);
    }
}
