namespace Len.Abp.Wpf.Router;

/// <summary>
/// Options for the router
/// </summary>
public class RouterOptions
{
    /// <summary>
    /// Action to be executed after each navigation
    /// </summary>
    public Action<INavigationContext>? AfterEach { get; set; }

    /// <summary>
    /// Action to be executed before each navigation
    /// </summary>
    public Action<INavigationContext, Action<string>> BeforeEach { get; set; } =
        (context, next) =>
        {
            next(context.To);
        };

    /// <summary>
    /// Routes to be registered
    /// </summary>
    public required ICollection<Route> Routes { get; set; }
}
