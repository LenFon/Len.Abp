using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Len.Abp.Wpf;
using Len.Abp.Wpf.Navigation;
using Len.Abp.Wpf.Navigation.Regions;
using Volo.Abp.DependencyInjection;

namespace WpfApp.ViewModels;

/// <summary>
/// 演示视图模型，展示如何通过导航服务切换到其他视图。
/// </summary>
public partial class DemoViewModel : ObservableObject, INavigationAware, ITransientDependency, IRegionMemberLifetime
{
    /// <summary>
    /// 获取或设置 ABP 延迟服务提供程序。
    /// </summary>
    public required IAbpLazyServiceProvider AbpLazyServiceProvider { get; init; }

    /// <inheritdoc/>
    public bool KeepAlive => false;

    /// <summary>
    /// 获取或设置显示的消息。
    /// </summary>
    [ObservableProperty]
    public partial string Message { get; set; } = "Hello, World!";

    /// <summary>
    /// 获取导航服务。
    /// </summary>
    protected INavigationService NavigationService => AbpLazyServiceProvider.LazyGetRequiredService<INavigationService>();

    /// <inheritdoc/>
    public Task<bool> IsNavigationTargetAsync(INavigationContext navigationContext) => Task.FromResult(true);

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
    private async Task ClickAsync()
    {
        Message = "Button Clicked!";

        await NavigationService.NavigateAsync("DemoRegion", "demo2", IParameters.Create());
    }
}
