using Len.Abp.Wpf.Mvvm;
using System.Windows;
using WpfApp.ViewModels;

namespace WpfApp.Views;

/// <summary>
/// 主窗口，作为导航枢纽在区域中切换各个演示视图。
/// </summary>
[View<MainShellViewModel>(ViewName = "main", IsShell = true)]
public partial class MainShell
{
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
