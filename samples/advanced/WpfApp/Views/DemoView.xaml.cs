using Len.Abp.Wpf.Mvvm;
using System.Windows.Controls;
using WpfApp.ViewModels;

namespace WpfApp.Views;

/// <summary>
/// 演示视图，演示通过导航服务切换到其他视图。
/// </summary>
[View<DemoViewModel>(ViewName = "demo")]
public partial class DemoView : UserControl
{
    public DemoView()
    {
        InitializeComponent();
    }
}