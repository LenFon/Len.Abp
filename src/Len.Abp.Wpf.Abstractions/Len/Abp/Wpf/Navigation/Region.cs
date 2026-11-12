using Len.Abp.Core;
using Len.Abp.Wpf.Navigation.Regions;
using System.ComponentModel;
using System.Windows;

namespace Len.Abp.Wpf.Navigation;

/// <summary>
/// 将控件标记为导航区域宿主的附加属性。
/// </summary>
public static class Region
{
    /// <summary>
    /// 标识区域名称的附加属性。
    /// </summary>
    public static readonly DependencyProperty NameProperty =
        DependencyProperty.RegisterAttached("Name", typeof(string), typeof(Region), new PropertyMetadata(null, OnNameChanged));

    /// <summary>
    /// 获取控件的区域名称。
    /// </summary>
    /// <param name="regionTarget">区域宿主控件。</param>
    /// <returns>区域名称。</returns>
    public static string GetName(DependencyObject regionTarget)
    {
        ArgumentNullException.ThrowIfNull(regionTarget);

        return (string)regionTarget.GetValue(NameProperty);
    }

    /// <summary>
    /// 设置控件的区域名称。
    /// </summary>
    /// <param name="regionTarget">区域宿主控件。</param>
    /// <param name="name">区域名称。</param>
    public static void SetName(DependencyObject regionTarget, string name)
    {
        ArgumentNullException.ThrowIfNull(regionTarget);

        regionTarget.SetValue(NameProperty, name);
    }

    private static void CreateRegion(DependencyObject element)
    {
        var builder = AbpServiceProvider.GetRequiredService<IRegionBuilder>();

        builder.Build(element);
    }

    private static void OnNameChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
    {
        if (!DesignerProperties.GetIsInDesignMode(element))
        {
            CreateRegion(element);
        }
    }
}
