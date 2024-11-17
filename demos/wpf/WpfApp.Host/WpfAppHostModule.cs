using Len.Abp.Wpf;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace WpfApp.Host;

[DependsOn(typeof(AbpAutofacModule))]
[DependsOn(typeof(LenAbpWpfModule))]
[DependsOn(typeof(WpfAppModule))]
public class WpfAppHostModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<WpfOptions>(options =>
        {
            options.SingleInstance = true;
        });
    }
}
