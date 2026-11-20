using Len.Abp.Wpf.Mvvm;
using MinimalApp.ViewModels;
using System.Windows;

namespace MinimalApp.Views;

/// <summary>
/// 主窗口，承载唯一的导航区域。
/// </summary>
/// <remarks>
/// <c>IsShell = true</c> 的视图会被注册为单例，并由框架在启动时显示，
/// 因此必须派生自 <see cref="Window"/>。
/// </remarks>
[View<MainViewModel>(ViewName = "main", IsShell = true)]
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
}
