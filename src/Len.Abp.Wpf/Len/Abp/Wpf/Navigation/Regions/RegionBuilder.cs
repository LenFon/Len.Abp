using Len.Abp.Wpf.Extensions;
using Len.Abp.Wpf.Mvvm;
using System.Runtime.CompilerServices;
using System.Windows;

namespace Len.Abp.Wpf.Navigation.Regions;

/// <summary>
/// <see cref="IRegionBuilder"/> 的默认实现，在控件加载后为其创建区域。
/// </summary>
/// <remarks>
/// <para>
/// 每次设置 <c>Region.Name</c> 都会从容器解析一个新的瞬态实例并调用 <see cref="Build"/>，
/// 因此"同一元素只订阅一次"不能靠实例状态保证，必须按元素记录。
/// 这里用 <see cref="ConditionalWeakTable{TKey, TValue}"/> 记录订阅状态：
/// 弱引用不会阻止元素被回收，也不会让已卸载的视图长期留在表里。
/// </para>
/// <para>
/// 订阅本身不随卸载解除：元素被移到新窗口时会再次触发 <c>Loaded</c>，
/// 若此时已解除订阅，区域就再也建不起来。改为在卸载时清掉"本轮已创建"标记，
/// 下次 <c>Loaded</c> 重新创建区域。
/// </para>
/// <para>
/// 卸载时还必须把本轮创建的区域从 <see cref="IRegionProvider"/> 摘除：
/// 区域不摘除会永久残留，提供程序里出现同名区域，导航可能命中早已失效的实例。
/// 但<b>只摘除不重建</b>——区域里的视图仍被元素持有（<c>Content</c> / <c>ItemsSource</c>
/// 仍指向它们），重新加载时复用同一个区域并登记回去。
/// 若改为重建，适配器会看到控件状态仍被旧视图占用而抛异常。
/// </para>
/// </remarks>
/// <param name="regionAdapterFinder">区域适配器查找器。</param>
/// <param name="regionProvider">区域提供程序，用于卸载时摘除、重新加载时登记区域。</param>
internal sealed class RegionBuilder(IRegionAdapterFinder regionAdapterFinder, IRegionProvider regionProvider) : IRegionBuilder
{
    /// <summary>
    /// 已订阅的元素及其本轮加载周期的创建状态。
    /// </summary>
    private static readonly ConditionalWeakTable<FrameworkElement, ElementState> HandledElements = [];

    private readonly IRegionAdapterFinder _regionAdapterFinder = regionAdapterFinder;
    private readonly IRegionProvider _regionProvider = regionProvider;

    /// <inheritdoc/>
    public void Build(DependencyObject element)
    {
        ArgumentNullException.ThrowIfNull(element);

        // 幂等保护：同一元素重复设置名称时，只保留第一次的订阅。
        // 否则 Loaded 触发时会有多个 builder 各创建一个区域，提供程序中出现重复条目。
        if (element is not FrameworkElement frameworkElement || !HandledElements.TryAdd(frameworkElement, new ElementState()))
        {
            return;
        }

        frameworkElement.Loaded += OnElementLoaded;
        frameworkElement.Unloaded += OnElementUnloaded;
    }

    private static IView GetOwnerView(DependencyObject targetElement) =>
        (targetElement as IView) ?? targetElement.GetParent<IView>()
        ?? throw new ArgumentException("无法确定目标元素的视图名称。请确认该元素位于某个视图内。");

    private IRegion CreateRegion(DependencyObject targetElement, string regionName)
    {
        try
        {
            var regionAdapter = _regionAdapterFinder.FindByControlType(targetElement.GetType());
            var ownerView = GetOwnerView(targetElement);

            // 创建出的区域通过 RegionProviderRegistrationBehavior 自行完成注册。
            return regionAdapter.Initialize(targetElement, regionName, ownerView);
        }
        catch (Exception ex)
        {
            throw new RegionCreationException(regionName, ex);
        }
    }

    /// <summary>
    /// 元素加载时创建或恢复区域。
    /// </summary>
    /// <param name="sender">事件源。</param>
    /// <param name="e">事件参数。</param>
    /// <remarks>
    /// 声明为 <c>internal</c> 而不是私有：测试需要直接调用它验证类型守卫与幂等行为，
    /// 而真实触发 <c>Loaded</c> 需要完整的视觉树与消息泵。用 <c>InternalsVisibleTo</c>
    /// 暴露比反射调用可靠——方法改名时编译期就会失败。
    /// </remarks>
    internal void OnElementLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement element)
        {
            return;
        }

        if (!element.CheckAccess())
        {
            return;
        }

        if (!HandledElements.TryGetValue(element, out var state))
        {
            return;
        }

        // 同一加载周期只创建一次：Loaded 可能因元素在树内移动而重复触发。
        if (!state.TryMarkCreated())
        {
            return;
        }

        // 卸载时摘除的区域在这里登记回去。不能重建：适配器在初始化时把
        // Content / ItemsSource 单向指向区域中的视图，卸载不会清除这些引用，
        // 重建时适配器会判定"控件状态已被占用"而抛异常。
        var regionName = Navigation.RegionHost.GetName(element);

        if (state.TakeDetachedRegion() is { } detached)
        {
            if (string.Equals(detached.Name, regionName, StringComparison.Ordinal))
            {
                _regionProvider.Add(detached);

                // 必须重新记为"本轮区域"：下一次卸载要靠它再次摘除，
                // 否则区域会留在提供程序里，反复重载后累积。
                state.SetRegion(detached);
                return;
            }

            // 名称已变更：旧区域不再适用，必须重建。先清空区域并让适配器解除
            // 对控件的占用，否则重建时适配器会看到残留状态而抛异常。
            DiscardRegion(element, detached);
        }

        var region = CreateRegion(element, regionName);

        // 记下本轮创建的区域，卸载时据此摘除，避免提供程序里出现同名区域。
        state.SetRegion(region);
    }

    /// <summary>
    /// 丢弃一个不再适用的区域：清空其中的视图，并让适配器解除对控件的占用。
    /// </summary>
    /// <param name="element">区域宿主元素。</param>
    /// <param name="region">要丢弃的区域。</param>
    /// <remarks>
    /// <para>
    /// 顺序不能颠倒：先清空区域，适配器解除占用时控件才不会短暂显示已拆除的视图。
    /// 适配器不实现 <see cref="IRegionAdapterDetach"/> 时只能清空区域，
    /// 重建仍可能因控件状态残留而失败——这是自定义适配器的固有限制，
    /// 异常会照常包装成 <see cref="RegionCreationException"/> 抛出。
    /// </para>
    /// <para>
    /// <b>必须强制拆除。</b>区域被丢弃后不会再回到提供程序，其中的视图——包括声明了
    /// <see cref="IRegionMemberLifetime.KeepAlive"/> 的——都不会再被任何区域复用。
    /// 走普通 <see cref="IRegion.RemoveAll()"/> 会让保活视图被摘掉却不释放，
    /// 作用域与可释放资源永远没人回收。
    /// </para>
    /// </remarks>
    private void DiscardRegion(DependencyObject element, IRegion region)
    {
        if (region is Region concrete)
        {
            concrete.RemoveAll(force: true);
        }
        else
        {
            region.RemoveAll();
        }

        if (_regionAdapterFinder.FindByControlType(element.GetType()) is not IRegionAdapterDetach detachable)
        {
            return;
        }

        detachable.Detach(region, element);
    }

    /// <summary>
    /// 元素卸载时清掉"本轮已创建"标记并摘除本轮创建的区域，使重新挂载后能恢复区域。
    /// </summary>
    /// <param name="sender">事件源。</param>
    /// <param name="e">事件参数。</param>
    /// <remarks>
    /// <para>
    /// 只重置标记，不解绑 <c>Loaded</c>：元素被移到新窗口时会再次触发 <c>Loaded</c>，
    /// 解绑后区域就再也建不起来。
    /// </para>
    /// <para>
    /// 必须同时摘除区域：不摘除会永久残留，提供程序里出现同名区域，
    /// 导航可能命中早已失效的实例。
    /// 但<b>只摘除不拆除、也不重建</b>——旧区域中的视图仍被元素持有
    /// （<c>Content</c> / <c>ItemsSource</c> 仍指向它们），重新加载时复用同一个区域。
    /// 重建会让适配器看到控件状态被占用而抛异常。
    /// </para>
    /// </remarks>
    private void OnElementUnloaded(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement element || !HandledElements.TryGetValue(element, out var state))
        {
            return;
        }

        if (state.TakeRegion() is { } region)
        {
            _regionProvider.Remove(region);
            state.SetDetachedRegion(region);
        }

        state.Reset();
    }

    /// <summary>
    /// 单个元素的订阅状态。
    /// </summary>
    private sealed class ElementState
    {
        private int _created;
        private IRegion? _detachedRegion;
        private IRegion? _region;

        /// <summary>
        /// 标记本轮已创建；已创建过时返回 <see langword="false"/>。
        /// </summary>
        /// <returns>本次调用成功标记时返回 <see langword="true"/>。</returns>
        public bool TryMarkCreated() => Interlocked.Exchange(ref _created, 1) == 0;

        /// <summary>
        /// 记录本轮创建的区域。
        /// </summary>
        /// <param name="region">创建出的区域。</param>
        public void SetRegion(IRegion region) => _region = region;

        /// <summary>
        /// 取出并清空本轮创建的区域。
        /// </summary>
        /// <returns>本轮创建的区域；没有时返回 <see langword="null"/>。</returns>
        public IRegion? TakeRegion()
        {
            var region = _region;

            _region = null;

            return region;
        }

        /// <summary>
        /// 记录卸载时摘除的区域，供下次加载登记回去。
        /// </summary>
        /// <param name="region">被摘除的区域。</param>
        public void SetDetachedRegion(IRegion region) => _detachedRegion = region;

        /// <summary>
        /// 取出并清空卸载时摘除的区域。
        /// </summary>
        /// <returns>被摘除的区域；没有时返回 <see langword="null"/>。</returns>
        public IRegion? TakeDetachedRegion()
        {
            var region = _detachedRegion;

            _detachedRegion = null;

            return region;
        }

        /// <summary>
        /// 重置标记，允许下一次加载重新创建区域。
        /// </summary>
        public void Reset() => Interlocked.Exchange(ref _created, 0);
    }
}
