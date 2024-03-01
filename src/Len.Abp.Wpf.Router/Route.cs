namespace Len.Abp.Wpf.Router;

/// <summary>
/// Represents a route in the application.
/// </summary>
public class Route
{
    /// <summary>
    /// The children routes of the current route.
    /// </summary>
    public ICollection<Route> Children { get; init; } = new List<Route>();

    /// <summary>
    /// The component that can be used to render the route.
    /// </summary>
    public Type? Component { get; init; }

    /// <summary>
    /// The components that can be used to render the route.
    /// </summary>
    public Dictionary<string, Type> Components { get; init; } = [];

    /// <summary>
    /// The path of the route.
    /// </summary>
    public required string Path { get; init; }

    internal Dictionary<string, Type> GetComponents()
    {
        if (Component != null)
            Components.TryAdd(RouterView.DefaultRouterViewName, Component);

        return Components;
    }
}