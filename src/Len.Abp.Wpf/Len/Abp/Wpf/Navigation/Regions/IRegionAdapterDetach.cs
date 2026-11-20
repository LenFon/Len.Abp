using Len.Abp.Wpf.Mvvm;

namespace Len.Abp.Wpf.Navigation.Regions;

/// <summary>
/// 区域适配器的可选能力：解除对区域宿主控件的占用。
/// </summary>
/// <remarks>
/// <para>
/// 适配器在 <see cref="IRegionAdapter.Initialize"/> 时会把控件状态单向指向区域内容
/// （<c>ContentControl.Content</c>、<c>ItemsControl.ItemsSource</c>、选项卡集合），
/// 这些引用在区域被摘除后仍然存在。若之后必须重建区域（例如区域名称变更），
/// 适配器的守卫会判定"控件状态已被占用"并抛异常。
/// </para>
/// <para>
/// 本接口是<b>可选能力</b>而不是 <see cref="IRegionAdapter"/> 的成员：
/// 使用者自定义的适配器不实现它时，退化为"无法重建"，不影响既有行为。
/// 框架自带的适配器都通过 <see cref="BasicRegionAdapter{T}"/> 实现它。
/// </para>
/// <para>
/// 实现时只清除适配器自己写入的状态，不触碰使用者设置的值。
/// 调用前区域应已清空（<see cref="IRegion.RemoveAll"/>），否则控件会短暂显示空内容。
/// </para>
/// </remarks>
public interface IRegionAdapterDetach
{
    /// <summary>
    /// 解除适配器对控件的占用，使控件可以重新适配。
    /// </summary>
    /// <param name="region">即将被丢弃的区域。</param>
    /// <param name="regionTarget">区域宿主控件。</param>
    /// <remarks>
    /// 只清除适配器自己写入的状态，不触碰使用者设置的值。
    /// 调用前区域应已清空（<see cref="IRegion.RemoveAll"/>），否则控件会短暂显示空内容。
    /// </remarks>
    void Detach(IRegion region, object regionTarget);
}
