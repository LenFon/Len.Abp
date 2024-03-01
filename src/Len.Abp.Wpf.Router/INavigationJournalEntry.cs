namespace Len.Abp.Wpf.Router;

/// <summary>
/// Represents a navigation journal entry.
/// </summary>
public interface INavigationJournalEntry
{
    /// <summary>
    /// Gets the navigation parameters.
    /// </summary>
    IParameters Parameters { get; }

    /// <summary>
    /// Gets the path of the target view.
    /// </summary>
    string Path { get; }
}