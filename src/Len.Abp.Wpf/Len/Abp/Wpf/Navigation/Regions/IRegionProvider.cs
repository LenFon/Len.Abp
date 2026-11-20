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
    /// 摘除指定区域，不释放其中的视图。
    /// </summary>
    /// <param name="region">要摘除的区域。</param>
    /// <returns>区域存在并被摘除时返回 <see langword="true"/>。</returns>
    /// <remarks>
    /// 用于卸载场景：区域宿主从可视树摘除时，区域必须从提供程序摘除，
    /// 否则提供程序里会累积同名区域，导航可能命中早已失效的旧实例。
    /// 只摘除不释放——旧区域中的视图可能仍被元素持有，重新加载时会复用同一个区域。
    /// </remarks>
    bool Remove(IRegion region);

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

    /// <summary>
    /// 清空所有区域，并返回被清空区域中的视图。
    /// </summary>
    /// <returns>被清空区域中的视图，供调用方决定是否继续拆除其子树。</returns>
    /// <remarks>
    /// 只摘除区域，不负责释放视图：释放规则属于区域行为，由调用方按生命周期决定。
    /// 应用退出时用它断开提供程序对全部区域的引用。
    /// </remarks>
    IReadOnlyList<IView> Clear();
}
