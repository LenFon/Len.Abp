namespace Len.Abp.Wpf.Navigation.Regions;

/// <summary>
/// 区域查找器，按名称定位区域。
/// </summary>
public interface IRegionFinder
{
    /// <summary>
    /// 按名称查找区域。
    /// </summary>
    /// <param name="regionName">区域名称。</param>
    /// <returns>匹配的区域；不存在时返回 <see langword="null"/>。</returns>
    /// <remarks>
    /// 存在多个同名区域时优先返回当前可见的那个，即所属视图位于活动链上的实例。
    /// </remarks>
    IRegion? FindRegion(string regionName);
}
