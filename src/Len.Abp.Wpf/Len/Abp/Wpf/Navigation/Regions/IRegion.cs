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
    void Add(IView view);

    /// <summary>
    /// 失活指定视图。
    /// </summary>
    /// <param name="view">要失活的视图，必须已存在于本区域。</param>
    void Deactivate(IView view);

    /// <summary>
    /// 按视图名称查找区域中的视图。
    /// </summary>
    /// <param name="viewName">视图名称。</param>
    /// <returns>匹配的视图；不存在时返回 <see langword="null"/>。</returns>
    IView? GetView(string viewName);

    /// <summary>
    /// 从区域中移除指定视图。
    /// </summary>
    /// <param name="view">要移除的视图，必须已存在于本区域。</param>
    void Remove(IView view);

    /// <summary>
    /// 清空区域中的所有视图。
    /// </summary>
    void RemoveAll();
}
