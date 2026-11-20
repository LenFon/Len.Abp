using Len.Abp.Core;
using Len.Abp.Wpf.Navigation.Regions;
using System.ComponentModel;
using System.Windows;

namespace Len.Abp.Wpf.Navigation;

/// <summary>
/// 将控件标记为导航区域宿主的附加属性。
/// </summary>
/// <remarks>
/// 类名带 <c>Host</c> 后缀以区别于运行时区域对象 <c>Len.Abp.Wpf.Navigation.Regions.Region</c>：
/// 前者是 XAML 里声明区域宿主的附加属性，后者是运行时的区域对象。
/// </remarks>
public static class RegionHost
{
    /// <summary>
    /// 标识区域名称的附加属性。
    /// </summary>
    public static readonly DependencyProperty NameProperty =
        DependencyProperty.RegisterAttached("Name", typeof(string), typeof(RegionHost), new PropertyMetadata(null, OnNameChanged));

    /// <summary>
    /// 标识视图在 <see cref="System.Windows.Controls.TabControl"/> 区域中显示标题的附加属性。
    /// </summary>
    /// <remarks>
    /// 未设置时回退到视图的 <c>ViewName</c>。视图名通常是导航用的标识（如 <c>lifetime-scoped</c>），
    /// 不适合直接展示给用户，需要可读标题时在视图上设置本属性。
    /// </remarks>
    public static readonly DependencyProperty TabHeaderProperty =
        DependencyProperty.RegisterAttached("TabHeader", typeof(object), typeof(RegionHost), new PropertyMetadata(null));

    /// <summary>
    /// 获取视图的选项卡标题。
    /// </summary>
    /// <param name="view">视图元素。</param>
    /// <returns>设置的标题；未设置时返回 <see langword="null"/>。</returns>
    public static object? GetTabHeader(DependencyObject view)
    {
        ArgumentNullException.ThrowIfNull(view);

        return view.GetValue(TabHeaderProperty);
    }

    /// <summary>
    /// 设置视图的选项卡标题。
    /// </summary>
    /// <param name="view">视图元素。</param>
    /// <param name="header">标题内容。</param>
    public static void SetTabHeader(DependencyObject view, object? header)
    {
        ArgumentNullException.ThrowIfNull(view);

        view.SetValue(TabHeaderProperty, header);
    }

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
