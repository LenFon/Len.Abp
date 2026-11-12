namespace Len.Abp.Wpf.Mvvm;

/// <summary>
/// 可被区域承载的视图，提供视图模型与视图名称。
/// </summary>
public interface IView
{
    /// <summary>
    /// 获取视图模型。
    /// </summary>
    object ViewModel { get; }

    /// <summary>
    /// 获取视图名称，用于导航与键控服务解析。
    /// </summary>
    string ViewName { get; }
}

/// <summary>
/// 强类型视图，视图模型以具体类型暴露。
/// </summary>
/// <typeparam name="TViewModel">视图模型类型。</typeparam>
public interface IView<out TViewModel> : IView
    where TViewModel : class
{
    /// <summary>
    /// 获取强类型视图模型。
    /// </summary>
    new TViewModel ViewModel { get; }
}
