namespace Len.Abp.Wpf.Router;

/// <summary>
/// Encapsulates information about a navigation request.
/// </summary>
public interface INavigationContext
{
    /// <summary>
    /// Source path
    /// </summary>
    string? From { get; }

    /// <summary>
    /// Navigation parameters
    /// </summary>
    IParameters Parameters { get; }

    /// <summary>
    /// Target path
    /// </summary>
    string To { get; }
}