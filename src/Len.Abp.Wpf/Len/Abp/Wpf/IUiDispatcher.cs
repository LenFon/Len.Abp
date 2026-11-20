namespace Len.Abp.Wpf;

/// <summary>
/// UI 线程调度器抽象，供导航等需要线程亲和性的操作封送。
/// </summary>
/// <remarks>
/// 抽成接口而不是直接依赖 <see cref="System.Windows.Threading.Dispatcher"/>：
/// 测试环境没有 <see cref="System.Windows.Application"/> 单例，也无法泵消息，
/// 只能用替身验证封送路径。
/// </remarks>
internal interface IUiDispatcher
{
    /// <summary>
    /// 判断当前线程是否为 UI 线程。
    /// </summary>
    /// <returns>当前线程拥有 UI 对象时返回 <see langword="true"/>。</returns>
    /// <exception cref="InvalidOperationException">WPF 应用尚未启动时抛出。</exception>
    bool CheckAccess();

    /// <summary>
    /// 在 UI 线程上执行异步操作并等待其完成。
    /// </summary>
    /// <typeparam name="T">操作结果类型。</typeparam>
    /// <param name="action">要在 UI 线程上执行的操作。</param>
    /// <returns>操作完成并携带结果的任务。</returns>
    /// <exception cref="InvalidOperationException">WPF 应用尚未启动时抛出。</exception>
    Task<T> InvokeAsync<T>(Func<Task<T>> action);
}
