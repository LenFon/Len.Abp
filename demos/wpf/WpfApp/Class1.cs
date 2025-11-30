using Len.Abp.Wpf;
using System.Windows;

namespace WpfApp;

public static class ClassB
{
    /// <summary>
    ///
    /// </summary>
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("RoutedEventGenerator", "1.0.0.0")]
    public static readonly global::System.Windows.RoutedEvent CCEvent = global::System.Windows.EventManager.RegisterRoutedEvent(
        "CC",
        global::System.Windows.RoutingStrategy.Bubble,
        typeof(global::System.Windows.RoutedEventHandler),
        typeof(global::WpfApp.Class1)
    );

    /// <summary>
    ///
    /// </summary>
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("RoutedEventGenerator", "1.0.0.0")]
    public static void AddCCHandler(DependencyObject d, RoutedEventHandler handler)
    {
        if (d is not UIElement element) return;

        element.AddHandler(CCEvent, handler);
    }

    public static void RaiseEvent(DependencyObject d, RoutedEventArgs? args = default)
    {
        if (d is not UIElement element) return;

        args ??= new();
        args.RoutedEvent = CCEvent;
        element.RaiseEvent(args);
    }

    /// <summary>
    ///
    /// </summary>
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]
    [global::System.CodeDom.Compiler.GeneratedCode("RoutedEventGenerator", "1.0.0.0")]
    public static void RemoveCCHandler(DependencyObject d, RoutedEventHandler handler)
    {
        if (d is not UIElement element) return;

        element.RemoveHandler(CCEvent, handler);
    }
}

[AttachedRoutedEvent<EventHandler<AAEvent>>("CC", RoutingStrategy.Bubble)]
public static partial class MyAttachedEvents
{
}

public class AAEvent : RoutedEventArgs { }

[RoutedEvent("CC", RoutingStrategy.Bubble)]
[RoutedEvent<EventHandler<AAEvent>>("Custom", RoutingStrategy.Bubble, Description = "自定义事件")]
public partial class Class1 : global::System.Windows.Controls.Control
{
}