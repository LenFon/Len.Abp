using Len.Abp.Wpf.Mvvm;
using WpfApp.ViewModels;

namespace WpfApp.Views;

/// <summary>
/// MainShell.xaml 的交互逻辑
/// </summary>
[View<MainShellViewModel>(ViewName = "main", IsShell = true)]
public partial class MainShell
{
    /// <summary>
    /// 初始化主窗口。
    /// </summary>
    public MainShell()
    {
        InitializeComponent();
    }
}