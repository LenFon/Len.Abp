using Len.Abp.Wpf;
using Len.Abp.Wpf.Mvvm;
using WpfApp.ViewModels;

namespace WpfApp.Views;

/// <summary>
/// MainShell.xaml 的交互逻辑
/// </summary>
[View(typeof(MainShellViewModel))]
public partial class MainShell : IShell
{
    public MainShell()
    {
        InitializeComponent();
    }
}
