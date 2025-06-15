using Len.Abp.Wpf.Mvvm;
using System.Windows.Controls;
using WpfApp.ViewModels;

namespace WpfApp.Views;

/// <summary>
/// Demo2View.xaml 的交互逻辑
/// </summary>
[View(typeof(Demo2ViewModel), ViewName = "demo2")]
public partial class Demo2View : UserControl
{
    public Demo2View()
    {
        InitializeComponent();
    }
}