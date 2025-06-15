namespace Len.Abp.Wpf.Navigation.Regions;

public abstract class BasicRegionAdapter<T> : IRegionAdapter where T : class
{
    IRegion IRegionAdapter.Initialize(object regionTarget, string regionName)
    {
        return Initialize(GetCastedObject(regionTarget), regionName);
    }

    protected abstract void Adapt(IRegion region, T regionTarget);

    protected abstract IRegion CreateRegion();

    private static T GetCastedObject(object regionTarget)
    {
        ArgumentNullException.ThrowIfNull(regionTarget, nameof(regionTarget));

        if (regionTarget is not T castedObject)
        {
            throw new InvalidOperationException($"The region target must be of type {typeof(T).FullName}.");
        }

        return castedObject;
    }

    private IRegion Initialize(T regionTarget, string regionName)
    {
        ArgumentNullException.ThrowIfNull(regionName, nameof(regionName));

        var region = CreateRegion();

        region.Name = regionName;

        Adapt(region, regionTarget);

        return region;
    }
}