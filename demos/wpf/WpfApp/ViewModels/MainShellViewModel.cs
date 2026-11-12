using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Len.Abp.Wpf;
using Len.Abp.Wpf.Navigation;
using Volo.Abp.DependencyInjection;

namespace WpfApp.ViewModels;

/// <summary>
/// 主窗口视图模型，作为导航枢纽在区域中切换各个演示视图。
/// </summary>
/// <remarks>
/// Shell 视图是单例，视图模型若声明为 Scoped 会形成 captive dependency，
/// 因此这里使用瞬态生命周期。
/// </remarks>
public partial class MainShellViewModel(INavigationService navigationService) : ObservableObject, ITransientDependency
{
    /// <summary>
    /// 获取导航服务，供自检等诊断代码直接使用。
    /// </summary>
    public INavigationService NavigationService => navigationService;

    /// <summary>
    /// 获取或设置窗口标题。
    /// </summary>
    [ObservableProperty]
    public partial string Title { get; set; } = "Len.Abp 生成器示例";

    /// <summary>
    /// 获取或设置当前视图名称，用于高亮导航按钮。
    /// </summary>
    [ObservableProperty]
    public partial string CurrentViewName { get; set; } = "gallery";

    /// <summary>
    /// 获取或设置最近一次导航的结果文本。
    /// </summary>
    [ObservableProperty]
    public partial string NavigationStatus { get; set; } = "就绪";

    /// <summary>
    /// 导航到控件画廊。
    /// </summary>
    [RelayCommand]
    private Task ShowGalleryAsync() => NavigateAsync("gallery", IParameters.Create());

    /// <summary>
    /// 导航到单例生命周期演示。
    /// </summary>
    [RelayCommand]
    private Task ShowSingletonAsync() => NavigateAsync("lifetime-singleton", IParameters.Create("?source=shell"));

    /// <summary>
    /// 导航到瞬态生命周期演示。
    /// </summary>
    [RelayCommand]
    private Task ShowTransientAsync() => NavigateAsync("lifetime-transient", IParameters.Create("?source=shell"));

    /// <summary>
    /// 导航到作用域生命周期演示。
    /// </summary>
    [RelayCommand]
    private Task ShowScopedAsync() => NavigateAsync("lifetime-scoped", IParameters.Create("?source=shell"));

    /// <summary>
    /// 导航到八层嵌套区域演示。
    /// </summary>
    [RelayCommand]
    private Task ShowNestedRegionAsync() => NavigateAsync("nested-root", IParameters.Create());

    /// <summary>
    /// 导航到原有的导航示例视图。
    /// </summary>
    [RelayCommand]
    private Task ShowDemoAsync() => NavigateAsync("demo", IParameters.Create());

    /// <summary>
    /// 导航到登录视图。
    /// </summary>
    [RelayCommand]
    private Task ShowLoginAsync() => NavigateAsync("login", IParameters.Create());

    private async Task NavigateAsync(string viewName, IParameters parameters)
    {
        var result = await navigationService.NavigateAsync("MainRegion", viewName, parameters);

        if (result.Result)
        {
            CurrentViewName = viewName;
            NavigationStatus = $"已导航到 {viewName}";
        }
        else
        {
            NavigationStatus = $"导航到 {viewName} 失败：{Describe(result.Exception)}";
        }
    }

    /// <summary>
    /// 展开异常链，避免只看到最外层的容器包装信息。
    /// </summary>
    private static string Describe(Exception? exception)
    {
        if (exception is null)
        {
            return "目标区域不存在";
        }

        var messages = new List<string>();
        for (var current = exception; current is not null; current = current.InnerException)
        {
            messages.Add(current.Message);
        }

        return string.Join(" → ", messages);
    }
}
