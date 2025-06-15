using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Len.Abp.Wpf;
using Len.Abp.Wpf.Navigation;
using Len.Abp.Wpf.Navigation.Regions;
using Volo.Abp.DependencyInjection;

namespace WpfApp.ViewModels;

public partial class DemoViewModel : ObservableObject, INavigationAware, ITransientDependency, IRegionMemberLifetime
{
    [ObservableProperty]
    private string _message = "Hello, World!";

    public bool KeepAlive => false;

    public IAbpLazyServiceProvider LazyServiceProvider { get; set; } = default!;

    protected INavigationService NavigationService => LazyServiceProvider.LazyGetRequiredService<INavigationService>();

    public Task<bool> IsNavigationTargetAsync(INavigationContext navigationContext) => Task.FromResult(true);

    public Task OnNavigatedFromAsync(INavigationContext navigationContext)
    {
        return Task.CompletedTask;
    }

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