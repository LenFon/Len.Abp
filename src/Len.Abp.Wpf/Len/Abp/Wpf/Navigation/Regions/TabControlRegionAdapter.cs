using Len.Abp.Wpf.Extensions;
using Len.Abp.Wpf.Mvvm;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Volo.Abp.DependencyInjection;

namespace Len.Abp.Wpf.Navigation.Regions;

/// <summary>
/// 将 <see cref="TabControl"/> 适配为区域，每个视图对应一个选项卡。
/// </summary>
/// <remarks>
/// <para>
/// 区域为单活动视图：<see cref="TabControl"/> 同一时刻只显示一个内容，
/// 与 <see cref="ContentControl"/> 的语义一致。切换选项卡会激活对应视图，
/// 旧视图按既有的保活规则决定保留还是拆除——不保活的视图在切走后会连同选项卡一起消失。
/// </para>
/// <para>
/// 视图被包装成 <see cref="TabItem"/> 而不是直接把 <see cref="IRegion.Views"/> 交给
/// <c>ItemsSource</c>：<see cref="TabControl"/> 默认把项本身当作 <c>Header</c>，
/// 视图是 <see cref="UIElement"/>，会被当作标题内容渲染并从原位置移走。
/// 包装后可以显式设置 <c>Header</c>，也不必覆盖使用者的 <c>ItemContainerStyle</c>。
/// </para>
/// </remarks>
[ExposeKeyedService<IRegionAdapter>(typeof(TabControl))]
internal class TabControlRegionAdapter(IAbpLazyServiceProvider abpLazyServiceProvider)
    : BasicRegionAdapter<TabControl>(abpLazyServiceProvider),
        ISingletonDependency
{
    /// <inheritdoc/>
    protected override void Adapt(IRegion region, TabControl regionTarget)
    {
        ArgumentNullException.ThrowIfNull(regionTarget, nameof(regionTarget));

        if (regionTarget.ItemsSource is not null || regionTarget.HasBinding(ItemsControl.ItemsSourceProperty))
        {
            throw new ArgumentException("TabControl 的 ItemsSource 已被占用。", nameof(regionTarget));
        }

        var tabs = new ObservableCollection<TabItem>();
        var sync = new TabSelectionSync(regionTarget);

        regionTarget.ItemsSource = tabs;

        void OnViewsChanged(object? sender, NotifyCollectionChangedEventArgs e) => SyncTabs(tabs, region, e);
        void OnActiveViewsChanged(object? sender, NotifyCollectionChangedEventArgs e) => sync.SelectTabFor(tabs, e);
        void OnSelectionChanged(object? sender, SelectionChangedEventArgs e) => sync.ActivateSelectedView(region);

        region.Views.CollectionChanged += OnViewsChanged;
        region.ActiveViews.CollectionChanged += OnActiveViewsChanged;
        regionTarget.SelectionChanged += OnSelectionChanged;

        // 区域被丢弃后必须解订阅：控件是长寿命对象，闭包持有区域与选项卡集合，
        // 不解订阅会让旧区域继续驱动控件（切换选项卡时对已清空的区域调 Activate 会抛异常），
        // 且旧区域与其中的视图都无法回收。
        OnDetach(region, () => region.Views.CollectionChanged -= OnViewsChanged);
        OnDetach(region, () => region.ActiveViews.CollectionChanged -= OnActiveViewsChanged);
        OnDetach(region, () => regionTarget.SelectionChanged -= OnSelectionChanged);

        RebuildTabs(tabs, region);
    }

    /// <summary>
    /// 让选项卡集合跟随区域视图集合变化。
    /// </summary>
    /// <param name="tabs">选项卡集合。</param>
    /// <param name="region">区域。</param>
    /// <param name="e">集合变更事件参数。</param>
    private static void SyncTabs(ObservableCollection<TabItem> tabs, IRegion region, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add when e.NewItems is [IView view, ..]:
                tabs.Insert(Math.Clamp(e.NewStartingIndex, 0, tabs.Count), CreateTab(view));
                break;

            case NotifyCollectionChangedAction.Remove when e.OldItems is [IView view, ..]:
                RemoveTab(tabs, view);
                break;

            default:
                RebuildTabs(tabs, region);
                break;
        }
    }

    /// <inheritdoc/>
    protected override IRegion CreateRegion(string regionName, IView ownerView) =>
        new SingleActiveRegion(regionName, ownerView.ViewName, ownerView);

    /// <summary>
    /// 解除 <see cref="ItemsControl.ItemsSource"/> 对选项卡集合的引用。
    /// </summary>
    /// <param name="region">即将被丢弃的区域。</param>
    /// <param name="regionTarget">区域宿主控件。</param>
    /// <remarks>
    /// 选项卡集合是适配器自己创建的 <see cref="ObservableCollection{T}"/>，
    /// 无法像 <see cref="ItemsControlRegionAdapter"/> 那样按引用比对
    /// <see cref="IRegion.Views"/>。这里改为判断"当前项是否全部是本区域的选项卡"：
    /// 使用者若在适配之后替换了数据源，项就不是 <see cref="TabItem"/>，不会被误清。
    /// 订阅清理由基类的显式接口实现先行完成，这里的 <c>base.Detach</c> 是空实现。
    /// </remarks>
    protected override void Detach(IRegion region, TabControl regionTarget)
    {
        base.Detach(region, regionTarget);

        if (regionTarget.ItemsSource is ObservableCollection<TabItem> tabs && IsOwnedBy(tabs, region))
        {
            regionTarget.ItemsSource = null;
        }
    }

    /// <summary>
    /// 判断选项卡集合是否由本区域生成。
    /// </summary>
    /// <param name="tabs">选项卡集合。</param>
    /// <param name="region">区域。</param>
    /// <returns>集合中的每个选项卡都承载本区域的视图时返回 <see langword="true"/>。</returns>
    private static bool IsOwnedBy(ObservableCollection<TabItem> tabs, IRegion region) =>
        tabs.All(tab => tab.Content is IView view && region.Views.Contains(view));

    /// <summary>
    /// 为视图创建选项卡。
    /// </summary>
    /// <param name="view">视图。</param>
    /// <returns>承载该视图的选项卡。</returns>
    /// <remarks>
    /// 标题优先取视图上的 <c>RegionHost.TabHeader</c> 附加属性，未设置时回退到
    /// <see cref="IView.ViewName"/>。视图名是导航标识（如 <c>lifetime-scoped</c>），
    /// 直接展示通常不够友好。
    /// </remarks>
    private static TabItem CreateTab(IView view)
    {
        var header = view is DependencyObject element
            ? RegionHost.GetTabHeader(element) ?? view.ViewName
            : view.ViewName;

        return new TabItem { Header = header, Content = view };
    }

    private static void RemoveTab(ObservableCollection<TabItem> tabs, IView view)
    {
        var tab = tabs.FirstOrDefault(candidate => ReferenceEquals(candidate.Content, view));

        if (tab is not null)
        {
            tabs.Remove(tab);
        }
    }

    private static void RebuildTabs(ObservableCollection<TabItem> tabs, IRegion region)
    {
        tabs.Clear();

        foreach (var view in region.Views)
        {
            tabs.Add(CreateTab(view));
        }
    }

    /// <summary>
    /// 区域激活状态与 <see cref="Selector.SelectedItem"/> 的双向同步。
    /// </summary>
    /// <param name="regionTarget">区域宿主控件。</param>
    /// <remarks>
    /// 程序化设置 <c>SelectedItem</c> 会再次触发 <c>SelectionChanged</c>，
    /// 必须短路，否则激活与选中会互相触发形成递归。
    /// </remarks>
    private sealed class TabSelectionSync(TabControl regionTarget)
    {
        private bool _isSyncing;

        /// <summary>
        /// 区域激活视图变化时，把对应选项卡设为选中。
        /// </summary>
        /// <param name="tabs">选项卡集合。</param>
        /// <param name="e">集合变更事件参数。</param>
        public void SelectTabFor(ObservableCollection<TabItem> tabs, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action != NotifyCollectionChangedAction.Add || e.NewItems is not [IView view, ..])
            {
                return;
            }

            var tab = tabs.FirstOrDefault(candidate => ReferenceEquals(candidate.Content, view));

            if (tab is null || ReferenceEquals(regionTarget.SelectedItem, tab))
            {
                return;
            }

            _isSyncing = true;

            try
            {
                regionTarget.SelectedItem = tab;
            }
            finally
            {
                _isSyncing = false;
            }
        }

        /// <summary>
        /// 用户切换选项卡时激活对应视图。
        /// </summary>
        /// <param name="region">区域。</param>
        public void ActivateSelectedView(IRegion region)
        {
            if (_isSyncing || regionTarget.SelectedItem is not TabItem { Content: IView view })
            {
                return;
            }

            if (!region.ActiveViews.Contains(view))
            {
                region.Activate(view);
            }
        }
    }
}
