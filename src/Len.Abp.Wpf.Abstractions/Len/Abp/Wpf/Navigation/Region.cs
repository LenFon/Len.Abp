using Len.Abp.Wpf.Navigation.Regions;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Windows;

namespace Len.Abp.Wpf.Navigation;

public static class Region
{
    public static readonly DependencyProperty NameProperty =
        DependencyProperty.RegisterAttached("Name", typeof(string), typeof(Region), new PropertyMetadata(null, OnNameChanged));

    internal static IServiceProvider ServiceProvider { get; set; } = default!;

    public static string GetName(DependencyObject regionTarget)
    {
        ArgumentNullException.ThrowIfNull(regionTarget);

        return (string)regionTarget.GetValue(NameProperty);
    }

    public static void SetName(DependencyObject regionTarget, string name)
    {
        ArgumentNullException.ThrowIfNull(regionTarget);

        regionTarget.SetValue(NameProperty, name);
    }

    private static void CreateRegion(DependencyObject element)
    {
        var builder = ServiceProvider.GetRequiredService<IRegionBulider>();

        builder.Bulid(element);
    }

    private static void OnNameChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
    {
        if (!DesignerProperties.GetIsInDesignMode(element))
        {
            CreateRegion(element);
        }
    }
}