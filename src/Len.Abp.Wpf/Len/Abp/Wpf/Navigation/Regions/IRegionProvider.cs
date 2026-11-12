using Len.Abp.Wpf.Mvvm;

namespace Len.Abp.Wpf.Navigation.Regions;

/// <summary>
/// 区域提供程序，以单例形式保存所有已创建的区域。
/// </summary>
public interface IRegionProvider
{
    /// <summary>
    /// 获取所有已注册的区域，按注册顺序排列。
    /// </summary>
    IReadOnlyList<IRegion> Regions { get; }

    /// <summary>
    /// 注册区域。
    /// </summary>
    /// <param name="region">要注册的区域。</param>
    void Add(IRegion region);

    /// <summary>
    /// 移除由指定视图创建的区域，并返回这些区域中的视图。
    /// </summary>
    /// <param name="ownerView">区域所属的视图实例。</param>
    /// <returns>被摘除区域中的视图，供调用方决定是否继续拆除其子树。</returns>
    /// <remarks>
    /// 按实例而不是按视图名匹配，同名视图存在多个实例时才能只拆除目标实例的子树。
    /// 本方法只处理一层；是否继续深入由调用方按视图的生命周期决定。
    /// </remarks>
    IReadOnlyList<IView> RemoveOwnedRegions(IView ownerView);
}
