using System.Windows;
using System.Windows.Threading;
using Volo.Abp.DependencyInjection;

namespace Len.Abp.Wpf;

internal class WpfContext : ISingletonDependency
{
    /// <summary>
    /// Defines if the host application is stopped when the UI applications stops
    /// </summary>
    public bool IsLifetimeLinked { get; set; } = true;

    /// <summary>
    /// Is the WPF application started and still running?
    /// </summary>
    public bool IsRunning { get; set; }

    /// <summary>
    /// This is the WPF ShutdownMode used for the WPF application lifetime, default is OnLastWindowClose
    /// </summary>
    public ShutdownMode ShutdownMode { get; set; } = ShutdownMode.OnLastWindowClose;

    /// <summary>
    /// The Application
    /// </summary>
    public Application WpfApplication { get; set; } = default!;

    /// <summary>
    /// This WPF dispatcher
    /// </summary>
    public Dispatcher Dispatcher => WpfApplication.Dispatcher;
}
