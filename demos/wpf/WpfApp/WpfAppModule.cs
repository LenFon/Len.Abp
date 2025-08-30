using Len.Abp.Wpf;
using System.Collections.ObjectModel;
using System.Windows;
using Volo.Abp.Modularity;

namespace WpfApp;

[DependsOn(typeof(LenAbpWpfAbstractionsModule))]
public class WpfAppModule : AbpWpfModule
{
    public override void ConfigureResources(Collection<ResourceDictionary> resources)
    {
        resources.Add(new UriResourceDictionary("pack://application:,,,/WpfApp;component/Default.xaml"));
    }
}