namespace Len.Abp.Wpf.Navigation;

/// <summary>
/// 导航生命周期回调，由视图模型实现以参与导航流程。
/// </summary>
public interface INavigationAware
{
    /// <summary>
    /// 判断当前视图是否接受本次导航。
    /// </summary>
    /// <param name="navigationContext">导航上下文。</param>
    /// <returns>接受本次导航时返回 <see langword="true"/>；返回 <see langword="false"/> 时导航服务会解析新实例。</returns>
    Task<bool> IsNavigationTargetAsync(INavigationContext navigationContext);

    /// <summary>
    /// 在视图失活之前调用。
    /// </summary>
    /// <param name="navigationContext">导航上下文。</param>
    /// <returns>表示异步操作的任务。</returns>
    Task OnNavigatedFromAsync(INavigationContext navigationContext);

    /// <summary>
    /// 在视图激活之后调用。
    /// </summary>
    /// <param name="navigationContext">导航上下文。</param>
    /// <returns>表示异步操作的任务。</returns>
    Task OnNavigatedToAsync(INavigationContext navigationContext);
}
