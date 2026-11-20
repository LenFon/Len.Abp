using Len.Abp.Wpf.Extensions;
using Len.Abp.Wpf.Mvvm;
using System.Windows.Controls;
using Volo.Abp.DependencyInjection;

namespace Len.Abp.Wpf.Navigation.Regions;

/// <summary>
/// 将 <see cref="ItemsControl"/> 适配为区域，使其项集合随区域中的视图变化。
/// </summary>
/// <remarks>
/// <para>
/// 区域为多活动视图：<see cref="ItemsControl"/> 天然能显示多个项，不需要单活动语义。
/// 区域中所有视图都会显示，与激活状态无关；激活只影响生命周期（保活与拆除）。
/// </para>
/// <para>
/// 多活动区域不会被导航自动失活，因此导航过的视图会一直留在区域中。
/// 需要回收时由使用者显式调用 <see cref="IRegion.Remove"/>：
/// 它会移除视图并释放其导航作用域。
/// </para>
/// <para>
/// <b>不要用 <see cref="IRegion.Deactivate"/> 回收。</b>它的职责是失活，
/// 对从未激活的视图是空操作（视图本来就不在 <c>ActiveViews</c> 里，
/// 不会触发拆除）；对已激活的视图，它按保活声明决定是否拆除，
/// 声明了保活的视图会留在区域中。要确保回收请用 <see cref="IRegion.Remove"/>。
/// </para>
/// <para>
/// <see cref="TabControl"/> 继承自 <see cref="ItemsControl"/>，由
/// <see cref="TabControlRegionAdapter"/> 单独处理；<see cref="RegionAdapterFinder"/>
/// 从具体类型沿继承链向上查找，因此 <see cref="TabControl"/> 会先命中自己的适配器。
/// </para>
/// </remarks>
[ExposeKeyedService<IRegionAdapter>(typeof(ItemsControl))]
internal class ItemsControlRegionAdapter(IAbpLazyServiceProvider abpLazyServiceProvider)
    : BasicRegionAdapter<ItemsControl>(abpLazyServiceProvider),
        ISingletonDependency
{
    /// <inheritdoc/>
    protected override void Adapt(IRegion region, ItemsControl regionTarget)
    {
        ArgumentNullException.ThrowIfNull(regionTarget, nameof(regionTarget));

        // ItemsSource 已被赋值或绑定说明使用者另有数据源，覆盖会静默丢弃它。
        if (regionTarget.ItemsSource is not null || regionTarget.HasBinding(ItemsControl.ItemsSourceProperty))
        {
            throw new ArgumentException("ItemsControl 的 ItemsSource 已被占用。", nameof(regionTarget));
        }

        regionTarget.ItemsSource = region.Views;
    }

    /// <inheritdoc/>
    protected override IRegion CreateRegion(string regionName, IView ownerView) =>
        new Region(regionName, ownerView.ViewName, ownerView);

    /// <summary>
    /// 解除 <see cref="ItemsControl.ItemsSource"/> 对本区域视图集合的引用。
    /// </summary>
    /// <param name="region">即将被丢弃的区域。</param>
    /// <param name="regionTarget">区域宿主控件。</param>
    /// <remarks>
    /// 只在 <c>ItemsSource</c> 仍指向本区域的 <see cref="IRegion.Views"/> 时才清空：
    /// 使用者可能在适配之后自行替换了数据源，那种情况不属于适配器的写入。
    /// 不清空会让控件无法重新适配——<see cref="Adapt"/> 会判定数据源已被占用。
    /// 订阅清理由基类的显式接口实现先行完成，这里的 <c>base.Detach</c> 是空实现。
    /// </remarks>
    protected override void Detach(IRegion region, ItemsControl regionTarget)
    {
        base.Detach(region, regionTarget);

        if (ReferenceEquals(regionTarget.ItemsSource, region.Views))
        {
            regionTarget.ItemsSource = null;
        }
    }
}
