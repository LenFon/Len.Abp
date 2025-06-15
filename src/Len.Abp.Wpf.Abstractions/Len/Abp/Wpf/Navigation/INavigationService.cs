namespace Len.Abp.Wpf.Navigation;

public interface INavigationService
{
    ValueTask<INavigationResult> NavigateAsync(string regionName, string viewName, IParameters navigationParameters);
}