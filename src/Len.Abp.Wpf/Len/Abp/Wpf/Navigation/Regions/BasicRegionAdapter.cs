using Len.Abp.Wpf.Mvvm;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Runtime.CompilerServices;
using Volo.Abp.DependencyInjection;

namespace Len.Abp.Wpf.Navigation.Regions;

/// <summary>
/// 区域适配器的通用基类，负责创建区域、适配目标控件并附加行为。
/// </summary>
/// <typeparam name="T">区域宿主的控件类型。</typeparam>
/// <remarks>
/// 派生类由容器构造，因此通过构造函数拿到 <see cref="IAbpLazyServiceProvider"/>，
/// 不需要外部在解析后回填。
/// <para>
/// 适配器是单例，一个实例会服务多个区域（多个同类型控件），
/// 因此订阅与清理动作必须按区域记录，不能放在实例字段上。
/// </para>
/// </remarks>
public abstract class BasicRegionAdapter<T> : IRegionAdapter, IRegionAdapterDetach
    where T : class
{
    /// <summary>
    /// 每个区域在解除适配时要执行的清理动作。
    /// </summary>
    /// <remarks>
    /// 用 <see cref="ConditionalWeakTable{TKey, TValue}"/> 而不是字典：
    /// 键是区域，弱引用保证正常复用路径（不解除适配）下条目不会阻止区域被回收。
    /// </remarks>
    private readonly ConditionalWeakTable<IRegion, List<Action>> _detachActions = [];

    /// <summary>
    /// 初始化区域适配器。
    /// </summary>
    /// <param name="abpLazyServiceProvider">ABP 延迟服务提供程序。</param>
    protected BasicRegionAdapter(IAbpLazyServiceProvider abpLazyServiceProvider)
    {
        ArgumentNullException.ThrowIfNull(abpLazyServiceProvider);

        AbpLazyServiceProvider = abpLazyServiceProvider;
    }

    /// <summary>
    /// 获取 ABP 延迟服务提供程序。
    /// </summary>
    protected IAbpLazyServiceProvider AbpLazyServiceProvider { get; }

    /// <summary>
    /// 获取区域配置。
    /// </summary>
    protected RegionOptions Options => ServiceProvider.GetRequiredService<IOptions<RegionOptions>>().Value;

    /// <summary>
    /// 获取服务提供程序。
    /// </summary>
    protected IServiceProvider ServiceProvider =>
        AbpLazyServiceProvider.LazyGetRequiredService<IServiceProvider>();

    IRegion IRegionAdapter.Initialize(object regionTarget, string regionName, IView ownerView)
    {
        ArgumentNullException.ThrowIfNull(regionTarget, nameof(regionTarget));
        ArgumentNullException.ThrowIfNull(regionName, nameof(regionName));
        ArgumentNullException.ThrowIfNull(ownerView, nameof(ownerView));

        return Initialize(CastTarget(regionTarget), regionName, ownerView);
    }

    /// <summary>
    /// 把区域目标转换为派生类声明的控件类型。
    /// </summary>
    /// <param name="regionTarget">区域宿主控件。</param>
    /// <returns>转换后的控件。</returns>
    /// <exception cref="ArgumentException">类型不匹配时抛出。</exception>
    /// <remarks>
    /// 初始化与解除适配两处都要做同样的转换与报错，集中在一处避免两条路径的
    /// 异常消息与判定条件出现分歧。
    /// </remarks>
    private static T CastTarget(object regionTarget) =>
        regionTarget as T
        ?? throw new ArgumentException($"区域目标必须是 {typeof(T).FullName} 类型。", nameof(regionTarget));

    /// <summary>
    /// 将区域适配到目标控件，通常用于把区域的活动视图同步到控件内容。
    /// </summary>
    /// <param name="region">要适配的区域。</param>
    /// <param name="regionTarget">区域宿主控件。</param>
    protected abstract void Adapt(IRegion region, T regionTarget);

    /// <summary>
    /// 解除适配器对控件的占用，使控件可以重新适配。
    /// </summary>
    /// <param name="region">即将被丢弃的区域。</param>
    /// <param name="regionTarget">区域宿主控件。</param>
    /// <remarks>
    /// <para>
    /// 默认不做事：只有真正写入控件状态的适配器才需要重写。
    /// 实现时只清除自己写入的值，不触碰使用者设置的内容。
    /// </para>
    /// <para>
    /// <b>不需要在这里处理 <see cref="OnDetach"/> 登记的清理动作</b>：
    /// <see cref="IRegionAdapterDetach.Detach"/> 的显式实现会先执行它们再调用本方法，
    /// 因此漏调 <c>base.Detach</c> 也不会导致订阅泄漏，最坏情况只是控件状态没清干净，
    /// 重建时立刻以异常暴露，不会静默累积。
    /// </para>
    /// </remarks>
    protected virtual void Detach(IRegion region, T regionTarget)
    {
    }

    /// <summary>
    /// 执行 <see cref="OnDetach"/> 登记的清理动作。
    /// </summary>
    /// <param name="region">即将被丢弃的区域。</param>
    /// <remarks>
    /// 逐个兜底：一个清理动作失败不应阻止其余动作执行。
    /// </remarks>
    private void RunDetachActions(IRegion region)
    {
        if (!_detachActions.TryGetValue(region, out var actions))
        {
            return;
        }

        _detachActions.Remove(region);

        foreach (var action in actions)
        {
            try
            {
                action();
            }
            catch (Exception)
            {
                // 解除订阅失败不影响控件状态已被清除的事实，忽略即可。
            }
        }
    }

    /// <summary>
    /// 登记一个在解除适配时执行的清理动作。
    /// </summary>
    /// <param name="region">清理动作所属的区域。</param>
    /// <param name="action">清理动作，通常是解除事件订阅。</param>
    /// <remarks>
    /// 适配器是单例，必须按区域登记：同一实例服务多个控件，
    /// 放在实例字段上会让后一个区域的订阅覆盖前一个。
    /// </remarks>
    protected void OnDetach(IRegion region, Action action)
    {
        ArgumentNullException.ThrowIfNull(region);
        ArgumentNullException.ThrowIfNull(action);

        _detachActions.GetOrCreateValue(region).Add(action);
    }

    /// <summary>
    /// 附加适配器自定义的行为。
    /// </summary>
    /// <param name="region">要附加行为的区域。</param>
    /// <param name="regionTarget">区域宿主控件。</param>
    protected virtual void AttachBehaviors(IRegion region, T regionTarget)
    {
    }

    /// <summary>
    /// 附加 <see cref="RegionOptions.BehaviorKeys"/> 中配置的默认行为。
    /// </summary>
    /// <param name="region">要附加行为的区域。</param>
    /// <param name="regionTarget">区域宿主控件。</param>
    protected virtual void AttachDefaultBehaviors(IRegion region, T regionTarget)
    {
        ArgumentNullException.ThrowIfNull(region, nameof(region));
        ArgumentNullException.ThrowIfNull(regionTarget, nameof(regionTarget));

        foreach (var behaviorKey in Options.BehaviorKeys)
        {
            if (!region.Behaviors.ContainsKey(behaviorKey))
            {
                var behavior = ServiceProvider.GetRequiredKeyedService<IRegionBehavior>(behaviorKey);

                region.Behaviors.Add(behaviorKey, behavior);
            }
        }
    }

    /// <summary>
    /// 创建区域实例。
    /// </summary>
    /// <param name="regionName">区域名称。</param>
    /// <param name="ownerView">区域所属的视图实例。</param>
    /// <returns>创建的区域。</returns>
    protected abstract IRegion CreateRegion(string regionName, IView ownerView);

    private IRegion Initialize(T regionTarget, string regionName, IView ownerView)
    {
        var region = CreateRegion(regionName, ownerView);

        Adapt(region, regionTarget);
        AttachBehaviors(region, regionTarget);
        AttachDefaultBehaviors(region, regionTarget);

        return region;
    }

    /// <summary>
    /// 解除适配器对控件的占用。
    /// </summary>
    /// <param name="region">即将被丢弃的区域。</param>
    /// <param name="regionTarget">区域宿主控件。</param>
    /// <exception cref="ArgumentException">区域目标类型不匹配时抛出。</exception>
    /// <remarks>
    /// 先执行 <see cref="OnDetach"/> 登记的清理动作，再调用派生类的
    /// <see cref="Detach"/>。顺序由这里固定，派生类漏调 <c>base.Detach</c>
    /// 也不会丢掉订阅清理——最坏只是控件状态没清干净，重建时会立刻暴露。
    /// </remarks>
    void IRegionAdapterDetach.Detach(IRegion region, object regionTarget)
    {
        ArgumentNullException.ThrowIfNull(region);
        ArgumentNullException.ThrowIfNull(regionTarget);

        RunDetachActions(region);
        Detach(region, CastTarget(regionTarget));
    }
}
