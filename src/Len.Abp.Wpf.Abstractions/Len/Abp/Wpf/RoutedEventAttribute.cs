using System.Windows;

namespace Len.Abp.Wpf;

/// <summary>
/// 用于生成路由事件的特性。
/// </summary>
/// <param name="name">路由事件的名称。名称在所有者类型内必须唯一，且不能为 null 或空字符串。</param>
/// <param name="strategy">路由事件的路由策略。</param>
/// <param name="handlerType">事件处理程序的类型，必须是委托类型。</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class RoutedEventAttribute(string name, RoutingStrategy strategy, Type? handlerType = null) : Attribute
{
    /// <summary>
    /// 获取或设置路由事件的分类。
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// 获取或设置路由事件的描述。
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 路由事件的名称。
    /// </summary>
    public string Name { get; } = name ?? throw new ArgumentNullException(nameof(name));

    /// <summary>
    /// 路由事件的路由策略。
    /// </summary>
    public RoutingStrategy Strategy { get; } = strategy;

    /// <summary>
    /// 事件处理程序的类型，必须是委托类型。
    /// <br/>默认值：typeof(RoutedEventHandler)
    /// </summary>
    public Type? Type { get; set; } = handlerType ?? typeof(RoutedEventHandler);
}

/// <summary>
/// 用于生成路由事件的特性。
/// </summary>
/// <typeparam name="T">事件处理程序的类型，必须是委托类型。</typeparam>
/// <param name="name">路由事件的名称。名称在所有者类型内必须唯一，且不能为 null 或空字符串。</param>
/// <param name="strategy">路由事件的路由策略。</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class RoutedEventAttribute<T>(string name, RoutingStrategy strategy) : RoutedEventAttribute(name, strategy, typeof(T));
