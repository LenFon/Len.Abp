using Len.Abp.Wpf.Router;
using System.Collections.Generic;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;
using WpfApp.Views;

namespace WpfApp;

[DependsOn(typeof(AbpAutofacModule))]
[DependsOn(typeof(LenAbpWpfRouterModule))]
public class WpfAppModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        Configure<RouterOptions>(options =>
        {
            options.Routes = new List<Route>
            {
                new()
                {
                    Path = "/",
                    Components = new()
                    {
                        { "HeaderRouter", typeof(HeaderView) },
                        { "ContentRouter", typeof(ContentView) },
                        { "FooterRouter", typeof(FooterView) }
                    }
                }
            };
        });
    }
}
