using Len.Abp.Wpf.Mvvm;

namespace Len.Abp.Wpf.Navigation.Regions;

/// <summary>
/// 区域，维护一组视图及其激活状态，并承载区域行为。
/// </summary>
public interface IRegion
{
    /// <summary>
    /// 获取当前处于激活状态的视图集合。
    /// </summary>
    IViewsCollection ActiveViews { get; }

    /// <summary>
    /// 获取附加到本区域的行为集合。
    /// </summary>
    IRegionBehaviorCollection Behaviors { get; }

    /// <summary>
    /// 获取区域名称，导航按名称查找区域。
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 获取区域所属的视图名称。
    /// </summary>
    string Owner { get; }

    /// <summary>
    /// 获取创建该区域的视图实例；区域不是由视图创建时为 <see langword="null"/>。
    /// </summary>
    /// <remarks>
    /// 按实例而不是按视图名记录所有权，嵌套视图出现多个同名实例时才能准确拆除各自的区域子树。
    /// </remarks>
    IView? OwnerView { get; }

    /// <summary>
    /// 获取区域中的所有视图。
    /// </summary>
    IViewsCollection Views { get; }

    /// <summary>
    /// 激活指定视图。
    /// </summary>
    /// <param name="view">要激活的视图，必须已存在于本区域。</param>
    void Activate(IView view);

    /// <summary>
    /// 把视图加入区域，不改变激活状态。
    /// </summary>
    /// <param name="view">要加入的视图。</param>
    /// <remarks>
    /// <para>
    /// 区域本身只把视图加入集合，不激活它。但<b>区域适配器可以监听集合变更并自动激活</b>：
    /// <see cref="ContentControlRegionAdapter"/> 在区域没有活动视图时会激活新加入的视图，
    /// 这是给直接使用 <see cref="IRegion"/> 的使用者准备的便利行为。
    /// </para>
    /// <para>
    /// 因此调用方不能假设"<c>Add</c> 之后 <see cref="ActiveViews"/> 一定为空"。
    /// 需要知道加入前的状态时，应在 <c>Add</c> 之前取快照——
    /// <see cref="Navigation.NavigationService"/> 就是这么做的。
    /// </para>
    /// </remarks>
    void Add(IView view);

    /// <summary>
    /// 失活指定视图。
    /// </summary>
    /// <param name="view">要失活的视图，必须已存在于本区域。</param>
    /// <remarks>
    /// 只改变激活状态，不保证移除视图：是否拆除由保活声明决定
    /// （见 <see cref="Behaviors.RegionMemberLifetimeBehavior"/>）。
    /// 对从未激活的视图是空操作——它本来就不在 <see cref="ActiveViews"/> 里，
    /// 不会触发任何拆除。要确保回收请用 <see cref="Remove"/>。
    /// </remarks>
    void Deactivate(IView view);

    /// <summary>
    /// 按视图名称查找区域中的视图。
    /// </summary>
    /// <param name="viewName">视图名称。</param>
    /// <returns>匹配的视图；不存在时返回 <see langword="null"/>。</returns>
    IView? GetView(string viewName);

    /// <summary>
    /// 从区域中移除指定视图，并释放不再复用的实例及其导航作用域。
    /// </summary>
    /// <param name="view">要移除的视图，必须已存在于本区域。</param>
    /// <remarks>
    /// 移除是显式回收：视图离开区域后不会再被导航复用，因此即使它声明了保活，
    /// 其作用域与可释放资源也会被释放。这与"失活时按保活声明自动拆除"是两回事。
    /// </remarks>
    void Remove(IView view);

    /// <summary>
    /// 清空区域中的所有视图。
    /// </summary>
    void RemoveAll();
}
