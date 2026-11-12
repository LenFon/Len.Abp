using Len.Abp.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace Len.Abp.Wpf.Navigation.Regions;

/// <summary>
/// 允许 <see cref="RegionAdapterFinder"/> 在不了解泛型参数的情况下为适配器提供延迟服务提供程序。
/// </summary>
internal interface IRegionAdapterServiceProviderAware
{
    IAbpLazyServiceProvider? AbpLazyServiceProvider { get; set; }
}

/// <summary>
/// 区域适配器的通用基类，负责创建区域、适配目标控件并附加行为。
/// </summary>
/// <typeparam name="T">区域宿主的控件类型。</typeparam>
public abstract class BasicRegionAdapter<T> : IRegionAdapter, IRegionAdapterServiceProviderAware
    where T : class
{
    /// <summary>
    /// 获取或设置 ABP 延迟服务提供程序。
    /// </summary>
    /// <remarks>
    /// 该属性由 <see cref="RegionAdapterFinder"/> 在解析适配器时赋值，因此不依赖容器的属性注入能力。
    /// 使用默认 DI 容器时 ABP 不会执行属性注入，若只依赖注入该属性会为 null。
    /// </remarks>
    public IAbpLazyServiceProvider? AbpLazyServiceProvider { get; set; }

    /// <summary>
    /// 获取区域配置。
    /// </summary>
    protected RegionOptions Options => ServiceProvider.GetRequiredService<IOptions<RegionOptions>>().Value;

    /// <summary>
    /// 获取服务提供程序。
    /// </summary>
    protected IServiceProvider ServiceProvider =>
        AbpLazyServiceProvider is { } lazyServiceProvider
            ? lazyServiceProvider.LazyGetRequiredService<IServiceProvider>()
            : AbpServiceProvider.Current;

    IRegion IRegionAdapter.Initialize(object regionTarget, string regionName, string viewName)
    {
        ArgumentNullException.ThrowIfNull(regionTarget, nameof(regionTarget));
        ArgumentNullException.ThrowIfNull(regionName, nameof(regionName));
        ArgumentNullException.ThrowIfNull(viewName, nameof(viewName));

        if (regionTarget is not T castedObject)
        {
            throw new ArgumentException($"区域目标必须是 {typeof(T).FullName} 类型。", nameof(regionTarget));
        }

        return Initialize(castedObject, regionName, viewName);
    }

    /// <summary>
    /// 将区域适配到目标控件，通常用于把区域的活动视图同步到控件内容。
    /// </summary>
    /// <param name="region">要适配的区域。</param>
    /// <param name="regionTarget">区域宿主控件。</param>
    protected abstract void Adapt(IRegion region, T regionTarget);

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
    /// <param name="viewName">区域所属的视图名称。</param>
    /// <returns>创建的区域。</returns>
    protected abstract IRegion CreateRegion(string regionName, string viewName);

    private IRegion Initialize(T regionTarget, string regionName, string viewName)
    {
        var region = CreateRegion(regionName, viewName);

        Adapt(region, regionTarget);
        AttachBehaviors(region, regionTarget);
        AttachDefaultBehaviors(region, regionTarget);

        return region;
    }
}