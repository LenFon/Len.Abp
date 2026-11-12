using Len.Abp.Wpf;
using System.Windows;
using Volo.Abp.Modularity;

namespace WpfApp;

/// <summary>
/// 示例应用的 WPF 模块，负责合并默认资源字典。
/// </summary>
[DependsOn(typeof(LenAbpWpfAbstractionsModule))]
public class WpfAppModule : AbpWpfModule
{
    /// <inheritdoc/>
    public override void ConfigureResources(ICollection<ResourceDictionary> resources)
    {
        resources.Add(new UriResourceDictionary("pack://application:,,,/WpfApp;component/Default.xaml"));
    }
}
