using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.DependencyInjection;

namespace Len.Abp.Wpf.Navigation.Regions;

internal class RegionAdapterFinder(IAbpLazyServiceProvider abpLazyServiceProvider) : IRegionAdapterFinder
{
    private readonly IAbpLazyServiceProvider _abpLazyServiceProvider = abpLazyServiceProvider;

    public IRegionAdapter FindByControlType(Type controlType)
    {
        var currentType = controlType;

        while (currentType != null)
        {
            var regionAdapter = _abpLazyServiceProvider.GetKeyedService<IRegionAdapter>(currentType);
            if (regionAdapter is not null)
            {
                return regionAdapter;
            }

            currentType = currentType.BaseType;
        }

        throw new KeyNotFoundException($"No region adapter found for control type '{controlType.FullName}'. Ensure that the control type is registered with a region adapter.");
    }
}