namespace Len.Abp.Wpf.Navigation.Regions;

internal class RegionAdapterFinder : IRegionAdapterFinder
{
    private readonly Dictionary<Type, IRegionAdapter> _mappings = [];

    public IRegionAdapter FindByControlType(Type controlType)
    {
        var currentType = controlType;

        while (currentType != null)
        {
            if (_mappings.TryGetValue(currentType, out var value))
            {
                return value;
            }

            currentType = currentType.BaseType;
        }

        throw new KeyNotFoundException($"No region adapter found for control type '{controlType.FullName}'. Ensure that the control type is registered with a region adapter.");
    }
}