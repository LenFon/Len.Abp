using Len.Abp.Wpf;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace WpfApp.Host;

/// <summary>
/// 示例应用的宿主模块，配置 WPF 运行选项。
/// </summary>
[DependsOn(typeof(AbpAutofacModule))]
[DependsOn(typeof(LenAbpWpfModule))]
[DependsOn(typeof(WpfAppModule))]
public class WpfAppHostModule : AbpModule
{
    /// <summary>
    /// 配置服务，启用单实例运行。
    /// </summary>
    /// <param name="context">服务配置上下文。</param>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<WpfOptions>(options =>
        {
            options.SingleInstance = true;
        });
    }
}
