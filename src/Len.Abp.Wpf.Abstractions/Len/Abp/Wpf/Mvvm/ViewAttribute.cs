using Microsoft.Extensions.DependencyInjection;

namespace Len.Abp.Wpf.Mvvm;

/// <summary>
/// 标记视图类，并为 <c>MvvmGenerator</c> 提供视图模型、视图名称等信息。
/// </summary>
/// <param name="viewModel">视图模型类型。</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class ViewAttribute(Type viewModel) : Attribute
{
    /// <summary>
    /// 获取或设置该视图是否为 Shell 主窗口。
    /// </summary>
    public bool IsShell { get; set; } = false;

    /// <summary>
    /// 获取或设置视图模型类型。
    /// </summary>
    public Type ViewModel { get; set; } = viewModel;

    /// <summary>
    /// 获取或设置视图名称，导航时用于定位视图。
    /// </summary>
    public string? ViewName { get; set; }

    /// <summary>
    /// 获取或设置视图在容器中的注册生命周期。
    /// </summary>
    /// <remarks>
    /// 默认值 <see cref="ServiceLifetime.Transient"/>；Shell 视图固定为单例，该设置会被忽略。
    /// </remarks>
    public ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Transient;
}

/// <summary>
/// 标记视图类，并指定视图模型类型。
/// </summary>
/// <typeparam name="T">视图模型类型。</typeparam>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class ViewAttribute<T>() : ViewAttribute(typeof(T));
