namespace Len.Abp.Wpf.Router;

/// <summary>
/// Interface for objects that need to be notified when they have been navigated to.
/// </summary>
public interface INavigatedToAware
{
    /// <summary>
    /// Called when the implementer has been navigated to.
    /// </summary>
    /// <param name="context">The navigation context.</param>
    void OnNavigatedTo(INavigationContext context);
}