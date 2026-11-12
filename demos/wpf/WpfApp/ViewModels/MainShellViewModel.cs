using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Len.Abp.Wpf;
using Len.Abp.Wpf.Navigation;
using Volo.Abp.DependencyInjection;

namespace WpfApp.ViewModels;

/// <summary>
/// 主窗口视图模型，演示在 Shell 中切换区域内容。
/// </summary>
/// <remarks>
/// Shell 视图是单例，视图模型若声明为 Scoped 会形成 captive dependency，
/// 因此这里使用瞬态生命周期。
/// </remarks>
public partial class MainShellViewModel(INavigationService navigationService) : ObservableObject, ITransientDependency
{
    private bool _state = false;

    /// <summary>
    /// 获取或设置窗口标题。
    /// </summary>
    [ObservableProperty]
    public partial string Title { get; set; } = "Main Shell";

    [RelayCommand]
    private async Task ChangeTitleAsync()
    {
        Title = _state ? "Main Shell" : "Main Shell Window";
        _state = !_state;

        if (_state)
        {
            await navigationService.NavigateAsync("MainRegion", "login", IParameters.Create());
        }
        else
        {
            await navigationService.NavigateAsync("MainRegion", "demo2", IParameters.Create());
        }
    }
}
