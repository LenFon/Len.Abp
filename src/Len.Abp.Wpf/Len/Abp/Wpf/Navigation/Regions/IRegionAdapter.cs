using Len.Abp.Wpf.Mvvm;

namespace Len.Abp.Wpf.Navigation.Regions;

/// <summary>
/// 区域适配器，为特定控件类型创建并初始化区域。
/// </summary>
public interface IRegionAdapter
{
    /// <summary>
    /// 为区域宿主创建并初始化区域。
    /// </summary>
    /// <param name="regionTarget">区域宿主控件。</param>
    /// <param name="regionName">区域名称。</param>
    /// <param name="ownerView">区域所属的视图实例。</param>
    /// <returns>创建的区域。</returns>
    /// <remarks>
    /// 传入视图实例而不是视图名，区域才能按实例记录所有权，嵌套视图存在多个同名实例时也能准确拆除。
    /// </remarks>
    IRegion Initialize(object regionTarget, string regionName, IView ownerView);
}
