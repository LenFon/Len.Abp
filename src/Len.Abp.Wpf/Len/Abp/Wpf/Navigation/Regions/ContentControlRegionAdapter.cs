using Len.Abp.Wpf.Extensions;
using Len.Abp.Wpf.Mvvm;
using System.Collections.Specialized;
using System.Windows.Controls;
using Volo.Abp.DependencyInjection;

namespace Len.Abp.Wpf.Navigation.Regions;

[ExposeKeyedService<IRegionAdapter>(typeof(ContentControl))]
internal class ContentControlRegionAdapter : BasicRegionAdapter<ContentControl>, ISingletonDependency
{
    protected override void Adapt(IRegion region, ContentControl regionTarget)
    {
        ArgumentNullException.ThrowIfNull(regionTarget, nameof(regionTarget));

        var contentIsSet = regionTarget.Content != null || regionTarget.HasBinding(ContentControl.ContentProperty);

        if (contentIsSet)
        {
            throw new ArgumentException("ContentControl's Content property is not empty.", nameof(regionTarget));
        }

        region.ActiveViews.CollectionChanged += (s, e) =>
        {
            regionTarget.Content = region.ActiveViews.FirstOrDefault();
        };

        region.Views.CollectionChanged += (s, e) =>
        {
            if (e.Action == NotifyCollectionChangedAction.Add && region.ActiveViews.Count == 0 && e.NewItems is [IView view, ..])
            {
                region.Activate(view);
            }
        };
    }

    protected override IRegion CreateRegion(string regionName, string viewName) => new SingleActiveRegion(regionName, viewName);
}