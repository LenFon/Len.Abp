using Len.Abp.Wpf.Mvvm;
using System.Windows.Controls;
using WpfApp.ViewModels;

namespace WpfApp.Views;

/// <summary>
/// 区域适配器与导航能力演示视图。
/// </summary>
[View<RegionAdaptersViewModel>(ViewName = "regions")]
public partial class RegionAdaptersView : UserControl
{
    public RegionAdaptersView()
    {
        InitializeComponent();
    }
}
