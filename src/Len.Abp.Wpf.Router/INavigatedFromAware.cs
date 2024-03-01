namespace Len.Abp.Wpf.Router;

/// <summary>
/// Interface for objects that need to be notified when they are being navigated away from.
/// </summary>
public interface INavigatedFromAware
{
    /// <summary>
    /// Called when the implementer is being navigated away from.
    /// </summary>
    /// <param name="context">The navigation context.</param>
    void OnNavigatedFrom(INavigationContext context);
}