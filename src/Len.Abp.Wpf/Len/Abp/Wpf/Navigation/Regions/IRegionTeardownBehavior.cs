using Len.Abp.Wpf.Mvvm;

namespace Len.Abp.Wpf.Navigation.Regions;

/// <summary>
/// 区域拆除契约，供 <see cref="Region.RemoveAll()"/> 把清空请求转交给负责拆除的行为。
/// </summary>
/// <remarks>
/// 拆除逻辑（保活判断、子树递归、释放）属于 <see cref="Behaviors.RegionMemberLifetimeBehavior"/>，
/// 区域本身不应重复实现。行为未附加时，区域退化为直接清空元数据。
/// </remarks>
internal interface IRegionTeardownBehavior
{
    /// <summary>
    /// 拆除区域中的全部视图：不保活的视图连同其嵌套子树一起释放，保活的视图只移出区域。
    /// </summary>
    /// <param name="force">
    /// 为 <see langword="true"/> 时无视保活声明，释放区域中的全部视图。
    /// </param>
    /// <remarks>
    /// <paramref name="force"/> 用于区域被永久丢弃的场景（区域改名重建、宿主退出）：
    /// 承载视图的区域此后不再存在，保活声明失去意义，跳过释放只会留下孤儿实例。
    /// </remarks>
    void TearDownAll(bool force = false);

    /// <summary>
    /// 摘除区域中所有视图拥有的嵌套区域，不释放视图本身。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="Region.RemoveAll()"/> 在清空元数据前必须调用本方法。保活的视图不会被
    /// <see cref="TearDownAll"/> 移除，因此也走不到 <see cref="IRegionProvider.RemoveOwnedRegions"/>；
    /// 而清空元数据会让视图与区域的所有权关系彻底断开——它拥有的嵌套区域会留在
    /// 提供程序里成为"孤儿"：<see cref="RegionFinder"/> 会把所有者不在任何区域中的区域
    /// 当作根视图区域，导航因此可能命中用户根本看不到的区域。
    /// </para>
    /// <para>
    /// 只摘除不释放：保活视图仍可能被复用，它的嵌套区域会在视图重新加载时重建。
    /// </para>
    /// </remarks>
    void DetachOwnedRegions();

    /// <summary>
    /// 拆除单个视图：不保活时连同其嵌套子树一起释放，保活时只移出区域。
    /// </summary>
    /// <param name="view">要拆除的视图。</param>
    /// <param name="force">
    /// 为 <see langword="true"/> 时无视保活声明，直接移除并释放。
    /// </param>
    /// <remarks>
    /// <para>
    /// <see cref="Region.Remove"/> 必须走这里而不是直接改元数据：视图可能从未激活过
    /// （例如导航校验失败后回收的新建视图），此时它不在 <see cref="IRegion.ActiveViews"/> 中，
    /// 失活通知不会触发，作用域与可释放资源就永远没人回收。
    /// </para>
    /// <para>
    /// 调用方显式移除时传 <see langword="true"/>：视图一旦离开区域就再也不会被导航复用，
    /// 保留其作用域只是泄漏。保活语义约束的是"失活时是否自动拆除"，不是"能否显式移除"。
    /// </para>
    /// </remarks>
    void TearDown(IView view, bool force);

    /// <summary>
    /// 开始延迟拆除：期间失活的视图只登记，不立即拆除。
    /// </summary>
    /// <remarks>
    /// 导航需要"失败即回滚"的语义：激活新视图会让旧视图失活，
    /// 若此时立即拆除，回滚就没有可恢复的实例。延迟到导航成功后再拆，
    /// 失败时取消即可让旧视图原样留在区域中。
    /// </remarks>
    void BeginDeferral();

    /// <summary>
    /// 提交延迟拆除：导航成功后真正拆除期间失活的视图。
    /// </summary>
    void CommitDeferral();

    /// <summary>
    /// 取消延迟拆除：导航失败时丢弃登记，视图保持原样。
    /// </summary>
    void CancelDeferral();
}
