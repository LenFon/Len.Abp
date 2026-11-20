using Len.Abp.Wpf.Mvvm;
using Len.Abp.Wpf.Navigation.Regions;
using System.Collections.Specialized;
using System.Windows.Controls;
using System.Windows.Data;
using Volo.Abp.DependencyInjection;

namespace WpfApp.Regions;

/// <summary>
/// 自定义区域适配器：把 <see cref="ListBox"/> 适配为区域，每个视图一个列表项。
/// </summary>
/// <remarks>
/// <para>
/// 内置适配器只覆盖 <c>ContentControl</c>、<c>TabControl</c> 与 <c>ItemsControl</c>。
/// 需要别的宿主控件时，继承 <see cref="BasicRegionAdapter{T}"/> 并实现
/// <see cref="BasicRegionAdapter{T}.Adapt"/> 与 <see cref="BasicRegionAdapter{T}.CreateRegion"/> 即可。
/// </para>
/// <para>
/// 用键控方式注册：<c>RegionAdapterFinder</c> 按控件类型沿继承链查找，
/// 因此 <see cref="ListBox"/> 会命中本适配器，而 <see cref="ItemsControl"/> 仍走内置的那个。
/// </para>
/// <para>
/// 与 <c>ItemsControl</c> 的区别在于本适配器是**单活动视图**：
/// 列表项显示全部视图，但只有选中项处于激活状态，切换选中会激活对应视图。
/// </para>
/// </remarks>
[ExposeKeyedService<IRegionAdapter>(typeof(ListBox))]
public class ListBoxRegionAdapter(IAbpLazyServiceProvider abpLazyServiceProvider)
    : BasicRegionAdapter<ListBox>(abpLazyServiceProvider),
        ISingletonDependency
{
    /// <inheritdoc/>
    protected override void Adapt(IRegion region, ListBox regionTarget)
    {
        ArgumentNullException.ThrowIfNull(regionTarget, nameof(regionTarget));

        if (regionTarget.ItemsSource is not null
            || BindingOperations.GetBindingBase(regionTarget, ItemsControl.ItemsSourceProperty) is not null)
        {
            throw new ArgumentException("ListBox 的 ItemsSource 已被占用。", nameof(regionTarget));
        }

        regionTarget.ItemsSource = region.Views;

        // 激活状态与选中项双向同步：程序化设置 SelectedItem 会再次触发 SelectionChanged，
        // 用标志位短路，避免激活与选中互相触发形成递归。
        var isSyncing = false;

        region.ActiveViews.CollectionChanged += (_, e) =>
        {
            if (e.Action != NotifyCollectionChangedAction.Add || e.NewItems is not [IView view, ..])
            {
                return;
            }

            if (ReferenceEquals(regionTarget.SelectedItem, view))
            {
                return;
            }

            isSyncing = true;

            try
            {
                regionTarget.SelectedItem = view;
            }
            finally
            {
                isSyncing = false;
            }
        };

        regionTarget.SelectionChanged += (_, _) =>
        {
            if (isSyncing || regionTarget.SelectedItem is not IView view || region.ActiveViews.Contains(view))
            {
                return;
            }

            region.Activate(view);
        };
    }

    /// <inheritdoc/>
    protected override IRegion CreateRegion(string regionName, IView ownerView) =>
        new SingleActiveRegion(regionName, ownerView.ViewName, ownerView);
}
