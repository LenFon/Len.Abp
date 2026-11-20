using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace Len.Abp.Wpf;

/// <summary>
/// 记录未处理异常，并按配置决定是否让进程继续运行。
/// </summary>
/// <remarks>
/// 抽成独立类型而不是直接写在事件处理器里：
/// <see cref="System.Windows.Threading.DispatcherUnhandledExceptionEventArgs"/> 的构造函数是 internal，
/// 测试无法构造该参数对象，只能直接验证这里的判定与日志。
/// 需要自定义行为时实现 <see cref="IWpfExceptionHandler"/> 并替换本类型，不必修改框架。
/// </remarks>
[ExposeServices(typeof(IWpfExceptionHandler))]
internal sealed class WpfExceptionHandler(WpfContext wpfContext, IAbpLazyServiceProvider abpLazyServiceProvider)
    : IWpfExceptionHandler,
        ISingletonDependency
{
    private readonly WpfContext _wpfContext = wpfContext;
    private readonly IAbpLazyServiceProvider _abpLazyServiceProvider = abpLazyServiceProvider;

    /// <summary>
    /// 记录 UI 线程未处理异常，并返回是否将其标记为已处理。
    /// </summary>
    /// <param name="exception">未处理异常。</param>
    /// <returns>为 true 时进程继续运行，为 false 时保持 WPF 原有的崩溃行为。</returns>
    public bool HandleDispatcherException(Exception exception)
    {
        Logger?.LogError(exception, "UI 线程发生未处理异常。");

        return _wpfContext.HandleUnhandledExceptions;
    }

    /// <summary>
    /// 记录后台任务的未观察异常。
    /// </summary>
    /// <param name="exception">未观察异常。</param>
    /// <remarks>
    /// 标记已观察由 <see cref="WpfThreadService"/> 统一完成，
    /// 这样替换实现时不必重复处理该细节。
    /// </remarks>
    public void HandleUnobservedTaskException(Exception exception) =>
        Logger?.LogError(exception, "后台任务发生未观察异常。");

    private ILogger<WpfExceptionHandler>? Logger =>
        _abpLazyServiceProvider.LazyGetService<ILogger<WpfExceptionHandler>>();
}
