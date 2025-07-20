using System.Collections.Specialized;
using Len.Abp.Wpf.Mvvm;
using Volo.Abp.DependencyInjection;

namespace Len.Abp.Wpf.Navigation.Regions.Behaviors;

[ExposeKeyedService<IRegionBehavior>(nameof(RegionMemberLifetimeBehavior))]
public class RegionMemberLifetimeBehavior(IRegionProvider regionProvider) : IRegionBehavior, ITransientDependency
{
    private readonly IRegionProvider _regionProvider = regionProvider;

    public IRegion Region { get; set; } = default!;

    public void Attach() => Region.ActiveViews.CollectionChanged += ActiveViews_CollectionChanged;

    private static bool ShouldKeepAlive(IView view) => view.ViewModel is not IRegionMemberLifetime lifetime || lifetime.KeepAlive;

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