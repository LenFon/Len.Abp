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
    /// UI 线程未处理异常是否标记为已处理。
    /// </summary>
    public bool HandleUnhandledExceptions { get; set; }

    /// <summary>
    /// WPF 应用实例。
    /// </summary>
    public Application WpfApplication { get; set; } = default!;

    /// <summary>
    /// WPF 调度器。
    /// </summary>
    /// <remarks>
    /// 声明为虚属性，测试可以替换成不依赖 <see cref="Application"/> 实例的调度器。
    /// </remarks>
    public virtual Dispatcher Dispatcher => WpfApplication.Dispatcher;

    /// <summary>
    /// 请求 WPF 应用关闭。
    /// </summary>
    /// <remarks>
    /// 声明为虚方法，测试可以替换成不依赖 <see cref="Application"/> 实例的实现：
    /// 一个 AppDomain 只能创建一个 <see cref="Application"/>，而它已被生命周期测试占用。
    /// </remarks>
    public virtual void Shutdown() => WpfApplication.Shutdown();
}
