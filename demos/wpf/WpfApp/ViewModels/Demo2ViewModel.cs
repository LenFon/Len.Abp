using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Len.Abp.Wpf.Navigation;
using Len.Abp.Wpf.Navigation.Regions;
using Volo.Abp.DependencyInjection;

namespace WpfApp.ViewModels;

/// <summary>
/// 演示视图模型，展示被导航到的目标视图。
/// </summary>
public partial class Demo2ViewModel : ObservableObject, ITransientDependency, INavigationAware, IRegionMemberLifetime
{
    /// <inheritdoc/>
    public bool KeepAlive => false;

    /// <summary>
    /// 获取或设置显示的消息。
    /// </summary>
    [ObservableProperty]
    public partial string Message { get; set; } = "Hello, World! Demo2.";

    /// <inheritdoc/>
    public Task<bool> IsNavigationTargetAsync(INavigationContext navigationContext)
    {
        return Task.FromResult(true);
    }

    /// <inheritdoc/>
    public Task OnNavigatedFromAsync(INavigationContext navigationContext)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task OnNavigatedToAsync(INavigationContext navigationContext)
    {
        return Task.CompletedTask;
    }

    [RelayCommand]
    private void Click()
    {
        Message = "Demo2 Button Clicked!";
    }
}
