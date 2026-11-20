using Len.Abp.Wpf.Navigation;
using Len.Abp.Wpf.Navigation.Regions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Volo.Abp;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.DependencyInjection;
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
    /// 为后台工作器补齐延迟服务提供程序。
    /// </summary>
    /// <param name="context">应用初始化上下文。</param>
    /// <remarks>
    /// <para>
    /// <c>BackgroundWorkerBase.LazyServiceProvider</c> 由 ABP 的属性注入填充，
    /// 而属性注入只有 Autofac 集成提供。使用默认 DI 容器时该属性为 null，
    /// 工作器一启动就在 <c>Logger</c> 上抛 <see cref="NullReferenceException"/>。
    /// </para>
    /// <para>
    /// 必须绑定根容器而不是 <c>context.ServiceProvider</c>：后者是初始化作用域，
    /// <c>InitializeModulesAsync</c> 结束时即被释放。工作器是单例，运行期仍会解析服务
    /// （例如单实例激活回调里的 <see cref="ShellActivator"/>），绑定到已释放的作用域会让每次解析都抛
    /// <see cref="ObjectDisposedException"/>。已解析过的服务因缓存仍可用，缺陷只在运行期首次解析时暴露。
    /// </para>
    /// </remarks>
    public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
    {
        var application = context.ServiceProvider.GetRequiredService<IAbpApplication>();
        var lazyServiceProvider = application.ServiceProvider.GetRequiredService<IAbpLazyServiceProvider>();

        foreach (var worker in context.ServiceProvider.GetServices<IBackgroundWorker>())
        {
            if (worker is BackgroundWorkerBase backgroundWorker)
            {
                backgroundWorker.LazyServiceProvider = lazyServiceProvider;
            }
        }
    }

    /// <summary>
    /// 注册 WPF 选项与单实例通道。
    /// </summary>
    /// <param name="context">服务配置上下文。</param>
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddOptions<WpfOptions>();
        context.Services.AddOptions<NavigationOptions>();

        // 通道按应用名创建内核对象，名称来自宿主环境，因此用工厂注册。
        // 必须是单例：它持有进程级的内核对象（互斥量判定主实例、事件传递激活请求），
        // 生命周期与进程一致。注册成 Transient 时，工作器在初始化作用域里解析出的实例
        // 会随该作用域一起被释放，互斥量提前消失导致单实例失效，关闭时再次释放还会抛异常。
        context.Services.AddSingleton<IInstanceActivationChannel>(provider =>
            new NamedInstanceActivationChannel(
                provider.GetRequiredService<IHostEnvironment>().ApplicationName
            )
        );
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
