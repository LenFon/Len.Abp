namespace Len.Abp.Wpf.Router;

/// <summary>
/// Encapsulates information about a navigation request.
/// </summary>
/// <param name="parameters"></param>
/// <param name="from"></param>
/// <param name="to"></param>
internal class NavigationContext(IParameters parameters, string? from, string to) : INavigationContext
{
    /// <inheritdoc/>
    public string? From { get; } = from;

    /// <inheritdoc/>
    public IParameters Parameters { get; } = parameters;

    /// <inheritdoc/>
    public string To { get; } = to;
}