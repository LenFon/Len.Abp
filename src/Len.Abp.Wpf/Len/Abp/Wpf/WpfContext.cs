using System.Windows;
using System.Windows.Threading;
using Volo.Abp.DependencyInjection;

namespace Len.Abp.Wpf;

internal class WpfContext : ISingletonDependency
{
    /// <summary>
    /// 定义 UI 应用停止时是否同时停止宿主应用。
    /// </summary>
    public bool IsLifetimeLinked { get; set; } = true;

    /// <summary>
    /// WPF 应用是否已启动并仍在运行。
    /// </summary>
    public bool IsRunning { get; set; }

    /// <summary>
    /// WPF 应用生命周期使用的关闭模式，默认为 OnLastWindowClose。
    /// </summary>
    public ShutdownMode ShutdownMode { get; set; } = ShutdownMode.OnLastWindowClose;

    /// <summary>
    /// WPF 应用实例。
    /// </summary>
    public Application WpfApplication { get; set; } = default!;

    /// <summary>
    /// WPF 调度器。
    /// </summary>
    public Dispatcher Dispatcher => WpfApplication.Dispatcher;
}
