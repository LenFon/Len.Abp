using Len.Abp.Wpf;
using System.Windows;

namespace WpfApp;

[RoutedEvent("CC", RoutingStrategy.Bubble)]
[RoutedEvent<EventHandler<AAEvent>>("Custom", RoutingStrategy.Bubble, Description = "自定义事件")]
public partial class Class1 : global::System.Windows.Controls.Control
{
}

public class AAEvent : RoutedEventArgs { }