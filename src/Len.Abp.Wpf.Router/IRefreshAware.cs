namespace Len.Abp.Wpf.Router;

/// <summary>
/// Interface for views that need to refresh when navigated to.
/// </summary>
public interface IRefreshAware
{
    /// <summary>
    /// Refresh the view
    /// </summary>
    /// <param name="context">The navigation context.</param>
    void OnRefresh(INavigationContext context);
}