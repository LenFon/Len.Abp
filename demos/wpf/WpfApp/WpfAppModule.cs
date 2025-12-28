using Len.Abp.Wpf;
using System.Windows;
using Volo.Abp.Modularity;

namespace WpfApp;

[DependsOn(typeof(LenAbpWpfAbstractionsModule))]
public class WpfAppModule : AbpWpfModule
{
    public override void ConfigureResources(ICollection<ResourceDictionary> resources)
    {
        resources.Add(new UriResourceDictionary("pack://application:,,,/WpfApp;component/Default.xaml"));
    }
}