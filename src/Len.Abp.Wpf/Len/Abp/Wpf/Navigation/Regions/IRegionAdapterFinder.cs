using Volo.Abp.DependencyInjection;

namespace Len.Abp.Wpf.Navigation.Regions;

/// <summary>
/// 区域适配器查找器，按控件类型定位对应的区域适配器。
/// </summary>
public interface IRegionAdapterFinder : ISingletonDependency
{
    /// <summary>
    /// 沿控件类型的继承链查找区域适配器。
    /// </summary>
    /// <param name="controlType">区域宿主的控件类型。</param>
    /// <returns>匹配的区域适配器。</returns>
    /// <exception cref="KeyNotFoundException">整条继承链都没有注册适配器时抛出。</exception>
    IRegionAdapter FindByControlType(Type controlType);
}
