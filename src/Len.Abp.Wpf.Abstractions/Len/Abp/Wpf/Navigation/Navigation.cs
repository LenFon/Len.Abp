using Len.Abp.Wpf.Navigation.Regions;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Windows;

namespace Len.Abp.Wpf.Navigation;

public class Navigation
{
    public static readonly DependencyProperty RegionNameProperty =
        DependencyProperty.RegisterAttached("RegionName", typeof(string), typeof(Navigation), new PropertyMetadata(null, OnRegionNameChanged));

    internal static IServiceProvider ServiceProvider { get; set; } = default!;

    public static string GetRegionName(DependencyObject regionTarget)
    {
        ArgumentNullException.ThrowIfNull(regionTarget);

        return (string)regionTarget.GetValue(RegionNameProperty);
    }

    public static void SetRegionName(DependencyObject regionTarget, string name)
    {
        ArgumentNullException.ThrowIfNull(regionTarget);

        regionTarget.SetValue(RegionNameProperty, name);
    }

    private static void CreateRegion(DependencyObject element)
    {
        var builder = ServiceProvider.GetRequiredService<IRegionBulider>();

        builder.Bulid(element);
    }

    private static void OnRegionNameChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
    {
        if (!DesignerProperties.GetIsInDesignMode(element))
        {
            CreateRegion(element);
        }
    }
}