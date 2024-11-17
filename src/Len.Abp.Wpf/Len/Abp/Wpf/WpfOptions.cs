using System.Windows;

namespace Len.Abp.Wpf;

public class WpfOptions
{
    /// <summary>
    /// Defines if the host application is stopped when the UI applications stops
    /// </summary>
    public bool IsLifetimeLinked { get; set; } = true;

    /// <summary>
    /// This is the WPF ShutdownMode used for the WPF application lifetime, default is OnLastWindowClose
    /// </summary>
    public ShutdownMode ShutdownMode { get; set; } = ShutdownMode.OnLastWindowClose;

    public bool SingleInstance { get; set; }
}
