using Volo.Abp.DependencyInjection;

namespace Len.Abp.Wpf.Navigation.Regions;

internal class RegionProvider : IRegionProvider, ISingletonDependency
{
    private readonly List<IRegion> _regions = [];

    public IReadOnlyList<IRegion> Regions => _regions;

    public void Add(IRegion region)
    {
        _regions.Add(region);
    }

    public void RemoveByViewName(string viewName)
    {
        _regions.RemoveAll(region => region.ViewName == viewName);
    }
}