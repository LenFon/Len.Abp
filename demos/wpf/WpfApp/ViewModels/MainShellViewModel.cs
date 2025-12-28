using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Len.Abp.Wpf;
using Len.Abp.Wpf.Navigation;
using Volo.Abp.DependencyInjection;

namespace WpfApp.ViewModels;

public partial class MainShellViewModel : ObservableObject, IScopedDependency
{
    private bool _state = false;

    public IAbpLazyServiceProvider LazyServiceProvider { get; set; } = default!;

    [ObservableProperty]
    public partial string Title { get; set; } = "Main Shell";

    protected INavigationService NavigationService => LazyServiceProvider.LazyGetRequiredService<INavigationService>();

    [RelayCommand]
    private async Task ChangeTitleAsync()
    {
        Title = _state ? "Main Shell" : "Main Shell Window";
        _state = !_state;

        if (_state)
        {
            await NavigationService.NavigateAsync("MainRegion", "login", IParameters.Create());
        }
        else
        {
            await NavigationService.NavigateAsync("MainRegion", "demo2", IParameters.Create());
        }
    }
}