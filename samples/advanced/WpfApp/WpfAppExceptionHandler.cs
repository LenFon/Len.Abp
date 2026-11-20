using Len.Abp.Wpf;
using Microsoft.Extensions.Logging;
using System.Windows;
using Volo.Abp.DependencyInjection;

namespace WpfApp;

/// <summary>
/// 自定义全局异常处理：在默认日志之外，把异常写入界面可见的日志并弹窗提示。
/// </summary>
/// <remarks>
/// <para>
/// 通过 <see cref="DependencyAttribute"/> 的 <c>ReplaceServices</c> 顶掉框架默认实现。
/// 必须同时实现 <see cref="ISingletonDependency"/>：ABP 的约定注册只处理带生命周期标记的类型，
/// 仅靠 <c>[Dependency]</c> + <c>[ExposeServices]</c> 不会注册。
/// </para>
/// <para>
/// 弹窗只在 UI 线程异常时出现：后台任务的未观察异常可能来自任意线程，
/// 在非 UI 线程上创建窗口会抛 <see cref="InvalidOperationException"/>。
/// </para>
/// </remarks>
[Dependency(ReplaceServices = true)]
[ExposeServices(typeof(IWpfExceptionHandler))]
public sealed class WpfAppExceptionHandler(
    ILogger<WpfAppExceptionHandler> logger,
    ExceptionLog exceptionLog
) : IWpfExceptionHandler, ISingletonDependency
{
    /// <inheritdoc/>
    public bool HandleDispatcherException(Exception exception)
    {
        logger.LogError(exception, "UI 线程发生未处理异常。");
        exceptionLog.Record($"UI 线程：{exception.Message}");

        // 示例里让进程继续运行，便于观察异常处理效果。
        // 生产环境应结合业务判断：数据已损坏时继续运行可能造成更大问题。
        MessageBox.Show(
            $"{exception.GetType().Name}\n\n{exception.Message}",
            "发生未处理异常",
            MessageBoxButton.OK,
            MessageBoxImage.Error
        );

        return true;
    }

    /// <inheritdoc/>
    public void HandleUnobservedTaskException(Exception exception)
    {
        logger.LogError(exception, "后台任务发生未观察异常。");
        exceptionLog.Record($"后台任务：{exception.Message}");
    }
}

/// <summary>
/// 记录异常消息，供界面展示。
/// </summary>
/// <remarks>
/// 与 <see cref="Regions.RegionActivityLog"/> 分开：异常日志的生命周期与展示位置都不同，
/// 混在一起会让区域演示的日志被异常淹没。
/// </remarks>
public sealed class ExceptionLog : ISingletonDependency
{
    private const int MaxEntries = 20;

    /// <summary>
    /// 获取异常记录，最新的在前。
    /// </summary>
    public System.Collections.ObjectModel.ObservableCollection<string> Entries { get; } = [];

    /// <summary>
    /// 追加一条异常记录。
    /// </summary>
    /// <param name="message">记录内容。</param>
    public void Record(string message)
    {
        Entries.Insert(0, $"{DateTime.Now:HH:mm:ss.fff}  {message}");

        while (Entries.Count > MaxEntries)
        {
            Entries.RemoveAt(Entries.Count - 1);
        }
    }
}
