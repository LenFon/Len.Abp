using Volo.Abp.DependencyInjection;

namespace Len.Abp.Wpf.Navigation.Regions.Behaviors;

[ExposeKeyedService<IRegionBehavior>(nameof(RegionProviderRegistrationBehavior))]
public class RegionProviderRegistrationBehavior(IRegionProvider regionProvider) : IRegionBehavior, ITransientDependency
{
    private readonly IRegionProvider _regionProvider = regionProvider;

    public IRegion Region { get; set; } = default!;

    public void Attach()
    {
        if (Region is not null)
        {
            _regionProvider.Add(Region);
        }
    }
}