using Volo.Abp.DependencyInjection;

namespace Len.Abp.Wpf.Navigation.Regions;

/// <summary>
/// <see cref="IRegionFinder"/> 的默认实现，按名称从区域提供程序中查找区域。
/// </summary>
/// <remarks>
/// 嵌套区域允许重名（同一视图的多个实例各自拥有同名子区域），因此同名候选多于一个时，
/// 优先返回当前可见的那个：其所属视图位于活动链上，用户看到的就是它。
/// </remarks>
internal sealed class RegionFinder(IRegionProvider regionProvider) : IRegionFinder, ISingletonDependency
{
    private readonly IRegionProvider _regionProvider = regionProvider;

    /// <inheritdoc/>
    public IRegion? FindRegion(string regionName)
    {
        ArgumentNullException.ThrowIfNull(regionName);

        var candidates = _regionProvider.Regions.Where(region => region.Name == regionName).ToArray();

        if (candidates.Length <= 1)
        {
            return candidates.FirstOrDefault();
        }

        return candidates.FirstOrDefault(IsVisible) ?? candidates[0];
    }

    private bool IsVisible(IRegion region) => IsVisible(region, []);

    /// <summary>
    /// 判断区域是否位于当前可见的视图链上。
    /// </summary>
    /// <param name="region">待判断的区域。</param>
    /// <param name="path">当前递归路径，用于阻断环形引用。</param>
    /// <returns>区域可见时返回 true。</returns>
    /// <remarks>
    /// 区域由视图创建，只有该视图在父区域中处于活动状态，区域才可能被用户看到。
    /// 所属视图不在任何区域中时视为根视图（如 Shell），其区域始终可见。
    /// </remarks>
    private bool IsVisible(IRegion region, HashSet<IRegion> path)
    {
        if (!path.Add(region))
        {
            return false;
        }

        try
        {
            if (region.OwnerView is not { } ownerView)
            {
                return true;
            }

            var parents = _regionProvider.Regions.Where(parent => parent.Views.Contains(ownerView)).ToArray();

            if (parents.Length == 0)
            {
                return true;
            }

            return parents.Any(parent => parent.ActiveViews.Contains(ownerView) && IsVisible(parent, path));
        }
        finally
        {
            path.Remove(region);
        }
    }
}
