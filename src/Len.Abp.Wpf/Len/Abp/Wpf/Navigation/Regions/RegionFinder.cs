using Volo.Abp.DependencyInjection;

namespace Len.Abp.Wpf.Navigation.Regions;

internal class RegionFinder(IRegionProvider regionProvider) : IRegionFinder, ISingletonDependency
{
    private readonly IRegionProvider _regionProvider = regionProvider;

    public IRegion? FindRegion(string regionName)
    {
        ArgumentNullException.ThrowIfNull(regionName, nameof(regionName));

        return _regionProvider.Regions.FirstOrDefault(x => x.Name == regionName);
    }
}