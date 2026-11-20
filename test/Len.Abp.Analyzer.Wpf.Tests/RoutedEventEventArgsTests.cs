using Len.Abp.Analyzer.TestSupport;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Len.Abp.Analyzer.Wpf.Tests;

/// <summary>
/// 覆盖：路由事件的处理程序第二参数必须派生自 <c>RoutedEventArgs</c>。
/// </summary>
/// <remarks>
/// <para>
/// 生成的 <c>Raise</c> 方法会写 <c>args.RoutedEvent = ...</c> 并调用
/// <c>element.RaiseEvent(args)</c>，两者都要求事件参数派生自
/// <c>System.Windows.RoutedEventArgs</c>。不校验时使用者看到的是
/// CS1061 与 CS1503 两条错误，位置指向生成文件，很难反推到特性声明。
/// </para>
/// <para>
/// WPF 的 <c>EventManager.RegisterRoutedEvent</c> 本身也要求该约束，
/// 但检查发生在运行时（注册静态字段时），生成器可以提前到编译期。
/// </para>
/// </remarks>
public class RoutedEventEventArgsTests
{
    /// <summary>
    /// 第二参数不是 <c>RoutedEventArgs</c> 派生类型时报告 <c>LENABP004</c>。
    /// </summary>
    [Fact]
    public void EventArgsNotDerivedFromRoutedEventArgs_ReportsLENABP004()
    {
        var (sources, diagnostics) = GeneratorTestHost.RunWithErrors(
            new RoutedEventGenerator(),
            Stubs
                + """

                namespace Demo
                {
                    public delegate void PlainEventHandler(object sender, System.EventArgs e);

                    [Len.Abp.Wpf.RoutedEvent("Changed", System.Windows.RoutingStrategy.Bubble, typeof(PlainEventHandler))]
                    public partial class MyControl : System.Windows.UIElement { }
                }
                """
        );

        Assert.Empty(sources);

        var diagnostic = Assert.Single(diagnostics, d => d.Id == "LENABP004");
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("RoutedEventArgs", diagnostic.GetMessage());
    }

    /// <summary>
    /// 附加版本同样校验。
    /// </summary>
    [Fact]
    public void AttachedEventArgsNotDerivedFromRoutedEventArgs_ReportsLENABP004()
    {
        var (sources, diagnostics) = GeneratorTestHost.RunWithErrors(
            new AttachedRoutedEventGenerator(),
            Stubs
                + """

                namespace Demo
                {
                    public delegate void PlainEventHandler(object sender, System.EventArgs e);

                    [Len.Abp.Wpf.AttachedRoutedEvent("Changed", System.Windows.RoutingStrategy.Bubble, typeof(PlainEventHandler))]
                    public static partial class MyAttached { }
                }
                """
        );

        Assert.Empty(sources);
        Assert.Single(diagnostics, d => d.Id == "LENABP004");
    }

    /// <summary>
    /// 派生自 <c>RoutedEventArgs</c> 的类型正常生成。
    /// </summary>
    /// <remarks>
    /// 对照组：校验不能误伤真实 WPF 事件参数类型。
    /// </remarks>
    [Fact]
    public void DerivedEventArgs_GeneratesCode()
    {
        var (sources, diagnostics) = GeneratorTestHost.RunWithErrors(
            new RoutedEventGenerator(),
            Stubs
                + """

                namespace Demo
                {
                    public delegate void MouseButtonEventHandler(object sender, System.Windows.MouseButtonEventArgs e);

                    [Len.Abp.Wpf.RoutedEvent("Pressed", System.Windows.RoutingStrategy.Bubble, typeof(MouseButtonEventHandler))]
                    public partial class MyControl : System.Windows.UIElement { }
                }
                """
        );

        Assert.Single(sources);
        Assert.DoesNotContain(diagnostics, d => d.Id == "LENABP004");
    }

    /// <summary>
    /// 默认的 <c>RoutedEventHandler</c> 正常生成。
    /// </summary>
    [Fact]
    public void DefaultHandler_GeneratesCode()
    {
        var (sources, diagnostics) = GeneratorTestHost.RunWithErrors(
            new RoutedEventGenerator(),
            Stubs
                + """

                namespace Demo
                {
                    [Len.Abp.Wpf.RoutedEvent("Clicked", System.Windows.RoutingStrategy.Bubble)]
                    public partial class MyControl : System.Windows.UIElement { }
                }
                """
        );

        Assert.Single(sources);
        Assert.DoesNotContain(diagnostics, d => d.Id == "LENABP004");
    }

    /// <summary>
    /// 生成器所需的桩。
    /// </summary>
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

            public class RoutedEventArgs { public RoutedEvent? RoutedEvent { get; set; } }

            // 派生类型：应被放行。
            public class MouseButtonEventArgs : RoutedEventArgs { }

            public enum RoutingStrategy { Tunnel = 0, Bubble = 1, Direct = 2 }

            public delegate void RoutedEventHandler(object sender, RoutedEventArgs e);

            public static class EventManager
            {
                public static RoutedEvent RegisterRoutedEvent(string name, RoutingStrategy routingStrategy, System.Type handlerType, System.Type ownerType) => new();
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
