using Autofac;
using Len.Abp.Wpf.Core;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace Len.Abp.Wpf.Mvvm;

[DependsOn(typeof(LenAbpWpfCoreModule))]
public class LenAbpWpfMvvmModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        var builder = context.Services.GetContainerBuilder();

        builder.ComponentRegistryBuilder.Registered += (sender, e) =>
        {
            e.ComponentRegistration.PipelineBuilding += (sender, e) =>
            {
                e.Use(new ViewAndViewModelResolveMiddleware());
            };
        };
    }
}
