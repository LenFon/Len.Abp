namespace Len.Abp.Wpf.Router;

/// <summary>
/// Provides methods to navigate between pages in the application.
/// </summary>
public interface IRouterService
{
    /// <summary>
    /// Gets a value indicating whether there is at least one entry in the back navigation history.
    /// </summary>
    bool CanGoBack { get; }

    /// <summary>
    /// Gets a value indicating whether there is at least one entry in the forward navigation history.
    /// </summary>
    bool CanGoForward { get; }

    /// <summary>
    /// Gets the current navigation entry.
    /// </summary>
    INavigationJournalEntry? CurrentEntry { get; }

    /// <summary>
    /// Navigate to the previous page in the navigation history.
    /// </summary>
    bool GoBack();

    /// <summary>
    /// Navigate to the next page in the navigation history.
    /// </summary>
    bool GoForward();

    /// <summary>
    /// Navigate to the specified page.
    /// </summary>
    void NavigateTo(string url);

    /// <summary>
    /// Navigate to the specified page with the specified parameters.
    /// </summary>
    void NavigateTo(string url, IParameters parameters);
}