using Len.Abp.Wpf.Mvvm;
using System.Windows;
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

        // 区域在宿主控件的 Loaded 中创建，而 Window.Loaded 先于子元素触发。
        // 挂到区域宿主自身，保证首次导航时区域已经存在。
        RegionHost.Loaded += OnRegionHostLoaded;
    }

    private async void OnRegionHostLoaded(object sender, RoutedEventArgs e)
    {
        RegionHost.Loaded -= OnRegionHostLoaded;

        await ViewModel.ShowGalleryCommand.ExecuteAsync(null);

        if (NestedRegionDiagnostics.IsEnabled)
        {
            await NestedRegionDiagnostics.RunAsync(ViewModel.NavigationService, Console.WriteLine);
        }
    }
}
