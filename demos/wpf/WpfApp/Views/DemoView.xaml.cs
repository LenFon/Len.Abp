using Len.Abp.Wpf.Mvvm;
using System.Windows.Controls;
using WpfApp.ViewModels;

namespace WpfApp.Views;

/// <summary>
/// DemoView.xaml 的交互逻辑
/// </summary>
[View(typeof(DemoViewModel), ViewName = "demo")]
public partial class DemoView : UserControl
{
    public DemoView()
    {
        InitializeComponent();
    }
}