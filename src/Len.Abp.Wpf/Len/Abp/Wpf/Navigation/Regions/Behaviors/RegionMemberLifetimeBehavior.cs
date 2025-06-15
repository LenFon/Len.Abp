using Len.Abp.Wpf.Mvvm;
using System.Collections.Specialized;
using Volo.Abp.DependencyInjection;

namespace Len.Abp.Wpf.Navigation.Regions.Behaviors;

[ExposeKeyedService<IRegionBehavior>(nameof(RegionMemberLifetimeBehavior))]
public class RegionMemberLifetimeBehavior(IRegionProvider regionProvider) : IRegionBehavior, ITransientDependency
{
    private readonly IRegionProvider _regionProvider = regionProvider;

    public IRegion Region { get; set; } = default!;

    public void Attach()
    {
        if (Region is not null)
        {
            Region.ActiveViews.CollectionChanged += ActiveViews_CollectionChanged;
        }
    }

    private static bool ShouldKeepAlive(IView inactiveView)
    {
        if (inactiveView.ViewModel is IRegionMemberLifetime lifetime)
        {
            return lifetime.KeepAlive;
        }

        return true;
    }

    private void ActiveViews_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action != NotifyCollectionChangedAction.Remove)
            return;

        var inactiveViews = e.OldItems!.Cast<IView>();
        foreach (var inactiveView in inactiveViews)
        {
            if (!ShouldKeepAlive(inactiveView))
            {
                if (Region.Views.Contains(inactiveView))
                {
                    Region.Remove(inactiveView);
                    _regionProvider.RemoveByViewName(inactiveView.ViewName);
                }
            }
        }
    }
}