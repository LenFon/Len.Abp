using Len.Abp.Wpf.Mvvm;
using System.Windows.Controls;
using WpfApp.ViewModels;

namespace WpfApp.Views;

/// <summary>
/// 演示视图的导航目标，演示被导航到的视图。
/// </summary>
[View<Demo2ViewModel>(ViewName = "demo2")]
public partial class Demo2View : UserControl
{
    public Demo2View()
    {
        InitializeComponent();
    }
}