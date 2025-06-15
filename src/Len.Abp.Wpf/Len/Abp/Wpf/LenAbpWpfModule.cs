using Len.Abp.Wpf.Navigation.Regions;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Modularity;

namespace Len.Abp.Wpf;

[DependsOn(typeof(LenAbpWpfAbstractionsModule))]
[DependsOn(typeof(AbpBackgroundWorkersModule))]
public class LenAbpWpfModule : AbpModule
{
    public override async Task OnApplicationInitializationAsync(ApplicationInitializationContext context)
    {
        await context.AddBackgroundWorkerAsync<MutexWpfBackgroundWorker>();
        await context.AddBackgroundWorkerAsync<WpfBackgroundWorker>();
    }

    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddOptions<WpfOptions>();

        var keys = new List<string>();
        context.Services.OnRegistered(c =>
        {
            if (c.ImplementationType.IsAssignableTo<IRegionBehavior>())
            {
                keys.Add(c.ImplementationType.Name);
            }
        });

        context.Services.Configure<RegionOptions>(options =>
        {
            foreach (var key in keys)
            {
                options.AddBehaviorKey(key);
            }
        });
    }
}