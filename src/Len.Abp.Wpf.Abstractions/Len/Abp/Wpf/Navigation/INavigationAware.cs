namespace Len.Abp.Wpf.Navigation;

public interface INavigationAware
{
    Task<bool> IsNavigationTargetAsync(INavigationContext navigationContext);

    Task OnNavigatedFromAsync(INavigationContext navigationContext);

    Task OnNavigatedToAsync(INavigationContext navigationContext);
}