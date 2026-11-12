using Volo.Abp.DependencyInjection;

namespace Len.Abp.Wpf.Navigation.Regions.Behaviors;

/// <summary>
/// 区域行为：区域创建后将其登记到 <see cref="IRegionProvider"/>，使导航服务能够按名称找到它。
/// </summary>
/// <param name="regionProvider">区域提供程序。</param>
[ExposeKeyedService<IRegionBehavior>(nameof(RegionProviderRegistrationBehavior))]
public class RegionProviderRegistrationBehavior(IRegionProvider regionProvider) : IRegionBehavior, ITransientDependency
{
    private readonly IRegionProvider _regionProvider = regionProvider;

    /// <inheritdoc/>
    public IRegion Region { get; set; } = default!;

    /// <inheritdoc/>
    public void Attach()
    {
        _regionProvider.Add(Region);
    }
}
