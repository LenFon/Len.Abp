using Len.Abp.Analyzer.TestSupport;
using Xunit;

namespace Len.Abp.Analyzer.Wpf.Tests;

/// <summary>
/// 锁定路由事件生成器的生成结果。
/// 在重构生成器期间，这些断言必须逐字保持不变。
/// </summary>
public class RoutedEventGeneratorTests
{
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

    /// <summary>
    /// 路由事件生成注册字段、事件访问器与触发方法。
    /// </summary>
    [Fact]
    public void RoutedEvent_GeneratesRegisterEventAndRaiseMethod()
    {
        var sources = GeneratorTestHost.Run(
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

        var generated = Assert.Single(sources);

        Assert.Contains("global::System.Windows.EventManager.RegisterRoutedEvent(", generated.Text);
        Assert.Contains("public static readonly global::System.Windows.RoutedEvent ClickedEvent", generated.Text);
        Assert.Contains("public event global::System.Windows.RoutedEventHandler Clicked", generated.Text);
        Assert.Contains("protected void RaiseClickedEvent(global::System.Windows.RoutedEventArgs args)", generated.Text);
    }

    /// <summary>
    /// 附加路由事件生成 Add/Remove 处理器与静态触发方法。
    /// </summary>
    [Fact]
    public void AttachedRoutedEvent_GeneratesAddRemoveHandlerAndStaticRaise()
    {
        var sources = GeneratorTestHost.Run(
            new AttachedRoutedEventGenerator(),
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

        Assert.Contains("public static void AddClickedHandler(global::System.Windows.DependencyObject d, global::System.Windows.RoutedEventHandler handler)", generated.Text);
        Assert.Contains("public static void RemoveClickedHandler(global::System.Windows.DependencyObject d, global::System.Windows.RoutedEventHandler handler)", generated.Text);
        Assert.Contains("public static void RaiseClickedEvent(global::System.Windows.DependencyObject d, global::System.Windows.RoutedEventArgs? args = default)", generated.Text);
    }
}
