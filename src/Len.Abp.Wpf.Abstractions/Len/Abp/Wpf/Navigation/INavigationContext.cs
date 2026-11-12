namespace Len.Abp.Wpf.Navigation;

/// <summary>
/// 单次导航的上下文，包含目标 URI 与导航参数。
/// </summary>
public interface INavigationContext
{
    /// <summary>
    /// 获取导航参数。
    /// </summary>
    IParameters Parameters { get; }

    /// <summary>
    /// 获取导航目标 URI。
    /// </summary>
    Uri Uri { get; }
}
