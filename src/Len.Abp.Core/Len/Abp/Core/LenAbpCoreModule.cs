using Volo.Abp;
using Volo.Abp.Modularity;

namespace Len.Abp.Core;

public class LenAbpCoreModule : AbpModule
{
    public override void OnApplicationShutdown(ApplicationShutdownContext context)
    {
        EnumHelper.CloseServiceProvider();
    }

    public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
    {
        EnumHelper.SetServiceProvider(context.ServiceProvider);
    }
}