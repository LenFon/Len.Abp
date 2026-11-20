using Len.Abp.Wpf.Mvvm;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using Volo.Abp.DependencyInjection;

namespace Len.Abp.Wpf.Navigation.Regions.Behaviors;

/// <summary>
/// 区域行为：视图失活时决定是否把视图留在区域中复用，不保留的视图会被彻底移除并释放。
/// </summary>
/// <remarks>
/// <para>
/// 保留规则按优先级：
/// <list type="number">
/// <item>视图模型声明 <see cref="IRegionMemberLifetime.KeepAlive"/> 为 true 时，视图与视图模型都保留。</item>
/// <item>视图或视图模型注册为单例时，生命周期归容器所有，保留实例供下次导航复用。</item>
/// <item>其余情况（含未实现 <see cref="IRegionMemberLifetime"/>、作用域视图）不保留，移除并释放。</item>
/// </list>
/// 默认不保留，避免忘记声明契约导致视图在区域里无限累积。
/// 不保留的视图会连同其内部任意层级的嵌套区域一起拆除；容器管理的嵌套视图保留自己的区域，
/// 否则下次复用该实例时区域已经不存在，导航会失败。
/// </para>
/// <para>
/// <b>保活只对区域直接承载的视图生效。</b>父视图被拆除时，它拥有的区域会先被整体摘除
/// （<see cref="IRegionProvider.RemoveOwnedRegions"/>），其中所有视图——包括声明了
/// <see cref="IRegionMemberLifetime.KeepAlive"/> 的——都随子树一起释放。
/// 原因是承载它们的区域已不可达：跳过释放只会留下既无法导航到、也不会被回收的孤儿实例。
/// 需要跨导航保留嵌套视图时，把保活声明放在父视图上（父视图保活则整棵子树都不拆除）。
/// </para>
/// <para>
/// <b>作用域视图不自动保活。</b>导航为每个新解析的视图创建独立作用域，
/// <c>Scoped</c> 视图每次导航得到新实例，因此不需要像单例那样留在区域中复用。
/// 拆除时优先释放视图关联的作用域，由作用域统一释放它创建的视图与视图模型，避免重复释放。
/// </para>
/// </remarks>
/// <param name="regionProvider">区域提供程序。</param>
/// <param name="viewScopeRegistry">视图与导航作用域的关联表。</param>
[ExposeKeyedService<IRegionBehavior>(nameof(RegionMemberLifetimeBehavior))]
public class RegionMemberLifetimeBehavior(
    IRegionProvider regionProvider,
    IViewScopeRegistry viewScopeRegistry
) : IRegionBehavior, IRegionTeardownBehavior, ITransientDependency
{
    private readonly IRegionProvider _regionProvider = regionProvider;
    private readonly IViewScopeRegistry _viewScopeRegistry = viewScopeRegistry;

    /// <summary>
    /// 已释放的实例，用于防止重复释放。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 用 <see cref="ConditionalWeakTable{TKey, TValue}"/> 而不是集合：弱引用保证记录本身
    /// 不会阻止实例被回收，表的大小由存活实例数决定。
    /// </para>
    /// <para>
    /// <b>必须是静态的</b>：本行为是瞬态注册、每个区域一个实例，而同一视图可能被
    /// 不同区域的行为拆除。典型路径是 <see cref="RegionTeardownService"/> 按快照遍历区域：
    /// 父区域先拆除子树并把嵌套区域从提供程序摘除、释放其中的视图，
    /// 随后快照里的嵌套区域仍会被单独 <c>RemoveAll</c>，其行为会再次看到同一批视图。
    /// 去重放在实例字段上对这条路径无效。
    /// </para>
    /// </remarks>
    private static readonly ConditionalWeakTable<object, object> DisposedInstances = [];

    /// <summary>
    /// 延迟拆除期间登记待拆除的视图；为 <see langword="null"/> 表示未处于延迟状态。
    /// </summary>
    /// <remarks>
    /// 用 <see cref="List{T}"/> 而不是单个字段：一次导航可能让多个视图失活
    /// （多活动视图区域），它们都要等导航结束后统一处理。
    /// </remarks>
    private List<IView>? _deferredViews;

    /// <inheritdoc/>
    public IRegion Region { get; set; } = default!;

    /// <inheritdoc/>
    public void Attach() => Region.ActiveViews.CollectionChanged += ActiveViews_CollectionChanged;

    /// <inheritdoc/>
    public void TearDownAll(bool force = false)
    {
        // 先取快照：TearDown 会修改区域内容，直接遍历集合会抛异常。
        foreach (var view in Region.Views.ToArray())
        {
            TearDown(view, force);
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// 只摘除不释放：保活视图仍可能被复用，它的嵌套区域会在视图重新加载时重建。
    /// 对已被 <see cref="TearDown"/> 移除的视图不会命中（它们已不在区域中），
    /// 因此不存在重复摘除。
    /// </remarks>
    public void DetachOwnedRegions()
    {
        // 先取快照：RemoveOwnedRegions 会修改提供程序，直接遍历区域集合是安全的，
        // 但保持与 TearDownAll 一致的写法，避免将来改动引入遍历期修改。
        foreach (var view in Region.Views.ToArray())
        {
            _regionProvider.RemoveOwnedRegions(view);
        }
    }

    /// <inheritdoc/>
    public void BeginDeferral() => _deferredViews ??= [];

    /// <inheritdoc/>
    public void CommitDeferral()
    {
        var deferred = _deferredViews;

        _deferredViews = null;

        if (deferred is null)
        {
            return;
        }

        foreach (var view in deferred)
        {
            TearDown(view);
        }
    }

    /// <inheritdoc/>
    public void CancelDeferral() => _deferredViews = null;

    /// <summary>
    /// 判断视图是否应保留在区域中。
    /// </summary>
    /// <remarks>
    /// 只有单例必须保留：它由容器持有，移除区域不会释放它，反而会丢掉视图内部的嵌套区域，
    /// 下次导航还得重建整棵视图子树。
    /// 作用域视图不保留——它由导航作用域创建，每次导航都会得到新实例，
    /// 保留旧实例只会让作用域无法释放。
    /// </remarks>
    private static bool ShouldKeepAlive(IView view) =>
        IsKeepAliveRequested(view) || IsSingleton(view) || IsSingleton(view.ViewModel);

    /// <summary>
    /// 判断视图模型是否显式要求保活。
    /// </summary>
    private static bool IsKeepAliveRequested(IView view) =>
        view.ViewModel is IRegionMemberLifetime lifetime && lifetime.KeepAlive;

    /// <summary>
    /// 判断实例是否为容器单例。
    /// </summary>
    private static bool IsSingleton(object? instance) => instance is ISingletonDependency;

    /// <summary>
    /// 释放不再保留的视图及其视图模型。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 视图由导航作用域创建时，作用域已经跟踪它创建的 <see cref="IDisposable"/> 实例，
    /// 释放作用域即可，不能再逐个释放，否则会重复调用 <c>Dispose</c>。
    /// 没有关联作用域时才走逐实例释放，且只释放非单例实例——
    /// 单例归容器所有，提前释放会让下次导航拿到已释放的对象。
    /// </para>
    /// <para>
    /// <b>每个实例最多释放一次。</b>去重是防御性的：正常路径下视图只会被拆除一次，
    /// 但 <see cref="RegionTeardownService"/> 按快照遍历区域时，同一视图可能先后
    /// 被父区域的行为与嵌套区域自己的行为拆除。第二次 <c>Dispose</c> 会让真实视图模型
    /// （持有 <see cref="CancellationTokenSource"/> 等）抛 <see cref="ObjectDisposedException"/>，
    /// 而拆除发生在导航或退出过程中，异常会让整次操作被判定为失败。
    /// </para>
    /// </remarks>
    private void DisposeIfNeeded(IView view)
    {
        if (_viewScopeRegistry.TryTake(view, out var scope))
        {
            // 作用域会释放它创建的视图与视图模型，但不会经过下面的逐实例去重。
            // 两者都登记一次，避免同一实例随后又被逐实例路径释放。
            MarkDisposed(view);
            MarkDisposed(view.ViewModel);

            scope!.Dispose();
            return;
        }

        if (!IsSingleton(view) && MarkDisposed(view))
        {
            (view as IDisposable)?.Dispose();
        }

        if (!IsSingleton(view.ViewModel) && MarkDisposed(view.ViewModel))
        {
            (view.ViewModel as IDisposable)?.Dispose();
        }
    }

    /// <summary>
    /// 登记实例为已释放。
    /// </summary>
    /// <param name="instance">要登记的实例。</param>
    /// <returns>首次登记时返回 <see langword="true"/>；已登记过时返回 <see langword="false"/>。</returns>
    /// <remarks>
    /// 视图模型可能为 <see langword="null"/>（手写 <see cref="IView"/> 实现），
    /// 此时无需登记，直接返回 <see langword="false"/> 让调用方跳过释放。
    /// </remarks>
    private static bool MarkDisposed(object? instance)
    {
        if (instance is null)
        {
            return false;
        }

        return DisposedInstances.TryAdd(instance, instance);
    }

    private void ActiveViews_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action != NotifyCollectionChangedAction.Remove || e.OldItems is not { Count: > 0 } oldItems)
        {
            return;
        }

        foreach (var inactiveView in oldItems.Cast<IView>())
        {
            // 延迟期间只登记，等导航结果确定后再决定拆除还是丢弃。
            if (_deferredViews is { } deferred)
            {
                deferred.Add(inactiveView);
                continue;
            }

            TearDown(inactiveView);
        }
    }

    /// <summary>
    /// 按保活规则处理一个视图：不保留的移出区域并释放整棵子树。
    /// </summary>
    /// <param name="view">要拆除的视图。</param>
    /// <param name="force">为 <see langword="true"/> 时无视保活声明，直接移除并释放。</param>
    /// <remarks>
    /// <para>
    /// <c>!Region.Views.Contains(view)</c> 是重入保护：<see cref="TearDownAll"/> 与
    /// <see cref="Region.Remove"/> 都会触发集合变更通知，此时视图已不在区域中，
    /// 必须短路，否则会重复释放。
    /// </para>
    /// <para>
    /// <paramref name="force"/> 用于调用方显式移除：视图离开区域后不会再被导航复用，
    /// 保留其作用域只是泄漏。自动拆除（失活、清空）不传，保活声明照常生效。
    /// </para>
    /// </remarks>
    public void TearDown(IView view, bool force = false)
    {
        if ((!force && ShouldKeepAlive(view)) || !Region.Views.Contains(view))
        {
            return;
        }

        // 先移出区域，断开区域对视图的引用；再拆除该视图拥有的整棵区域子树，
        // 最后释放可释放的实例，使整棵视图子树不再被框架持有。
        // 必须走 RemoveCore：Region.Remove 会再次查找本行为，造成递归。
        if (Region is Region concrete)
        {
            concrete.RemoveCore(view);
        }
        else
        {
            // 自定义 IRegion 实现不查拆除行为，直接移除不会递归。
            Region.Remove(view);
        }

        TearDownSubtree(view);
    }

    /// <summary>
    /// 拆除视图拥有的区域子树，并释放其中不再复用的视图。
    /// </summary>
    /// <param name="view">已从区域中移除的视图。</param>
    /// <remarks>
    /// <para>
    /// 递归而不是只处理一层，任意深度的嵌套都能被完整拆除。
    /// 单例视图会被复用，必须保留它的区域，否则下次导航找不到嵌套区域。
    /// </para>
    /// <para>
    /// <b>保活声明在这里不生效。</b><see cref="IRegionProvider.RemoveOwnedRegions"/> 已经把
    /// 父视图拥有的区域整体摘除，承载嵌套视图的区域从此不可达——跳过释放只会留下
    /// 既无法导航到、也不会被回收的孤儿实例。需要跨导航保留嵌套视图时，
    /// 应把保活声明放在父视图上，那样整棵子树都不会进入本方法。
    /// </para>
    /// <para>
    /// <b>释放只发生一次</b>：递归调用自身会在末尾释放 <paramref name="view"/>，
    /// 调用方不能再补一次。重复释放会让实现了 <see cref="IDisposable"/> 的视图模型
    /// 在第二次 <c>Dispose</c> 时抛 <see cref="ObjectDisposedException"/>，
    /// 而拆除发生在导航过程中，异常会让整次导航被判定为失败。
    /// </para>
    /// </remarks>
    private void TearDownSubtree(IView view)
    {
        foreach (var nestedView in _regionProvider.RemoveOwnedRegions(view))
        {
            if (IsSingleton(nestedView) || IsSingleton(nestedView.ViewModel))
            {
                continue;
            }

            // 递归内部已释放 nestedView，这里不能再释放一次。
            TearDownSubtree(nestedView);
        }

        DisposeIfNeeded(view);
    }
}
