using Len.Abp.Wpf.Extensions;
using System.Collections.Specialized;
using System.Windows.Controls;

namespace Len.Abp.Wpf.Navigation.Regions;

internal class ContentControlRegionAdapter : BasicRegionAdapter<ContentControl>
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
            if (e.Action == NotifyCollectionChangedAction.Add && region.ActiveViews.Count() == 0)
            {
                region.Activate(e.NewItems[0]);
            }
        };
    }

    protected override IRegion CreateRegion() => new SingleActiveRegion();
}