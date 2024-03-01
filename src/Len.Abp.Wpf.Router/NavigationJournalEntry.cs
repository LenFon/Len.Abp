namespace Len.Abp.Wpf.Router;

/// <summary>
/// Represents a navigation journal entry.
/// </summary>
/// <param name="path"></param>
/// <param name="parameters"></param>
internal class NavigationJournalEntry(string path, IParameters parameters) : INavigationJournalEntry
{
    ///<inheritdoc/>
    public IParameters Parameters { get; } = parameters;

    ///<inheritdoc/>
    public string Path { get; } = path;
}