using System.Windows;
using Len.Abp.Wpf.Mvvm;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace Len.Abp.Wpf.Router;

[DependsOn(typeof(LenAbpWpfMvvmModule))]
public class LenAbpWpfRouterModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton(sp =>
        {
            var type = AppDomain
                .CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .FirstOrDefault(w => w.IsAssignableTo<IRouterShell>());

            if (type is not null && sp.GetRequiredService(type) is IRouterShell routerShell and Window)
            {
                return routerShell;
            }

            throw new AbpException("The router shell parsing exception.");
        });
    }

    public override void OnPostApplicationInitialization(ApplicationInitializationContext context)
    {
        context.ServiceProvider.GetRequiredService<IRouterService>().NavigateTo("/");
    }
}