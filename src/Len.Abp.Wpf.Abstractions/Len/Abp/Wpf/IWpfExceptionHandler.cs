namespace Len.Abp.Wpf;

/// <summary>
/// 全局未处理异常的处理策略。
/// </summary>
/// <remarks>
/// <para>
/// 框架默认实现只记录日志，并按 <c>WpfOptions.HandleUnhandledExceptions</c> 决定
/// 是否让进程继续运行。需要弹窗、上报或按环境区分行为时，实现本接口并同时实现
/// <c>ISingletonDependency</c>（ABP 的约定注册要求类型带生命周期标记），
/// 再用 <c>[Dependency(ReplaceServices = true)]</c> 顶掉默认实现即可，无需修改框架代码。
/// </para>
/// <para>
/// 后台任务的未观察异常由框架无条件标记为已观察：未观察异常默认只在 GC 时静默丢弃，
/// 既不崩溃也不留线索。实现方只需记录，不必关心 <c>SetObserved</c>。
/// </para>
/// </remarks>
public interface IWpfExceptionHandler
{
    /// <summary>
    /// 处理 UI 线程未处理异常。
    /// </summary>
    /// <param name="exception">未处理异常。</param>
    /// <returns>
    /// 为 <see langword="true"/> 时进程继续运行；为 <see langword="false"/> 时保持 WPF 原有的崩溃行为。
    /// </returns>
    bool HandleDispatcherException(Exception exception);

    /// <summary>
    /// 处理后台任务的未观察异常。
    /// </summary>
    /// <param name="exception">未观察异常。</param>
    void HandleUnobservedTaskException(Exception exception);
}
