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
    public IReadOnlyList<IView> RemoveOwnedRegions(IView ownerView)
    {
        ArgumentNullException.ThrowIfNull(ownerView);

        var owned = _regions.Where(region => ReferenceEquals(region.OwnerView, ownerView)).ToArray();

        if (owned.Length == 0)
        {
            return [];
        }

        // 先整体摘除再收集视图：区域本身即将不可达，无需逐个 Remove，
        // 也就不会触发内层区域的保活行为，避免拆除过程中重入。
        foreach (var region in owned)
        {
            _regions.Remove(region);
        }

        return [.. owned.SelectMany(region => region.Views)];
    }
}
