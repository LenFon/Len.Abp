namespace Len.Abp.Wpf.Navigation.Regions;

/// <summary>
/// 视图保活契约，由视图模型实现以声明离开区域后是否保留实例。
/// </summary>
public interface IRegionMemberLifetime
{
    /// <summary>
    /// 获取视图失活后是否保留在区域中复用。
    /// </summary>
    /// <remarks>
    /// 返回 <see langword="false"/> 时视图会被移出区域并释放瞬态实例。
    /// 未实现本接口等同于 <see langword="false"/>。
    /// </remarks>
    bool KeepAlive { get; }
}
