using Len.Abp.Wpf;
using System.Windows;
using Volo.Abp.Modularity;
using WpfApp.Controls;

namespace WpfApp;

/// <summary>
/// 示例应用的 WPF 模块，负责合并默认资源字典。
/// </summary>
/// <remarks>
/// 依赖 <see cref="WpfAppControlsModule"/>，由它把自定义控件的默认样式合并进应用资源，
/// 因此本模块只需关心自己的主题资源。
/// </remarks>
[DependsOn(typeof(LenAbpWpfAbstractionsModule))]
[DependsOn(typeof(WpfAppControlsModule))]
public class WpfAppModule : AbpWpfModule
{
    /// <inheritdoc/>
    public override void ConfigureResources(ICollection<ResourceDictionary> resources)
    {
        resources.Add(new UriResourceDictionary("pack://application:,,,/WpfApp;component/Default.xaml"));
    }
}
