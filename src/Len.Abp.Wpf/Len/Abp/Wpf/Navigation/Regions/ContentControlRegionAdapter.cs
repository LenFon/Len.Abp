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

        var contentIsSet = regionTarget.Content != null;
        contentIsSet = contentIsSet || regionTarget.HasBinding(ContentControl.ContentProperty);

        if (contentIsSet)
        {
            throw new InvalidOperationException("ContentControl's Content property is not empty.");
        }

        region.ActiveViews.CollectionChanged += (s, e) =>
        {
            regionTarget.Content = region.ActiveViews.FirstOrDefault();
        };

        region.Views.CollectionChanged += (s, e) =>
        {
            if (e.Action == NotifyCollectionChangedAction.Add && !region.ActiveViews.Any())
            {
                region.Activate((IView)e.NewItems![0]!);
            }
        };
    }

    protected override IRegion CreateRegion(string viewName) => new SingleActiveRegion(viewName);
}