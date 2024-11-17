using Len.Abp.Wpf.Abstractions;
using Volo.Abp.Modularity;

namespace WpfApp;

[DependsOn(typeof(LenAbpWpfAbstractionsModule))]
public class WpfAppModule : AbpModule
{
}