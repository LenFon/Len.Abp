using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.DependencyInjection;

namespace Len.Abp.Wpf.Navigation.Regions;

/// <summary>
/// <see cref="IRegionAdapterFinder"/> 的默认实现，沿控件类型的继承链查找键控注册的区域适配器。
/// </summary>
internal class RegionAdapterFinder(IAbpLazyServiceProvider abpLazyServiceProvider) : IRegionAdapterFinder
{
    private readonly IAbpLazyServiceProvider _abpLazyServiceProvider = abpLazyServiceProvider;

    /// <inheritdoc/>
    public IRegionAdapter FindByControlType(Type controlType)
    {
        var currentType = controlType;

        while (currentType != null)
        {
            var regionAdapter = _abpLazyServiceProvider.GetKeyedService<IRegionAdapter>(currentType);
            if (regionAdapter is not null)
            {
                // 适配器需要延迟服务提供程序来解析选项与行为；显式赋值，
                // 这样默认 DI 容器（不执行属性注入）下也能正常工作。
                if (regionAdapter is IRegionAdapterServiceProviderAware aware)
                {
                    aware.AbpLazyServiceProvider ??= _abpLazyServiceProvider;
                }

                return regionAdapter;
            }

            currentType = currentType.BaseType;
        }

        throw new KeyNotFoundException($"未找到控件类型 {controlType.FullName} 对应的区域适配器。请确认该控件类型已注册区域适配器。");
    }
}
