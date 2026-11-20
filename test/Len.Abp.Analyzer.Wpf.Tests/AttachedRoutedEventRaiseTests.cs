using Len.Abp.Analyzer.TestSupport;
using Xunit;

namespace Len.Abp.Analyzer.Wpf.Tests;

/// <summary>
/// 覆盖：附加路由事件的 <c>Raise</c> 方法不能假定事件参数可无参构造。
/// </summary>
/// <remarks>
/// <para>
/// 原实现无条件生成 <c>args ??= new();</c>。WPF 的多数事件参数类型
/// （<c>MouseButtonEventArgs</c>、<c>KeyEventArgs</c>、<c>TextChangedEventArgs</c> 等）
/// 都没有公共无参构造函数——它们由 WPF 内部构造并传入。
/// 生成 <c>new()</c> 会让**所有实用场景**编译失败（CS7036）。
/// </para>
/// <para>
/// 既有测试的桩只定义了带隐式无参构造的 <c>RoutedEventArgs</c>，
/// 且只断言方法签名包含 <c>args = default</c>，从未编译过方法体。
/// </para>
/// </remarks>
public class AttachedRoutedEventRaiseTests
{
    /// <summary>
    /// 事件参数没有公共无参构造时，生成的 <c>Raise</c> 方法仍能编译。
    /// </summary>
    [Fact]
    public void EventArgsWithoutParameterlessConstructor_Compiles()
    {
        var (sources, errors, _) = GeneratorTestHost.RunAll(
            [new AttachedRoutedEventGenerator()],
            Stubs
                + """

                namespace Demo
                {
                    [Len.Abp.Wpf.AttachedRoutedEvent<System.Windows.MouseButtonEventHandler>("Pressed", System.Windows.RoutingStrategy.Bubble)]
                    public static partial class MyAttached { }
                }
                """
        );

        var generated = Assert.Single(sources);

        Assert.True(
            errors.Count == 0,
            "生成的代码无法编译：" + Environment.NewLine + string.Join(Environment.NewLine, errors)
                + Environment.NewLine + "---- 生成代码 ----" + Environment.NewLine + generated.Text
        );
    }

    /// <summary>
    /// 生成的 <c>Raise</c> 方法不再包含 <c>args ??= new()</c>。
    /// </summary>
    /// <remarks>
    /// 直接断言文本，失败信息比编译错误更直接指向根因。
    /// </remarks>
    [Fact]
    public void Raise_DoesNotConstructEventArgs()
    {
        var (sources, _, _) = GeneratorTestHost.RunAll(
            [new AttachedRoutedEventGenerator()],
            Stubs
                + """

                namespace Demo
                {
                    [Len.Abp.Wpf.AttachedRoutedEvent<System.Windows.MouseButtonEventHandler>("Pressed", System.Windows.RoutingStrategy.Bubble)]
                    public static partial class MyAttached { }
                }
                """
        );

        var generated = Assert.Single(sources);

        Assert.DoesNotContain("args ??= new()", generated.Text);
        Assert.Contains("ArgumentNullException.ThrowIfNull(args)", generated.Text);
    }

    /// <summary>
    /// 带无参构造的事件参数（<c>RoutedEventArgs</c>）同样能编译。
    /// </summary>
    /// <remarks>
    /// 对照组：修复不能只照顾"没有无参构造"的情况而破坏最常见的一种。
    /// </remarks>
    [Fact]
    public void DefaultEventArgs_Compiles()
    {
        var (sources, errors, _) = GeneratorTestHost.RunAll(
            [new AttachedRoutedEventGenerator()],
            Stubs
                + """

                namespace Demo
                {
                    [Len.Abp.Wpf.AttachedRoutedEvent("Clicked", System.Windows.RoutingStrategy.Bubble)]
                    public static partial class MyAttached { }
                }
                """
        );

        var generated = Assert.Single(sources);

        Assert.True(
            errors.Count == 0,
            "生成的代码无法编译：" + Environment.NewLine + string.Join(Environment.NewLine, errors)
                + Environment.NewLine + "---- 生成代码 ----" + Environment.NewLine + generated.Text
        );
    }

    /// <summary>
    /// 生成器所需的可编译桩。
    /// </summary>
    /// <remarks>
    /// <c>MouseButtonEventArgs</c> 刻意只提供带参构造，镜像真实 WPF 类型。
    /// </remarks>
    private const string Stubs = """
        namespace System.Windows
        {
            public class DependencyObject { }

            public class UIElement : DependencyObject
            {
                public void AddHandler(RoutedEvent routedEvent, System.Delegate handler) { }
                public void RemoveHandler(RoutedEvent routedEvent, System.Delegate handler) { }
                public void RaiseEvent(RoutedEventArgs e) { }
            }

            public class RoutedEvent { }

            public class RoutedEventArgs
            {
                public RoutedEvent? RoutedEvent { get; set; }
            }

            // 镜像真实 WPF 类型：没有公共无参构造函数。
            public class MouseButtonEventArgs : RoutedEventArgs
            {
                public MouseButtonEventArgs(int button) { }
            }

            public enum RoutingStrategy { Tunnel = 0, Bubble = 1, Direct = 2 }

            public delegate void RoutedEventHandler(object sender, RoutedEventArgs e);

            public delegate void MouseButtonEventHandler(object sender, MouseButtonEventArgs e);

            public static class EventManager
            {
                public static RoutedEvent RegisterRoutedEvent(string name, RoutingStrategy routingStrategy, System.Type handlerType, System.Type ownerType) => new();
            }
        }

        namespace Len.Abp.Wpf
        {
            [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true)]
            public class AttachedRoutedEventAttribute : System.Attribute
            {
                public AttachedRoutedEventAttribute(string name, System.Windows.RoutingStrategy strategy, System.Type? handlerType = null) { }
                public string? Category { get; set; }
                public string? Description { get; set; }
            }

            [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true)]
            public class AttachedRoutedEventAttribute<T> : AttachedRoutedEventAttribute
            {
                public AttachedRoutedEventAttribute(string name, System.Windows.RoutingStrategy strategy) : base(name, strategy, typeof(T)) { }
            }
        }
        """;
}
