using System.Windows;

namespace Len.Abp.Wpf;

/// <summary>
/// Attribute to generate a routed event.
/// </summary>
/// <param name="name">The name of the routed event. The name must be unique within the owner type and cannot be null or an empty string.</param>
/// <param name="strategy">The routing strategy of the routed event.</param>
/// <param name="handlerType">The type of the event handler. This must be a delegate type.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class RoutedEventAttribute(string name, RoutingStrategy strategy, Type? handlerType = null) : Attribute
{
    /// <summary>
    /// Gets or sets the category of the routed event.
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    ///  Gets or sets the description of the routed event.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The name of the routed event.
    /// </summary>
    public string Name { get; } = name ?? throw new ArgumentNullException(nameof(name));

    /// <summary>
    /// The routing strategy of the routed event.
    /// </summary>
    public RoutingStrategy Strategy { get; } = strategy;

    /// <summary>
    /// The type of the event handler. This must be a delegate type.
    /// <br/>Default: typeof(RoutedEventHandler)
    /// </summary>
    public Type? Type { get; set; } = handlerType ?? typeof(RoutedEventHandler);
}

/// <summary>
/// Attribute to generate a routed event.
/// </summary>
/// <typeparam name="T">The type of the event handler. This must be a delegate type.</typeparam>
/// <param name="name">The name of the routed event. The name must be unique within the owner type and cannot be null or an empty string.</param>
/// <param name="strategy">The routing strategy of the routed event.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class RoutedEventAttribute<T>(string name, RoutingStrategy strategy) : RoutedEventAttribute(name, strategy, typeof(T));