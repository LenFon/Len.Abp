using Len.Abp.Wpf.Extensions;
using Len.Abp.Wpf.Mvvm;
using System.Collections.Specialized;
using System.Windows.Controls;
using Volo.Abp.DependencyInjection;

namespace Len.Abp.Wpf.Navigation.Regions;

/// <summary>
/// 将 <see cref="ContentControl"/> 适配为区域，使其内容随区域的活动视图切换。
/// </summary>
[ExposeKeyedService<IRegionAdapter>(typeof(ContentControl))]
internal class ContentControlRegionAdapter : BasicRegionAdapter<ContentControl>, ISingletonDependency
{
    /// <inheritdoc/>
    protected override void Adapt(IRegion region, ContentControl regionTarget)
    {
        ArgumentNullException.ThrowIfNull(regionTarget, nameof(regionTarget));

        var contentIsSet = regionTarget.Content != null || regionTarget.HasBinding(ContentControl.ContentProperty);

        if (contentIsSet)
        {
            throw new ArgumentException("ContentControl 的 Content 属性不为空。", nameof(regionTarget));
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

    /// <inheritdoc/>
    protected override IRegion CreateRegion(string regionName, IView ownerView) =>
        new SingleActiveRegion(regionName, ownerView.ViewName, ownerView);
}