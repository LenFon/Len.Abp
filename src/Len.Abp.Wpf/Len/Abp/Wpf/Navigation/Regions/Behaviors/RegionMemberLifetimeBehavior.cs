using Len.Abp.Wpf.Mvvm;
using System.Collections.Specialized;
using Volo.Abp.DependencyInjection;

namespace Len.Abp.Wpf.Navigation.Regions.Behaviors;

/// <summary>
/// 区域行为：视图失活时决定是否把视图留在区域中复用，不保留的视图会被彻底移除并释放。
/// </summary>
/// <remarks>
/// 保留规则按优先级：
/// <list type="number">
/// <item>视图模型声明 <see cref="IRegionMemberLifetime.KeepAlive"/> 为 true 时，视图与视图模型都保留。</item>
/// <item>视图或视图模型注册为单例 / 作用域时，生命周期归容器所有，保留实例供下次导航复用。</item>
/// <item>其余情况（含未实现 <see cref="IRegionMemberLifetime"/>）不保留，移除并释放瞬态实例。</item>
/// </list>
/// 默认不保留，避免忘记声明契约导致视图在区域里无限累积。
/// 不保留的视图会连同其内部任意层级的嵌套区域一起拆除；容器管理的嵌套视图保留自己的区域，
/// 否则下次复用该实例时区域已经不存在，导航会失败。
/// </remarks>
/// <param name="regionProvider">区域提供程序。</param>
[ExposeKeyedService<IRegionBehavior>(nameof(RegionMemberLifetimeBehavior))]
public class RegionMemberLifetimeBehavior(IRegionProvider regionProvider) : IRegionBehavior, ITransientDependency
{
    private readonly IRegionProvider _regionProvider = regionProvider;

    /// <inheritdoc/>
    public IRegion Region { get; set; } = default!;

    /// <inheritdoc/>
    public void Attach() => Region.ActiveViews.CollectionChanged += ActiveViews_CollectionChanged;

    /// <summary>
    /// 判断视图是否应保留在区域中。
    /// </summary>
    /// <remarks>
    /// 容器管理的实例必须保留：它们由 IoC 容器持有，移除区域不会释放它们，
    /// 反而会丢掉视图内部的嵌套区域，下次导航还得重建整棵视图子树。
    /// </remarks>
    private static bool ShouldKeepAlive(IView view) =>
        IsKeepAliveRequested(view) || IsContainerManaged(view) || IsContainerManaged(view.ViewModel);

    /// <summary>
    /// 判断视图模型是否显式要求保活。
    /// </summary>
    private static bool IsKeepAliveRequested(IView view) =>
        view.ViewModel is IRegionMemberLifetime lifetime && lifetime.KeepAlive;

    /// <summary>
    /// 判断实例的生命周期是否由容器管理（单例或作用域）。
    /// </summary>
    private static bool IsContainerManaged(object? instance) =>
        instance is ISingletonDependency or IScopedDependency;

    /// <summary>
    /// 释放不再保留的视图及其视图模型。
    /// </summary>
    /// <remarks>
    /// 只有瞬态实例归本行为释放；单例与作用域实例由容器管理，提前释放会让下次导航拿到已释放的对象。
    /// </remarks>
    private static void DisposeIfNeeded(IView view)
    {
        if (!IsContainerManaged(view))
        {
            (view as IDisposable)?.Dispose();
        }

        if (!IsContainerManaged(view.ViewModel))
        {
            (view.ViewModel as IDisposable)?.Dispose();
        }
    }

    private void ActiveViews_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action != NotifyCollectionChangedAction.Remove || e.OldItems is not { Count: > 0 } oldItems)
        {
            return;
        }

        foreach (var inactiveView in oldItems.Cast<IView>())
        {
            if (ShouldKeepAlive(inactiveView) || !Region.Views.Contains(inactiveView))
            {
                continue;
            }

            // 先移出区域，断开区域对视图的引用；再拆除该视图拥有的整棵区域子树，
            // 最后释放可释放的实例，使整棵视图子树不再被框架持有。
            Region.Remove(inactiveView);
            TearDownSubtree(inactiveView);
        }
    }

    /// <summary>
    /// 拆除视图拥有的区域子树，并释放其中不再复用的视图。
    /// </summary>
    /// <param name="view">已从区域中移除的视图。</param>
    /// <remarks>
    /// 递归而不是只处理一层，任意深度的嵌套都能被完整拆除。
    /// 容器管理的视图会被复用，必须保留它的区域，否则下次导航找不到嵌套区域。
    /// </remarks>
    private void TearDownSubtree(IView view)
    {
        foreach (var nestedView in _regionProvider.RemoveOwnedRegions(view))
        {
            if (IsContainerManaged(nestedView) || IsContainerManaged(nestedView.ViewModel))
            {
                continue;
            }

            TearDownSubtree(nestedView);
            DisposeIfNeeded(nestedView);
        }

        DisposeIfNeeded(view);
    }
}
