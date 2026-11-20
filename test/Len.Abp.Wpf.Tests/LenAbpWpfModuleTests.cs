using Len.Abp.Wpf.Navigation.Regions;
using Len.Abp.Wpf.Navigation.Regions.Behaviors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Modularity;
using Xunit;

namespace Len.Abp.Wpf.Tests;

/// <summary>
/// 覆盖 <see cref="LenAbpWpfModule"/> 的选项注册与行为键收集。
/// </summary>
/// <remarks>
/// 行为键在 <c>PostConfigureServices</c> 中通过扫描服务描述符收集，
/// 因此这里先模拟 ABP 的程序集扫描（<c>AddAssembly</c>），再调用 <c>PostConfigureServices</c>。
/// </remarks>
[Collection(GlobalServiceProviderCollection.Name)]
public class LenAbpWpfModuleTests
{
    /// <summary>
    /// 配置服务时把以键控方式注册的 <see cref="IRegionBehavior"/> 登记为默认行为键。
    /// </summary>
    [Fact]
    public void PostConfigureServices_CollectsRegionBehaviorKeys()
    {
        var services = new ServiceCollection();
        var context = new ServiceConfigurationContext(services);

        new LenAbpWpfModule().PreConfigureServices(context);

        // 复现 ABP 在 ConfigureServices 阶段对模块程序集的扫描。
        services.AddAssembly(typeof(LenAbpWpfModule).Assembly);

        new LenAbpWpfModule().PostConfigureServices(context);

        var options = services.BuildServiceProvider().GetRequiredService<IOptions<RegionOptions>>().Value;

        Assert.Contains(nameof(RegionProviderRegistrationBehavior), options.BehaviorKeys);
        Assert.Contains(nameof(RegionMemberLifetimeBehavior), options.BehaviorKeys);
    }

    /// <summary>
    /// 行为键收集不依赖 Autofac 的注册回调，默认 DI 容器下同样生效。
    /// </summary>
    [Fact]
    public void PostConfigureServices_WorksWithoutRegistrationCallbacks()
    {
        var services = new ServiceCollection();
        var context = new ServiceConfigurationContext(services);

        new LenAbpWpfModule().PreConfigureServices(context);
        services.AddAssembly(typeof(LenAbpWpfModule).Assembly);
        new LenAbpWpfModule().PostConfigureServices(context);

        // 默认容器不会触发 OnRegistered，这里断言键仍然被收集到。
        var registrationActions = Microsoft.Extensions.DependencyInjection
            .ServiceCollectionRegistrationActionExtensions.GetRegistrationActionList(services);

        Assert.Empty(registrationActions);

        var options = services.BuildServiceProvider().GetRequiredService<IOptions<RegionOptions>>().Value;
        Assert.Equal(2, options.BehaviorKeys.Count());
    }

    /// <summary>
    /// 没有区域行为注册时不产生任何行为键。
    /// </summary>
    [Fact]
    public void PostConfigureServices_WithoutBehaviors_AddsNoKeys()
    {
        var services = new ServiceCollection();
        var context = new ServiceConfigurationContext(services);

        new LenAbpWpfModule().PreConfigureServices(context);
        new LenAbpWpfModule().PostConfigureServices(context);

        var options = services.BuildServiceProvider().GetRequiredService<IOptions<RegionOptions>>().Value;

        Assert.Empty(options.BehaviorKeys);
    }

    /// <summary>
    /// 配置服务时注册 <see cref="WpfOptions"/>。
    /// </summary>
    [Fact]
    public void PreConfigureServices_RegistersWpfOptions()
    {
        var services = new ServiceCollection();
        var context = new ServiceConfigurationContext(services);

        new LenAbpWpfModule().PreConfigureServices(context);

        Assert.NotNull(services.BuildServiceProvider().GetService<IOptions<WpfOptions>>());
    }

    /// <summary>
    /// 单实例通道注册为单例。
    /// </summary>
    /// <remarks>
    /// 通道持有进程级内核对象（互斥量判定主实例、事件传递激活请求）。
    /// 注册成 Transient 时，工作器在初始化作用域里解析出的实例会随该作用域一起被释放，
    /// 互斥量提前消失导致单实例失效，关闭时再次释放还会抛 <see cref="ObjectDisposedException"/>。
    /// </remarks>
    [Fact]
    public void PreConfigureServices_RegistersActivationChannelAsSingleton()
    {
        var services = new ServiceCollection();
        var context = new ServiceConfigurationContext(services);

        new LenAbpWpfModule().PreConfigureServices(context);

        var descriptor = services.Single(d =>
            d.ServiceType == typeof(IInstanceActivationChannel)
        );

        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    /// <summary>
    /// 程序集扫描后 <see cref="IUiDispatcher"/> 接口可被解析。
    /// </summary>
    /// <remarks>
    /// <see cref="IUiDispatcher"/> 是 internal 接口，ABP 的约定注册只为公开接口建立映射。
    /// 缺少 <c>[ExposeServices(typeof(IUiDispatcher))]</c> 时容器里只有具体类型，
    /// 导航服务解析接口会失败——这个缺陷只在真实容器里暴露，手工 new 的测试看不到。
    /// </remarks>
    [Fact]
    public void AssemblyScan_RegistersUiDispatcherInterface()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(new WpfContext());
        services.AddAssembly(typeof(LenAbpWpfModule).Assembly);

        var provider = services.BuildServiceProvider();

        Assert.IsType<WpfDispatcher>(provider.GetRequiredService<IUiDispatcher>());
    }

    /// <summary>
    /// 应用初始化时依次启动单实例检测与 WPF UI 线程两个后台工作器。
    /// </summary>
    /// <remarks>
    /// 顺序有语义：单实例检测必须先完成，否则第二个实例会先创建 UI 线程再退出，
    /// 用户会看到窗口闪一下。
    /// </remarks>
    [Fact]
    public async Task OnApplicationInitializationAsync_AddsWorkersInOrder()
    {
        var manager = new RecordingBackgroundWorkerManager();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IBackgroundWorkerManager>(manager);

        // AddBackgroundWorkerAsync 从容器解析工作器实例，因此必须先注册。
        services.AddTransient<SingleInstanceWpfBackgroundWorker>();
        services.AddTransient<WpfBackgroundWorker>();

        var provider = services.BuildServiceProvider();
        var context = new ApplicationInitializationContext(provider);

        await new LenAbpWpfModule().OnApplicationInitializationAsync(context);

        Assert.Equal(
            [typeof(SingleInstanceWpfBackgroundWorker), typeof(WpfBackgroundWorker)],
            manager.AddedTypes
        );
    }

    /// <summary>
    /// 单实例通道由宿主环境的应用名构造，且同一容器内始终返回同一实例。
    /// </summary>
    [Fact]
    public void PreConfigureServices_ActivationChannel_UsesApplicationNameAndIsSingleton()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IHostEnvironment>(new FakeHostEnvironment("channel-app"));
        var context = new ServiceConfigurationContext(services);

        new LenAbpWpfModule().PreConfigureServices(context);

        var provider = services.BuildServiceProvider();
        var first = provider.GetRequiredService<IInstanceActivationChannel>();
        var second = provider.GetRequiredService<IInstanceActivationChannel>();

        Assert.IsType<NamedInstanceActivationChannel>(first);
        Assert.Same(first, second);
    }

    /// <summary>
    /// 记录被添加的后台工作器类型。
    /// </summary>
    private sealed class RecordingBackgroundWorkerManager : IBackgroundWorkerManager
    {
        public List<Type> AddedTypes { get; } = [];

        public Task AddAsync(IBackgroundWorker worker, CancellationToken cancellationToken = default)
        {
            AddedTypes.Add(worker.GetType());
            return Task.CompletedTask;
        }

        public Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    [DependsOn(typeof(LenAbpWpfModule))]
    public sealed class WpfModule : AbpModule;
}
