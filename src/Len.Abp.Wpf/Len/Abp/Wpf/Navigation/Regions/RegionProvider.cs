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
    public void RemoveByViewName(string viewName)
    {
        _regions.RemoveAll(region => region.Owner == viewName);
    }
}
