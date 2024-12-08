using Len.Abp.Wpf;
using Volo.Abp.Modularity;

namespace WpfApp;

[DependsOn(typeof(LenAbpWpfAbstractionsModule))]
public class WpfAppModule : AbpModule
{
}