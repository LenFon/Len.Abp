using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Len.Abp.Wpf.Navigation;
using Len.Abp.Wpf.Navigation.Regions;
using Volo.Abp.DependencyInjection;

namespace WpfApp.ViewModels;

public partial class Demo2ViewModel : ObservableObject, ITransientDependency, INavigationAware, IRegionMemberLifetime
{
    public bool KeepAlive => false;

    [ObservableProperty]
    public partial string Message { get; set; } = "Hello, World! Demo2.";

    public Task<bool> IsNavigationTargetAsync(INavigationContext navigationContext)
    {
        return Task.FromResult(true);
    }

    public Task OnNavigatedFromAsync(INavigationContext navigationContext)
    {
        return Task.CompletedTask;
    }

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