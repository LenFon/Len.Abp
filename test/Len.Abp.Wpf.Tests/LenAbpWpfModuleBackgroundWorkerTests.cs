using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Modularity;
using Xunit;

namespace Len.Abp.Wpf.Tests;

/// <summary>
/// 验证 <see cref="LenAbpWpfModule"/> 在默认 DI 容器下也能启动后台工作器。
/// </summary>
/// <remarks>
/// <c>BackgroundWorkerBase.LazyServiceProvider</c> 由 ABP 的属性注入填充，
/// 而属性注入只有 Autofac 集成提供。使用默认容器时该属性为 null，
/// 工作器一启动就在 <c>Logger</c> 上抛 <see cref="NullReferenceException"/>，
/// 表现为宿主启动失败。这里直接验证模块补齐了该属性。
/// </remarks>
[Collection(GlobalServiceProviderCollection.Name)]
public class LenAbpWpfModuleBackgroundWorkerTests
{
    /// <summary>
    /// 应用初始化前为已注册的后台工作器补齐延迟服务提供程序。
    /// </summary>
    [Fact]
    public void OnPreApplicationInitialization_AssignsLazyServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IAbpLazyServiceProvider>(provider =>
            new AbpLazyServiceProvider(provider)
        );

        var worker = new ProbeBackgroundWorker();
        services.AddSingleton<IBackgroundWorker>(worker);

        // 应用替身需要引用容器本身，只能在容器构建后赋值。
        FakeAbpApplication? application = null;
        services.AddSingleton<IAbpApplication>(_ => application!);

        var provider = services.BuildServiceProvider();
        application = new FakeAbpApplication { ServiceProvider = provider };

        var context = new ApplicationInitializationContext(provider);

        new LenAbpWpfModule().OnPreApplicationInitialization(context);

        Assert.NotNull(worker.LazyServiceProvider);
    }

    /// <summary>
    /// 延迟服务提供程序绑定根容器，而不是初始化作用域。
    /// </summary>
    /// <remarks>
    /// 初始化作用域在 <c>InitializeModulesAsync</c> 结束时被释放，而工作器是单例，
    /// 运行期仍会解析服务。绑定到作用域会让运行期解析抛 <see cref="ObjectDisposedException"/>。
    /// </remarks>
    [Fact]
    public void OnPreApplicationInitialization_BindsRootContainer()
    {
        var rootServices = new ServiceCollection();
        rootServices.AddLogging();
        rootServices.AddSingleton<IAbpLazyServiceProvider>(provider =>
            new AbpLazyServiceProvider(provider)
        );
        rootServices.AddSingleton<RootOnlyService>();

        var rootProvider = rootServices.BuildServiceProvider();

        var worker = new ProbeBackgroundWorker();
        var scopeServices = new ServiceCollection();
        scopeServices.AddLogging();
        scopeServices.AddSingleton<IAbpApplication>(
            new FakeAbpApplication { ServiceProvider = rootProvider }
        );
        scopeServices.AddSingleton<IBackgroundWorker>(worker);

        var scope = scopeServices.BuildServiceProvider().CreateScope();
        var context = new ApplicationInitializationContext(scope.ServiceProvider);

        new LenAbpWpfModule().OnPreApplicationInitialization(context);

        // 模拟 InitializeModulesAsync 结束：作用域被释放。
        scope.Dispose();

        // 作用域已释放后仍能解析，说明绑定的是根容器。
        Assert.NotNull(worker.LazyServiceProvider.LazyGetService<RootOnlyService>());
    }

    /// <summary>
    /// 没有注册后台工作器时不抛异常。
    /// </summary>
    [Fact]
    public void OnPreApplicationInitialization_WithoutWorkers_DoesNotThrow()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IAbpLazyServiceProvider>(provider =>
            new AbpLazyServiceProvider(provider)
        );

        FakeAbpApplication? application = null;
        services.AddSingleton<IAbpApplication>(_ => application!);

        var provider = services.BuildServiceProvider();
        application = new FakeAbpApplication { ServiceProvider = provider };

        var context = new ApplicationInitializationContext(provider);

        new LenAbpWpfModule().OnPreApplicationInitialization(context);
    }

    /// <summary>
    /// 只用于验证根容器解析的服务。
    /// </summary>
    private sealed class RootOnlyService;

    /// <summary>
    /// 探针工作器：暴露 <see cref="BackgroundWorkerBase.LazyServiceProvider"/> 供断言。
    /// </summary>
    private sealed class ProbeBackgroundWorker : BackgroundWorkerBase
    {
        public override Task StartAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public override Task StopAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
