using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace Len.Abp.Core;

public class LenAbpCoreModule : AbpModule
{
    public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
    {
        EnumHelper.ServiceProvider = context.ServiceProvider.GetRequiredService<IAbpApplication>().ServiceProvider;
    }
}