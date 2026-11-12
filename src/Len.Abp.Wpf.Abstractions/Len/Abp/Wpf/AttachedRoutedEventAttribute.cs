using System.Windows;

namespace Len.Abp.Wpf;

/// <summary>
/// 用于生成附加路由事件的特性。
/// </summary>
/// <param name="name">路由事件的名称。名称在所有者类型内必须唯一，且不能为 null 或空字符串。</param>
/// <param name="strategy">路由事件的路由策略。</param>
/// <param name="handlerType">事件处理程序的类型，必须是委托类型。</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class AttachedRoutedEventAttribute(string name, RoutingStrategy strategy, Type? handlerType = null)
    : RoutedEventAttribute(name, strategy, handlerType);

/// <summary>
/// 用于生成附加路由事件的特性。
/// </summary>
/// <typeparam name="T">事件处理程序的类型，必须是委托类型。</typeparam>
/// <param name="name">路由事件的名称。名称在所有者类型内必须唯一，且不能为 null 或空字符串。</param>
/// <param name="strategy">路由事件的路由策略。</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class AttachedRoutedEventAttribute<T>(string name, RoutingStrategy strategy) : RoutedEventAttribute<T>(name, strategy);
