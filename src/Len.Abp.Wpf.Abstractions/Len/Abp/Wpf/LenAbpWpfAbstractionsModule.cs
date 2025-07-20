using Len.Abp.Core;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace Len.Abp.Wpf;

[DependsOn(typeof(LenAbpCoreModule))]
public class LenAbpWpfAbstractionsModule : AbpModule
{
    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        Navigation.Region.ServiceProvider = context.ServiceProvider.GetRequiredService<IAbpApplication>().ServiceProvider;
    }
}