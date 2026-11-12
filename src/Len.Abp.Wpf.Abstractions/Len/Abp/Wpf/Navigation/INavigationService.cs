namespace Len.Abp.Wpf.Navigation;

/// <summary>
/// 导航服务，负责解析目标视图、切换区域活动视图并通知导航生命周期。
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// 导航到指定名称的视图。
    /// </summary>
    /// <param name="regionName">目标区域名称。</param>
    /// <param name="viewName">目标视图名称。</param>
    /// <param name="navigationParameters">导航参数。</param>
    /// <returns>导航结果；区域不存在或视图解析失败时返回失败结果而不抛异常。</returns>
    ValueTask<INavigationResult> NavigateAsync(string regionName, string viewName, IParameters navigationParameters);
}
