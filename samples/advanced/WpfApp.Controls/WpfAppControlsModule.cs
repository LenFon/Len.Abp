using System.Windows;
using Volo.Abp.Modularity;

namespace WpfApp.Controls;

/// <summary>
/// 自定义控件库的 ABP 模块，负责把控件默认样式合并到 WPF 应用。
/// </summary>
/// <remarks>
/// 每个控件的样式独立成文件，由 <c>Themes/Default.xaml</c> 统一合并；
/// 本模块只加载这一个入口文件，新增控件时只需改 Default，不用改模块。
/// </remarks>
[DependsOn(typeof(Len.Abp.Wpf.LenAbpWpfAbstractionsModule))]
public class WpfAppControlsModule : AbpWpfModule
{
    /// <inheritdoc/>
    public override void ConfigureResources(ICollection<ResourceDictionary> resources)
    {
        resources.Add(new UriResourceDictionary("pack://application:,,,/WpfApp.Controls;component/Themes/Default.xaml"));
    }
}
