using Len.Abp.Wpf.Mvvm;
using WpfApp.ViewModels;

namespace WpfApp.Views;

/// <summary>
/// MainShell.xaml 的交互逻辑
/// </summary>
[View(typeof(MainShellViewModel), ViewName = "main", IsShell = true)]
public partial class MainShell
{
    public MainShell()
    {
        InitializeComponent();
    }
}