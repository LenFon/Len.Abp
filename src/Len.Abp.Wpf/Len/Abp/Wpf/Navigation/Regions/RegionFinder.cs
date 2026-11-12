using Volo.Abp.DependencyInjection;

namespace Len.Abp.Wpf.Navigation.Regions;

/// <summary>
/// <see cref="IRegionFinder"/> 的默认实现，按名称从区域提供程序中查找区域。
/// </summary>
internal sealed class RegionFinder(IRegionProvider regionProvider) : IRegionFinder, ISingletonDependency
{
    private readonly IRegionProvider _regionProvider = regionProvider;

    /// <inheritdoc/>
    public IRegion? FindRegion(string regionName)
    {
        ArgumentNullException.ThrowIfNull(regionName, nameof(regionName));

        return _regionProvider.Regions.FirstOrDefault(x => x.Name == regionName);
    }
}
