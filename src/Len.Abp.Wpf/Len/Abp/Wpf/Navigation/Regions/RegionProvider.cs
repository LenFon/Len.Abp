using Len.Abp.Wpf.Mvvm;
using Volo.Abp.DependencyInjection;

namespace Len.Abp.Wpf.Navigation.Regions;

/// <summary>
/// <see cref="IRegionProvider"/> 的默认实现，以单例形式保存所有已创建的区域。
/// </summary>
internal sealed class RegionProvider : IRegionProvider, ISingletonDependency
{
    private readonly List<IRegion> _regions = [];

    /// <inheritdoc/>
    public IReadOnlyList<IRegion> Regions => _regions;

    /// <inheritdoc/>
    public void Add(IRegion region)
    {
        _regions.Add(region);
    }

    /// <inheritdoc/>
    public bool Remove(IRegion region)
    {
        ArgumentNullException.ThrowIfNull(region);

        return _regions.Remove(region);
    }

    /// <inheritdoc/>
    public IReadOnlyList<IView> RemoveOwnedRegions(IView ownerView)
    {
        ArgumentNullException.ThrowIfNull(ownerView);

        var owned = _regions.Where(region => ReferenceEquals(region.OwnerView, ownerView)).ToArray();

        return owned.Length == 0 ? [] : DetachAndCollectViews(owned);
    }

    /// <inheritdoc/>
    public IReadOnlyList<IView> Clear()
    {
        if (_regions.Count == 0)
        {
            return [];
        }

        return DetachAndCollectViews([.. _regions]);
    }

    /// <summary>
    /// 从提供程序中摘除指定区域，并返回它们承载的全部视图。
    /// </summary>
    /// <param name="regions">要摘除的区域。</param>
    /// <returns>被摘除区域中的全部视图。</returns>
    /// <remarks>
    /// 先整体摘除再收集视图：区域本身即将不可达，无需逐个 <see cref="IRegion.Remove"/>，
    /// 也就不会触发内层区域的保活行为，避免拆除过程中重入。
    /// </remarks>
    private IReadOnlyList<IView> DetachAndCollectViews(IReadOnlyList<IRegion> regions)
    {
        foreach (var region in regions)
        {
            _regions.Remove(region);
        }

        return [.. regions.SelectMany(region => region.Views)];
    }
}
