using System.Windows;

namespace Len.Abp.Wpf;

public class WpfOptions
{
    /// <summary>
    /// 定义 UI 应用停止时是否同时停止宿主应用。
    /// </summary>
    public bool IsLifetimeLinked { get; set; } = true;

    /// <summary>
    /// WPF 应用生命周期使用的关闭模式，默认为 OnLastWindowClose。
    /// </summary>
    public ShutdownMode ShutdownMode { get; set; } = ShutdownMode.OnLastWindowClose;

    /// <summary>
    /// 是否只允许运行单个应用实例。
    /// </summary>
    public bool SingleInstance { get; set; }

    /// <summary>
    /// UI 线程未处理异常是否标记为已处理。默认 false，保持 WPF 原有崩溃行为。
    /// </summary>
    public bool HandleUnhandledExceptions { get; set; }
}
