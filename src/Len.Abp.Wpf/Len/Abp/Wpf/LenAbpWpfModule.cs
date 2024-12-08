using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Modularity;

namespace Len.Abp.Wpf;

[DependsOn(typeof(LenAbpWpfAbstractionsModule))]
[DependsOn(typeof(AbpBackgroundWorkersModule))]
public class LenAbpWpfModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddOptions<WpfOptions>();
    }

    public override async Task OnApplicationInitializationAsync(ApplicationInitializationContext context)
    {
        await context.AddBackgroundWorkerAsync<MutexWpfBackgroundWorker>();
        await context.AddBackgroundWorkerAsync<WpfBackgroundWorker>();
    }
}
