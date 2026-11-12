using Len.Abp.Wpf.Navigation.Regions;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Modularity;

namespace Len.Abp.Wpf;

/// <summary>
/// WPF 宿主模块，负责启动 UI 线程、单实例检测以及区域行为的自动注册。
/// </summary>
[DependsOn(typeof(LenAbpWpfAbstractionsModule))]
[DependsOn(typeof(AbpBackgroundWorkersModule))]
public class LenAbpWpfModule : AbpModule
{
    /// <summary>
    /// 在应用初始化时启动单实例检测与 WPF UI 线程后台工作器。
    /// </summary>
    /// <param name="context">应用初始化上下文。</param>
    public override async Task OnApplicationInitializationAsync(ApplicationInitializationContext context)
    {
        await context.AddBackgroundWorkerAsync<SingleInstanceWpfBackgroundWorker>();
        await context.AddBackgroundWorkerAsync<WpfBackgroundWorker>();
    }

    /// <summary>
    /// 注册 WPF 选项。
    /// </summary>
    /// <param name="context">服务配置上下文。</param>
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddOptions<WpfOptions>();
    }

    /// <summary>
    /// 收集所有以键控方式注册的区域行为，并登记到 <see cref="RegionOptions"/>。
    /// </summary>
    /// <remarks>
    /// 这里直接扫描服务描述符，而不是依赖 <c>OnRegistered</c> 回调：后者只在 Autofac 集成中触发，
    /// 使用默认 DI 容器时回调不会执行，区域行为会全部丢失，导航随之静默失效。
    /// <c>PostConfigureServices</c> 在所有模块的程序集扫描之后执行，此时描述符已经齐备。
    /// </remarks>
    /// <param name="context">服务配置上下文。</param>
    public override void PostConfigureServices(ServiceConfigurationContext context)
    {
        var keys = context
            .Services.Where(descriptor =>
                descriptor.ServiceType == typeof(IRegionBehavior) && descriptor.ServiceKey is string
            )
            .Select(descriptor => (string)descriptor.ServiceKey!)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (keys.Length == 0)
        {
            return;
        }

        context.Services.Configure<RegionOptions>(options =>
        {
            foreach (var key in keys)
            {
                options.AddBehaviorKey(key);
            }
        });
    }
}
