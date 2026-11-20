using Len.Abp.Wpf.Mvvm;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace Len.Abp.Wpf.Navigation.Regions;

/// <summary>
/// <see cref="IRegion"/> 的默认实现，维护区域内的视图集合及其激活状态。
/// </summary>
public class Region : IRegion
{
    private readonly ViewsCollection _activeViews;
    private readonly ObservableCollection<RegionMetadata> _metadata = [];
    private readonly ViewsCollection _views;

    /// <summary>
    /// 初始化区域。
    /// </summary>
    /// <param name="name">区域名称。</param>
    /// <param name="owner">区域所属的视图名称。</param>
    /// <param name="ownerView">创建该区域的视图实例。</param>
    public Region(string name, string owner, IView? ownerView = null)
    {
        Behaviors = new RegionBehaviorCollection(this);
        _views = new(_metadata, x => true);
        _activeViews = new(_metadata, x => x.IsActive);
        Name = name;
        Owner = owner;
        OwnerView = ownerView;
    }

    /// <inheritdoc/>
    public IViewsCollection ActiveViews => _activeViews;

    /// <inheritdoc/>
    public IRegionBehaviorCollection Behaviors { get; }

    /// <inheritdoc/>
    public string Name { get; }

    /// <inheritdoc/>
    public string Owner { get; }

    /// <inheritdoc/>
    public IView? OwnerView { get; }

    /// <inheritdoc/>
    public IViewsCollection Views => _views;

    /// <inheritdoc/>
    public virtual void Activate(IView view)
    {
        FindMetadata(view).IsActive = true;
    }

    /// <inheritdoc/>
    public void Add(IView view)
    {
        if (_metadata.Any(x => x.Item == view))
        {
            throw new ArgumentException("该视图已存在于区域中。");
        }

        _metadata.Add(new RegionMetadata(view));
    }

    /// <inheritdoc/>
    public virtual void Deactivate(IView view)
    {
        FindMetadata(view).IsActive = false;
    }

    /// <inheritdoc/>
    public IView? GetView(string viewName) => _metadata.FirstOrDefault(x => x.Item.ViewName == viewName)?.Item;

    /// <inheritdoc/>
    /// <remarks>
    /// 走拆除行为的统一路径：不保活的视图连同其嵌套子树一起释放，保活的视图只移出区域。
    /// 不能只改元数据——视图可能从未激活过（例如导航校验失败后回收的新建视图），
    /// 此时它不在 <see cref="ActiveViews"/> 中，失活通知不会触发，
    /// 作用域与可释放资源就永远没人回收。
    /// </remarks>
    public void Remove(IView view)
    {
        // 先验证存在性：拆除行为内部会再查一次，但这里要保证"移除不存在的视图"仍然抛异常。
        FindMetadata(view);

        if (Behaviors.Find<IRegionTeardownBehavior>() is { } teardown)
        {
            // 显式移除必须无视保活声明：视图离开区域后不会再被导航复用，
            // 保留其作用域只是泄漏。
            teardown.TearDown(view, force: true);
            return;
        }

        // 行为未附加时退化为直接移除，与 RemoveAll 的退化路径一致。
        RemoveCore(view);
    }

    /// <summary>
    /// 只从元数据中移除视图，不经过任何拆除逻辑。
    /// </summary>
    /// <param name="view">要移除的视图。</param>
    /// <remarks>
    /// 供拆除行为在释放视图后收尾，也作为行为缺失时的退化路径。
    /// 直接调用会跳过保活判断与资源释放，使用者应使用 <see cref="Remove"/>。
    /// </remarks>
    internal void RemoveCore(IView view) => _metadata.Remove(FindMetadata(view));

    /// <inheritdoc/>
    public void RemoveAll() => RemoveAll(force: false);

    /// <summary>
    /// 清空区域中的全部视图。
    /// </summary>
    /// <param name="force">
    /// 为 <see langword="true"/> 时无视保活声明，释放区域中的全部视图。
    /// </param>
    /// <remarks>
    /// <para>
    /// 拆除逻辑（保活判断、子树递归、释放）在行为里，区域只负责转交。
    /// </para>
    /// <para>
    /// <b><paramref name="force"/> 用于区域被永久丢弃。</b>区域改名重建时，
    /// 旧区域连同其承载的视图一起失去归属：保活视图不会再被任何区域复用，
    /// 跳过释放只会让它们既无法导航到、也不会被回收。普通清空（例如宿主退出时
    /// 由 <see cref="RegionTeardownService"/> 遍历）保留保活语义，避免误伤仍可复用的实例。
    /// </para>
    /// </remarks>
    internal void RemoveAll(bool force)
    {
        var teardown = Behaviors.Find<IRegionTeardownBehavior>();

        teardown?.TearDownAll(force);

        // 保活的视图不会被 TearDown 移除，但清空语义要求区域最终为空。
        // 它们的嵌套区域必须在这里摘除：清空元数据后视图与区域的所有权关系断开，
        // 区域会留在提供程序里成为孤儿，导航可能命中用户看不到的实例。
        teardown?.DetachOwnedRegions();

        RemoveAllCore();
    }

    /// <summary>
    /// 直接清空元数据，不经过任何拆除逻辑。
    /// </summary>
    /// <remarks>
    /// 供拆除行为在逐项处理完视图后收尾，也作为行为缺失时的退化路径。
    /// </remarks>
    internal void RemoveAllCore() => _metadata.Clear();

    /// <summary>
    /// 开始延迟拆除：期间失活的视图只登记，不立即拆除。
    /// </summary>
    /// <remarks>
    /// 导航用它实现"失败即回滚"：激活新视图会让旧视图失活，
    /// 若立即拆除，回滚就没有可恢复的实例。
    /// </remarks>
    internal void BeginDeferral() => Behaviors.Find<IRegionTeardownBehavior>()?.BeginDeferral();

    /// <summary>
    /// 提交延迟拆除：导航成功后真正拆除期间失活的视图。
    /// </summary>
    internal void CommitDeferral() => Behaviors.Find<IRegionTeardownBehavior>()?.CommitDeferral();

    /// <summary>
    /// 取消延迟拆除：导航失败时丢弃登记，视图保持原样。
    /// </summary>
    internal void CancelDeferral() => Behaviors.Find<IRegionTeardownBehavior>()?.CancelDeferral();

    /// <summary>
    /// 查找指定视图的元数据。
    /// </summary>
    /// <param name="view">目标视图。</param>
    /// <returns>该视图的元数据。</returns>
    /// <exception cref="ArgumentException">该视图不在区域中时抛出。</exception>
    [return: NotNull]
    private protected RegionMetadata FindMetadata(IView view) =>
        _metadata.FirstOrDefault(x => x.Item == view)
        ?? throw new ArgumentException("该区域不包含指定的视图。");
}