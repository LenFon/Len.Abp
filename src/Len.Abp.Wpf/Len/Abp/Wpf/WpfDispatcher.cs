using Volo.Abp.DependencyInjection;

namespace Len.Abp.Wpf;

/// <summary>
/// <see cref="IUiDispatcher"/> 的默认实现，包装 <see cref="WpfContext.Dispatcher"/>。
/// </summary>
/// <param name="wpfContext">WPF 上下文。</param>
/// <remarks>
/// 必须显式声明 <see cref="ExposeServicesAttribute"/>：ABP 的约定注册只为公开接口建立映射，
/// 而 <see cref="IUiDispatcher"/> 是 internal，不声明时容器里只有具体类型，
/// 导航服务解析接口会失败。
/// </remarks>
[ExposeServices(typeof(IUiDispatcher))]
internal sealed class WpfDispatcher(WpfContext wpfContext) : IUiDispatcher, ISingletonDependency
{
    private readonly WpfContext _wpfContext = wpfContext;

    /// <inheritdoc/>
    public bool CheckAccess() => GetDispatcher().CheckAccess();

    /// <inheritdoc/>
    public Task<T> InvokeAsync<T>(Func<Task<T>> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        return GetDispatcher().InvokeAsync(action).Task.Unwrap();
    }

    /// <summary>
    /// 获取调度器，应用未启动时给出明确异常。
    /// </summary>
    /// <remarks>
    /// <see cref="WpfContext.WpfApplication"/> 在 UI 线程启动前是 <see langword="default"/>，
    /// 直接访问 <see cref="WpfContext.Dispatcher"/> 会抛 <see cref="NullReferenceException"/>。
    /// 这里先检查运行状态，让调用方看到可操作的错误信息。
    /// </remarks>
    private System.Windows.Threading.Dispatcher GetDispatcher()
    {
        if (!_wpfContext.IsRunning)
        {
            throw new InvalidOperationException("WPF 应用尚未启动，无法导航。");
        }

        return _wpfContext.Dispatcher;
    }
}
