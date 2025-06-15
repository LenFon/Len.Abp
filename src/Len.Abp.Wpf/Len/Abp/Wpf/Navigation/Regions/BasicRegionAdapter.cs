using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace Len.Abp.Wpf.Navigation.Regions;

public abstract class BasicRegionAdapter<T> : IRegionAdapter where T : class
{
    public IAbpLazyServiceProvider AbpLazyServiceProvider { get; set; } = default!;

    protected RegionOptions Options => AbpLazyServiceProvider.LazyGetRequiredService<IOptions<RegionOptions>>().Value;

    protected IServiceProvider ServiceProvider => AbpLazyServiceProvider.LazyGetRequiredService<IServiceProvider>();

    IRegion IRegionAdapter.Initialize(object regionTarget, string regionName, string viewName)
    {
        return Initialize(GetCastedObject(regionTarget), regionName, viewName);
    }

    protected abstract void Adapt(IRegion region, T regionTarget);

    protected virtual void AttachBehaviors(IRegion region, T regionTarget)
    {
    }

    protected virtual void AttachDefaultBehaviors(IRegion region, T regionTarget)
    {
        ArgumentNullException.ThrowIfNull(region, nameof(region));
        ArgumentNullException.ThrowIfNull(regionTarget, nameof(regionTarget));

        foreach (var behaviorKey in Options.BehaviorKeys)
        {
            if (!region.Behaviors.ContainsKey(behaviorKey))
            {
                var behavior = ServiceProvider.GetRequiredKeyedService<IRegionBehavior>(behaviorKey);

                region.Behaviors.Add(behaviorKey, behavior);
            }
        }
    }

    protected abstract IRegion CreateRegion(string viewName);

    private static T GetCastedObject(object regionTarget)
    {
        ArgumentNullException.ThrowIfNull(regionTarget, nameof(regionTarget));

        if (regionTarget is not T castedObject)
        {
            throw new InvalidOperationException($"The region target must be of type {typeof(T).FullName}.");
        }

        return castedObject;
    }

    private IRegion Initialize(T regionTarget, string regionName, string viewName)
    {
        ArgumentNullException.ThrowIfNull(regionName, nameof(regionName));

        var region = CreateRegion(viewName);

        region.Name = regionName;

        Adapt(region, regionTarget);
        AttachBehaviors(region, regionTarget);
        AttachDefaultBehaviors(region, regionTarget);

        return region;
    }
}