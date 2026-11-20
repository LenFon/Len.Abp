using Len.Abp.Analyzer.TestSupport;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Len.Abp.Analyzer.Wpf.Tests;

/// <summary>
/// 覆盖：生成器必须拦截无法承载其成员的宿主类型。
/// </summary>
/// <remarks>
/// <para>
/// 三个缺陷同源：生成器不校验宿主类型，直接产出无法编译的代码。
/// 静态类视图报 9 条分散的错误（CS0708/CS0710/CS0714/CS0718/CS0103），
/// 结构体视图报 CS0261，静态类上的非附加依赖属性/路由事件报 CS0708/CS1057。
/// </para>
/// <para>
/// 修复后每种情况只报一条可操作诊断，且不生成代码。
/// 附加版本不受影响——它只生成静态方法，静态类宿主在编译层面合法
/// （附加依赖属性的运行期限制见 <see cref="StaticClassAttachedDependencyProperty_GeneratesCode"/>）。
/// </para>
/// </remarks>
public class HostTypeDiagnosticsTests
{
    /// <summary>
    /// 静态类视图报告 <c>LENABP012</c>，且不生成代码。
    /// </summary>
    /// <remarks>
    /// 回归：静态类不能实现接口（CS0714）、不能有实例构造函数（CS0710）、
    /// 不能声明实例成员（CS0708），生成的代码一条都用不了。
    /// </remarks>
    [Fact]
    public void StaticClassView_ReportsLENABP012()
    {
        var (sources, diagnostics) = RunMvvm(
            """
            public partial class PageViewModel { }

            [Len.Abp.Wpf.Mvvm.View<PageViewModel>(ViewName = "page")]
            public static partial class PageView { }
            """
        );

        Assert.Empty(sources);
        AssertSingleError(diagnostics, "LENABP012", "静态类");
    }

    /// <summary>
    /// 结构体视图报告 <c>LENABP012</c>，且不生成代码。
    /// </summary>
    /// <remarks>
    /// 回归：生成器写死 <c>partial class</c>，与用户的 <c>struct</c> 冲突（CS0261）；
    /// 结构体也没有 <c>DataContext</c> 属性（CS0103）。
    /// </remarks>
    [Fact]
    public void StructView_ReportsLENABP012()
    {
        var (sources, diagnostics) = RunMvvm(
            """
            public partial class PageViewModel { }

            [Len.Abp.Wpf.Mvvm.View<PageViewModel>(ViewName = "page")]
            public partial struct PageView { }
            """
        );

        Assert.Empty(sources);
        AssertSingleError(diagnostics, "LENABP012", "结构体");
    }

    /// <summary>
    /// <c>record struct</c> 视图同样报告 <c>LENABP012</c>。
    /// </summary>
    /// <remarks>
    /// <c>record struct</c> 的 <see cref="TypeKind"/> 是 <see cref="TypeKind.Struct"/>，
    /// 与普通结构体走同一分支。
    /// </remarks>
    [Fact]
    public void RecordStructView_ReportsLENABP012()
    {
        var (sources, diagnostics) = RunMvvm(
            """
            public partial class PageViewModel { }

            [Len.Abp.Wpf.Mvvm.View<PageViewModel>(ViewName = "page")]
            public partial record struct PageView { }
            """
        );

        Assert.Empty(sources);
        AssertSingleError(diagnostics, "LENABP012", "结构体");
    }

    /// <summary>
    /// <c>record</c> 视图不受影响，正常生成代码。
    /// </summary>
    /// <remarks>
    /// <c>record</c> 的 <see cref="TypeKind"/> 是 <see cref="TypeKind.Class"/>，
    /// 必须放行——record 视图模型是合法用法。
    /// </remarks>
    [Fact]
    public void RecordView_GeneratesCode()
    {
        var (sources, diagnostics) = RunMvvm(
            """
            public partial class PageViewModel { }

            [Len.Abp.Wpf.Mvvm.View<PageViewModel>(ViewName = "page")]
            public partial record PageView
            {
                public PageView() { }
            }
            """
        );

        Assert.NotEmpty(sources);
        Assert.DoesNotContain(diagnostics, d => d.Id == "LENABP012");
    }

    /// <summary>
    /// 普通类视图不受影响，正常生成代码。
    /// </summary>
    [Fact]
    public void PlainClassView_GeneratesCode()
    {
        var (sources, diagnostics) = RunMvvm(
            """
            public partial class PageViewModel { }

            [Len.Abp.Wpf.Mvvm.View<PageViewModel>(ViewName = "page")]
            public partial class PageView
            {
                public PageView() { }
            }
            """
        );

        Assert.NotEmpty(sources);
        Assert.DoesNotContain(diagnostics, d => d.Id == "LENABP012");
    }

    /// <summary>
    /// 静态类上的非附加依赖属性报告 <c>LENABP013</c>，且不生成代码。
    /// </summary>
    /// <remarks>
    /// 回归：非附加版本生成实例属性与 <c>protected</c> setter，
    /// 静态类无法承载（CS0708/CS1057）。
    /// </remarks>
    [Fact]
    public void StaticClassDependencyProperty_ReportsLENABP013()
    {
        var (sources, _, diagnostics) = GeneratorTestHost.RunAll(
            [new DependencyPropertyGenerator()],
            DependencyPropertyStubs
                + """

                  namespace Demo
                  {
                      [Len.Abp.Wpf.DependencyProperty<string>("Title")]
                      public static partial class StaticHost { }
                  }
                  """
        );

        Assert.Empty(sources);
        AssertSingleError(diagnostics, "LENABP013", "静态类");
    }

    /// <summary>
    /// 静态类上的非附加路由事件报告 <c>LENABP013</c>，且不生成代码。
    /// </summary>
    /// <remarks>
    /// 回归：非附加版本生成实例事件与 <c>protected</c> 的 <c>Raise</c> 方法，
    /// 静态类无法承载（CS0708/CS1057）。
    /// </remarks>
    [Fact]
    public void StaticClassRoutedEvent_ReportsLENABP013()
    {
        var (sources, _, diagnostics) = GeneratorTestHost.RunAll(
            [new RoutedEventGenerator()],
            RoutedEventStubs
                + """

                  namespace Demo
                  {
                      [Len.Abp.Wpf.RoutedEvent("Ping", System.Windows.RoutingStrategy.Bubble)]
                      public static partial class StaticHost { }
                  }
                  """
        );

        Assert.Empty(sources);
        AssertSingleError(diagnostics, "LENABP013", "静态类");
    }

    /// <summary>
    /// 静态类上的附加依赖属性仍正常生成，不报诊断。
    /// </summary>
    /// <remarks>
    /// 附加版本只生成静态的 <c>Get</c>/<c>Set</c> 方法与静态分部钩子，编译层面没有障碍，
    /// 因此生成器不报诊断。这条用例锁定"拦截不能误伤附加版本"。
    /// 注意：静态类宿主虽然能编译，但 <c>DependencyProperty.Register</c> 要求
    /// <c>ownerType</c> 派生自 <c>DependencyObject</c>，运行期会在类型初始化时抛异常。
    /// 该限制由文档约束，生成器不检查。
    /// </remarks>
    [Fact]
    public void StaticClassAttachedDependencyProperty_GeneratesCode()
    {
        var (sources, _, diagnostics) = GeneratorTestHost.RunAll(
            [new AttachedDependencyPropertyGenerator()],
            DependencyPropertyStubs
                + """

                  namespace Demo
                  {
                      [Len.Abp.Wpf.AttachedDependencyProperty<string>("Title")]
                      public static partial class StaticHost { }
                  }
                  """
        );

        Assert.NotEmpty(sources);
        Assert.DoesNotContain(diagnostics, d => d.Id == "LENABP013");
    }

    /// <summary>
    /// 静态类上的附加路由事件仍正常生成，不报诊断。
    /// </summary>
    [Fact]
    public void StaticClassAttachedRoutedEvent_GeneratesCode()
    {
        var (sources, _, diagnostics) = GeneratorTestHost.RunAll(
            [new AttachedRoutedEventGenerator()],
            RoutedEventStubs
                + """

                  namespace Demo
                  {
                      [Len.Abp.Wpf.AttachedRoutedEvent("Ping", System.Windows.RoutingStrategy.Bubble)]
                      public static partial class StaticHost { }
                  }
                  """
        );

        Assert.NotEmpty(sources);
        Assert.DoesNotContain(diagnostics, d => d.Id == "LENABP013");
    }

    /// <summary>
    /// 非静态类上的非附加依赖属性不受影响。
    /// </summary>
    [Fact]
    public void InstanceClassDependencyProperty_GeneratesCode()
    {
        var (sources, _, diagnostics) = GeneratorTestHost.RunAll(
            [new DependencyPropertyGenerator()],
            DependencyPropertyStubs
                + """

                  namespace Demo
                  {
                      [Len.Abp.Wpf.DependencyProperty<string>("Title")]
                      public partial class MyControl : System.Windows.DependencyObject { }
                  }
                  """
        );

        Assert.NotEmpty(sources);
        Assert.DoesNotContain(diagnostics, d => d.Id == "LENABP013");
    }

    /// <summary>
    /// 结构体上的非附加依赖属性报告 <c>LENABP013</c>。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 回归：早期版本认为"结构体可以声明实例成员，编译层面合法"而放行，
    /// 但生成器写的是 <c>partial class</c>，与 <c>struct</c> 声明冲突（CS0261），
    /// 且生成的 <c>GetValue</c>/<c>SetValue</c> 调用在结构体上不存在（CS0103）。
    /// </para>
    /// <para>
    /// 因此结构体与静态类一样必须拦截，只报告一条可操作诊断，
    /// 而不是让使用者面对一堆指向生成文件的语法错误。
    /// </para>
    /// </remarks>
    [Fact]
    public void StructDependencyProperty_ReportsLENABP013()
    {
        var (sources, _, diagnostics) = GeneratorTestHost.RunAll(
            [new DependencyPropertyGenerator()],
            DependencyPropertyStubs
                + """

                  namespace Demo
                  {
                      [Len.Abp.Wpf.DependencyProperty<string>("Title")]
                      public partial struct MyControl { }
                  }
                  """
        );

        Assert.Empty(sources);
        AssertSingleError(diagnostics, "LENABP013", "是结构体");
    }

    /// <summary>
    /// <c>record struct</c> 宿主同样报告 <c>LENABP013</c>。
    /// </summary>
    /// <remarks>
    /// <c>record struct</c> 的 <see cref="Microsoft.CodeAnalysis.TypeKind"/> 也是
    /// <see cref="Microsoft.CodeAnalysis.TypeKind.Struct"/>，与普通结构体走同一条判定。
    /// </remarks>
    [Fact]
    public void RecordStructDependencyProperty_ReportsLENABP013()
    {
        var (sources, _, diagnostics) = GeneratorTestHost.RunAll(
            [new DependencyPropertyGenerator()],
            DependencyPropertyStubs
                + """

                  namespace Demo
                  {
                      [Len.Abp.Wpf.DependencyProperty<string>("Title")]
                      public partial record struct MyControl { }
                  }
                  """
        );

        Assert.Empty(sources);
        AssertSingleError(diagnostics, "LENABP013", "是结构体");
    }

    /// <summary>
    /// 结构体上的非附加路由事件同样报告 <c>LENABP013</c>。
    /// </summary>
    [Fact]
    public void StructRoutedEvent_ReportsLENABP013()
    {
        var (sources, _, diagnostics) = GeneratorTestHost.RunAll(
            [new RoutedEventGenerator()],
            RoutedEventStubs
                + """

                  namespace Demo
                  {
                      [Len.Abp.Wpf.RoutedEvent("Clicked", System.Windows.RoutingStrategy.Bubble)]
                      public partial struct MyControl { }
                  }
                  """
        );

        Assert.Empty(sources);
        AssertSingleError(diagnostics, "LENABP013", "是结构体");
    }

    private static void AssertSingleError(IReadOnlyList<Diagnostic> diagnostics, string id, string expectedText)
    {
        var diagnostic = Assert.Single(diagnostics, d => d.Id == id);

        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.NotEqual(Location.None, diagnostic.Location);
        Assert.Contains(expectedText, diagnostic.GetMessage());
    }

    private static (IReadOnlyList<GeneratedSource> Sources, IReadOnlyList<Diagnostic> Diagnostics) RunMvvm(string snippet)
    {
        var (sources, _, diagnostics) = GeneratorTestHost.RunAll([new MvvmGenerator()], MvvmStubs + "\n" + snippet);

        return (sources, diagnostics);
    }

    /// <summary>
    /// 视图生成器所需的最小桩。
    /// </summary>
    private const string MvvmStubs = """
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
    /// 依赖属性生成器所需的最小桩。
    /// </summary>
    private const string DependencyPropertyStubs = """
        namespace System.Windows
        {
            public class DependencyObject
            {
                public object GetValue(DependencyProperty dp) => new();
                public void SetValue(DependencyProperty dp, object? value) { }
                public void SetValue(DependencyPropertyKey key, object? value) { }
            }

            public class DependencyProperty
            {
                public static DependencyProperty Register(string name, System.Type propertyType, System.Type ownerType, PropertyMetadata typeMetadata, ValidateValueCallback? validateValueCallback) => new();
                public static DependencyPropertyKey RegisterReadOnly(string name, System.Type propertyType, System.Type ownerType, PropertyMetadata typeMetadata, ValidateValueCallback? validateValueCallback) => new();
                public static DependencyProperty RegisterAttached(string name, System.Type propertyType, System.Type ownerType, PropertyMetadata defaultMetadata, ValidateValueCallback? validateValueCallback) => new();
                public static DependencyPropertyKey RegisterAttachedReadOnly(string name, System.Type propertyType, System.Type ownerType, PropertyMetadata defaultMetadata, ValidateValueCallback? validateValueCallback) => new();
            }

            public class DependencyPropertyKey { public DependencyProperty DependencyProperty => new(); }

            public delegate bool ValidateValueCallback(object value);

            public class PropertyMetadata
            {
                public PropertyMetadata(object? defaultValue, PropertyChangedCallback? propertyChangedCallback, CoerceValueCallback? coerceValueCallback) { }
            }

            public class FrameworkPropertyMetadata : PropertyMetadata
            {
                public FrameworkPropertyMetadata(object? defaultValue, FrameworkPropertyMetadataOptions flags, PropertyChangedCallback? propertyChangedCallback, CoerceValueCallback? coerceValueCallback, bool isAnimationProhibited) : base(defaultValue, propertyChangedCallback, coerceValueCallback) { }
            }

            [System.Flags]
            public enum FrameworkPropertyMetadataOptions { None = 0, AffectsRender = 1 }

            public delegate void PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e);

            public delegate object CoerceValueCallback(DependencyObject d, object baseValue);

            public struct DependencyPropertyChangedEventArgs { public object? OldValue => null; public object? NewValue => null; }
        }

        namespace System.Windows.Data
        {
            public enum UpdateSourceTrigger { Default = 0, PropertyChanged = 1, LostFocus = 2, Explicit = 3 }
        }

        namespace Len.Abp.Wpf
        {
            [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true)]
            public class DependencyPropertyAttribute : System.Attribute
            {
                public DependencyPropertyAttribute(string name, System.Type type) { }
                public string? Category { get; set; }
                public object? DefaultValue { get; set; }
                public string? DefaultValueExpression { get; set; }
                public string? Description { get; set; }
                public System.Windows.FrameworkPropertyMetadataOptions Flags { get; set; }
                public bool IsReadOnly { get; set; }
                public System.Windows.Data.UpdateSourceTrigger UpdateSourceTrigger { get; set; }
            }

            [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true)]
            public class DependencyPropertyAttribute<T> : DependencyPropertyAttribute
            {
                public DependencyPropertyAttribute(string name) : base(name, typeof(T)) { }
                public new T? DefaultValue { get; set; }
            }

            [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true)]
            public class AttachedDependencyPropertyAttribute : System.Attribute
            {
                public AttachedDependencyPropertyAttribute(string name, System.Type type) { }
                public string? Category { get; set; }
                public object? DefaultValue { get; set; }
                public string? DefaultValueExpression { get; set; }
                public string? Description { get; set; }
                public System.Windows.FrameworkPropertyMetadataOptions Flags { get; set; }
                public bool IsReadOnly { get; set; }
                public System.Windows.Data.UpdateSourceTrigger UpdateSourceTrigger { get; set; }
            }

            [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true)]
            public class AttachedDependencyPropertyAttribute<T> : AttachedDependencyPropertyAttribute
            {
                public AttachedDependencyPropertyAttribute(string name) : base(name, typeof(T)) { }
                public new T? DefaultValue { get; set; }
            }
        }
        """;

    /// <summary>
    /// 路由事件生成器所需的最小桩。
    /// </summary>
    private const string RoutedEventStubs = """
        namespace System.Windows
        {
            public enum RoutingStrategy { Tunnel = 0, Bubble = 1, Direct = 2 }

            public delegate void RoutedEventHandler(object sender, RoutedEventArgs e);

            public class RoutedEventArgs : System.EventArgs { public RoutedEvent? RoutedEvent { get; set; } }

            public class RoutedEvent { }

            public static class EventManager
            {
                public static RoutedEvent RegisterRoutedEvent(string name, RoutingStrategy strategy, System.Type handlerType, System.Type ownerType) => new();
            }

            public class UIElement
            {
                public void AddHandler(RoutedEvent routedEvent, System.Delegate handler) { }
                public void RemoveHandler(RoutedEvent routedEvent, System.Delegate handler) { }
                public void RaiseEvent(RoutedEventArgs e) { }
            }
        }

        namespace Len.Abp.Wpf
        {
            [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true)]
            public class RoutedEventAttribute : System.Attribute
            {
                public RoutedEventAttribute(string name, System.Windows.RoutingStrategy strategy, System.Type? handlerType = null) { }
                public string? Category { get; set; }
                public string? Description { get; set; }
            }

            [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true)]
            public class RoutedEventAttribute<T> : RoutedEventAttribute
            {
                public RoutedEventAttribute(string name, System.Windows.RoutingStrategy strategy) : base(name, strategy, typeof(T)) { }
            }

            [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true)]
            public class AttachedRoutedEventAttribute : RoutedEventAttribute
            {
                public AttachedRoutedEventAttribute(string name, System.Windows.RoutingStrategy strategy, System.Type? handlerType = null) : base(name, strategy, handlerType) { }
            }

            [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true)]
            public class AttachedRoutedEventAttribute<T> : RoutedEventAttribute<T>
            {
                public AttachedRoutedEventAttribute(string name, System.Windows.RoutingStrategy strategy) : base(name, strategy) { }
            }
        }
        """;
}
