using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Len.Abp.Wpf;
using Len.Abp.Wpf.Navigation;
using Volo.Abp.DependencyInjection;

namespace MinimalApp.ViewModels;

/// <summary>
/// 主窗口视图模型，负责在区域中切换视图。
/// </summary>
/// <remarks>
/// Shell 视图是单例，视图模型若声明为 Scoped 会形成 captive dependency，因此用瞬态。
/// </remarks>
public partial class MainViewModel(INavigationService navigationService) : ObservableObject, ITransientDependency
{
    /// <summary>
    /// 获取或设置最近一次操作的结果文本。
    /// </summary>
    [ObservableProperty]
    public partial string Status { get; set; } = "就绪";

    /// <summary>
    /// 导航到 Hello 视图。
    /// </summary>
    [RelayCommand]
    private Task ShowHelloAsync() => NavigateAsync("hello", IParameters.Create());

    /// <summary>
    /// 导航到 Goodbye 视图。
    /// </summary>
    [RelayCommand]
    private Task ShowGoodbyeAsync() => NavigateAsync("goodbye", IParameters.Create());

    /// <summary>
    /// 回退到上一个视图。
    /// </summary>
    /// <remarks>
    /// 导航历史按区域独立记录，回退不产生新的历史条目。
    /// </remarks>
    [RelayCommand]
    private async Task GoBackAsync()
    {
        var result = await navigationService.GoBackAsync("MainRegion");

        Status = result.Result ? $"已回退到 {result.Context.Uri.OriginalString}" : "没有更早的历史";
    }

    /// <summary>
    /// 导航到声明了必需参数的视图，演示参数校验。
    /// </summary>
    /// <param name="withParameters">是否提供必需参数。</param>
    [RelayCommand]
    private async Task OpenGreetingAsync(bool withParameters)
    {
        var parameters = withParameters
            ? IParameters.Create("?name=Len")
            : IParameters.Create();

        var result = await navigationService.NavigateAsync("MainRegion", "greeting", parameters);

        Status = result.Result
            ? "参数校验通过，已导航到 greeting"
            : $"参数校验失败：{result.Exception?.Message ?? "目标区域不存在"}";
    }

    private async Task NavigateAsync(string viewName, IParameters parameters)
    {
        var result = await navigationService.NavigateAsync("MainRegion", viewName, parameters);

        Status = result.Result ? $"已导航到 {viewName}" : $"导航失败：{result.Exception?.Message ?? "目标区域不存在"}";
    }
}
