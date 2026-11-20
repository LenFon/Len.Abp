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
internal class ContentControlRegionAdapter(IAbpLazyServiceProvider abpLazyServiceProvider)
    : BasicRegionAdapter<ContentControl>(abpLazyServiceProvider),
        ISingletonDependency
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

        void OnActiveViewsChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
            regionTarget.Content = region.ActiveViews.FirstOrDefault();

        void OnViewsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add && region.ActiveViews.Count == 0 && e.NewItems is [IView view, ..])
            {
                region.Activate(view);
            }
        }

        region.ActiveViews.CollectionChanged += OnActiveViewsChanged;
        region.Views.CollectionChanged += OnViewsChanged;

        // 区域被丢弃后必须解订阅：控件是长寿命对象，闭包持有区域，
        // 不解订阅会让旧区域继续驱动控件，且两者都无法回收。
        OnDetach(region, () => region.ActiveViews.CollectionChanged -= OnActiveViewsChanged);
        OnDetach(region, () => region.Views.CollectionChanged -= OnViewsChanged);
    }

    /// <inheritdoc/>
    protected override IRegion CreateRegion(string regionName, IView ownerView) =>
        new SingleActiveRegion(regionName, ownerView.ViewName, ownerView);

    /// <summary>
    /// 清空 <see cref="ContentControl.Content"/>，使控件可以重新适配。
    /// </summary>
    /// <param name="region">即将被丢弃的区域。</param>
    /// <param name="regionTarget">区域宿主控件。</param>
    /// <remarks>
    /// 只在 <c>Content</c> 仍指向本区域中的视图时才清空：使用者可能在区域之外
    /// 自己设置了内容，那种情况不属于适配器的写入，不能清掉。
    /// 订阅清理由基类的显式接口实现先行完成，这里的 <c>base.Detach</c> 是空实现。
    /// </remarks>
    protected override void Detach(IRegion region, ContentControl regionTarget)
    {
        base.Detach(region, regionTarget);

        if (regionTarget.Content is IView current && region.Views.Contains(current))
        {
            regionTarget.Content = null;
        }
    }
}