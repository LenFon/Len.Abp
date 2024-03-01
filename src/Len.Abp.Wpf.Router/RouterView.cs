using System.Windows;
using System.Windows.Controls;

namespace Len.Abp.Wpf.Router;

/// <summary>
/// A control that represents a view in the router.
/// </summary>
public class RouterView : ContentControl
{
    public static readonly DependencyProperty RouterNameProperty = DependencyProperty.Register(
        nameof(RouterName),
        typeof(string),
        typeof(RouterView),
        new PropertyMetadata(DefaultRouterViewName)
    );

    private const string _defaultRouterViewName = nameof(DefaultRouterViewName);

    /// <summary>
    /// The name of the router view.
    /// </summary>
    public string RouterName
    {
        get { return (string)GetValue(RouterNameProperty); }
        set { SetValue(RouterNameProperty, value); }
    }

    internal static string DefaultRouterViewName => _defaultRouterViewName;
}