namespace Len.Abp.Wpf.Navigation;

/// <summary>
/// 导航服务，负责解析目标视图、切换区域活动视图并通知导航生命周期。
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// 导航到指定名称的视图。
    /// </summary>
    /// <param name="regionName">目标区域名称。</param>
    /// <param name="viewName">目标视图名称。</param>
    /// <param name="navigationParameters">导航参数。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>导航结果；区域不存在或视图解析失败时返回失败结果而不抛异常。</returns>
    /// <exception cref="OperationCanceledException">
    /// <paramref name="cancellationToken"/> 已取消时抛出。取消是唯一不会被包装成失败结果的异常。
    /// </exception>
    ValueTask<INavigationResult> NavigateAsync(
        string regionName,
        string viewName,
        IParameters navigationParameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 导航到指定名称的视图，不附加导航参数。
    /// </summary>
    /// <param name="regionName">目标区域名称。</param>
    /// <param name="viewName">目标视图名称。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>导航结果。</returns>
    /// <exception cref="OperationCanceledException">
    /// <paramref name="cancellationToken"/> 已取消时抛出。
    /// </exception>
    ValueTask<INavigationResult> NavigateAsync(
        string regionName,
        string viewName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 回退到指定区域的上一个视图。
    /// </summary>
    /// <param name="regionName">目标区域名称。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>导航结果；区域没有历史或已到最早一条时返回失败结果。</returns>
    /// <exception cref="OperationCanceledException">
    /// <paramref name="cancellationToken"/> 已取消时抛出。
    /// </exception>
    /// <remarks>
    /// <para>
    /// 历史按区域独立记录，只保存视图名与参数，不保存视图实例。
    /// 回退到已被释放的瞬态视图时会重新解析新实例，参数取历史记录中的值。
    /// </para>
    /// <para>
    /// 提供默认实现是为了不破坏手写 <see cref="INavigationService"/> 实现。
    /// 默认实现抛 <see cref="NotSupportedException"/> 而不是静默返回失败：
    /// "不支持回退"与"回退失败"是两回事，静默失败会让调用方误以为历史为空。
    /// </para>
    /// </remarks>
    /// <exception cref="NotSupportedException">实现未提供导航历史时抛出。</exception>
    ValueTask<INavigationResult> GoBackAsync(string regionName, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException(
            $"{GetType().FullName} 未实现导航历史，无法回退。请使用框架提供的 INavigationService 实现。"
        );
}
