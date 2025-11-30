using System.Windows;

namespace Len.Abp.Wpf;

/// <summary>
/// Attribute to generate a attached routed event.
/// </summary>
/// <param name="name">The name of the routed event. The name must be unique within the owner type and cannot be null or an empty string.</param>
/// <param name="strategy">The routing strategy of the routed event.</param>
/// <param name="handlerType">The type of the event handler. This must be a delegate type.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class AttachedRoutedEventAttribute(string name, RoutingStrategy strategy, Type? handlerType = null)
    : RoutedEventAttribute(name, strategy, handlerType);

/// <summary>
/// Attribute to generate a attached routed event.
/// </summary>
/// <typeparam name="T">The type of the event handler. This must be a delegate type.</typeparam>
/// <param name="name">The name of the routed event. The name must be unique within the owner type and cannot be null or an empty string.</param>
/// <param name="strategy">The routing strategy of the routed event.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class AttachedRoutedEventAttribute<T>(string name, RoutingStrategy strategy) : RoutedEventAttribute<T>(name, strategy);