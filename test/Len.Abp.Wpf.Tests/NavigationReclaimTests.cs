using Len.Abp.Wpf.Mvvm;
using Len.Abp.Wpf.Navigation;
using Len.Abp.Wpf.Navigation.Regions;
using Len.Abp.Wpf.Navigation.Regions.Behaviors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using Xunit;

namespace Len.Abp.Wpf.Tests;

/// <summary>
/// 覆盖导航失败与拒绝导航时的区域状态。
/// </summary>
/// <remarks>
/// 这两组用例断言的是"区域里还剩什么"，而不是返回值：
/// 返回值正确但区域残留视图，界面与内存都会出问题。
/// </remarks>
public class NavigationReclaimTests
{
    /// <summary>
    /// 单例视图拒绝本次导航后，后续导航仍能成功。
    /// </summary>
    /// <remarks>
    /// 回归：容器对单例返回同一实例，<c>region.Add</c> 会抛"该视图已存在于区域中"，
    /// 之后每次导航都失败。修复后解析器检测到实例已在区域中时直接复用。
    /// </remarks>
    [Fact]
    public async Task NavigateAsync_SingletonDeclinesNavigation_SubsequentNavigationsSucceed()
    {
        var view = new FakeView("demo", new DecliningViewModel());
        var (service, region) = CreateService(services =>
            services.AddKeyedSingleton<IView>("demo", view)
        );

        var first = await service.NavigateAsync("MainRegion", "demo", IParameters.Create());
        var second = await service.NavigateAsync("MainRegion", "demo", IParameters.Create());
        var third = await service.NavigateAsync("MainRegion", "demo", IParameters.Create());

        Assert.True(first.Result);
        Assert.True(second.Result, second.Exception?.Message);
        Assert.True(third.Result, third.Exception?.Message);
        Assert.Single(region.Views);
        Assert.Same(view, region.Views.Single());
        Assert.Single(region.ActiveViews);
    }

    /// <summary>
    /// 参数校验失败时，本次新建的作用域视图不会留在区域中。
    /// </summary>
    /// <remarks>
    /// 回归：不保活的作用域视图不会被后续导航复用，每次失败都会新建一个并加入区域，
    /// 旧的既没激活也没失活，永久残留（连同导航作用域）。
    /// </remarks>
    [Fact]
    public async Task NavigateAsync_ValidationFails_DoesNotAccumulateScopedViews()
    {
        var created = 0;
        var (service, region) = CreateService(
            services => services.AddKeyedTransient<IView>("scoped", (_, _) =>
            {
                created++;
                return new ScopedView("scoped", new Dictionary<string, Type?> { ["Id"] = typeof(int) });
            }),
            withTeardown: true
        );

        for (var i = 0; i < 5; i++)
        {
            var result = await service.NavigateAsync("MainRegion", "scoped", IParameters.Create());
            Assert.False(result.Result);
        }

        Assert.Equal(5, created);
        Assert.Empty(region.Views);
        Assert.Empty(region.ActiveViews);
    }

    /// <summary>
    /// 取消时同样不留下新建的视图。
    /// </summary>
    [Fact]
    public async Task NavigateAsync_Cancelled_DoesNotAccumulateScopedViews()
    {
        var (service, region) = CreateService(
            services => services.AddKeyedTransient<IView>("scoped", (_, _) =>
                new ScopedView("scoped", new Dictionary<string, Type?>())),
            withTeardown: true
        );

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        for (var i = 0; i < 3; i++)
        {
            await Assert.ThrowsAsync<OperationCanceledException>(
                async () => await service.NavigateAsync("MainRegion", "scoped", IParameters.Create(), cts.Token)
            );
        }

        Assert.Empty(region.Views);
        Assert.Empty(region.ActiveViews);
    }

    /// <summary>
    /// 校验失败回收视图时，导航作用域及其创建的 Scoped 实例必须被释放。
    /// </summary>
    /// <remarks>
    /// 回归：回收只调用 <c>region.Remove</c>，而它只改元数据、不触发
    /// <c>ActiveViews</c> 变更，拆除行为收不到通知，作用域永久泄漏。
    /// 断言"区域为空"无法发现这个问题——必须观察 Scoped 实例是否被释放。
    /// </remarks>
    [Fact]
    public async Task NavigateAsync_ValidationFails_DisposesNavigationScope()
    {
        var probes = new List<DisposalProbe>();
        var (service, region) = CreateService(
            services =>
            {
                services.AddScoped<DisposalProbe>();
                services.AddKeyedTransient<IView>("scoped", (provider, _) =>
                {
                    // 由导航作用域创建，作用域释放时随之释放。
                    var probe = provider.GetRequiredService<DisposalProbe>();
                    probes.Add(probe);

                    return new ScopedView("scoped", new Dictionary<string, Type?> { ["Id"] = typeof(int) });
                });
            },
            withTeardown: true
        );

        for (var i = 0; i < 3; i++)
        {
            Assert.False((await service.NavigateAsync("MainRegion", "scoped", IParameters.Create())).Result);
        }

        Assert.Equal(3, probes.Count);
        Assert.All(probes, probe => Assert.True(probe.IsDisposed));
        Assert.Empty(region.Views);
    }

    /// <summary>
    /// 取消时回收视图同样释放导航作用域。
    /// </summary>
    /// <remarks>
    /// 取消必须发生在视图解析之后：入口处的取消检查会让导航根本不创建作用域，
    /// 那样断言"作用域已释放"是空转。这里在视图构造时取消令牌，
    /// 复现"解析完成、激活之前取消"这条真实路径。
    /// </remarks>
    [Fact]
    public async Task NavigateAsync_Cancelled_DisposesNavigationScope()
    {
        var probes = new List<DisposalProbe>();
        using var cts = new CancellationTokenSource();

        var (service, region) = CreateService(
            services =>
            {
                services.AddScoped<DisposalProbe>();
                services.AddKeyedTransient<IView>("scoped", (provider, _) =>
                {
                    probes.Add(provider.GetRequiredService<DisposalProbe>());
                    cts.Cancel();

                    return new ScopedView("scoped", new Dictionary<string, Type?>());
                });
            },
            withTeardown: true
        );

        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await service.NavigateAsync("MainRegion", "scoped", IParameters.Create(), cts.Token)
        );

        Assert.Single(probes);
        Assert.True(probes[0].IsDisposed);
        Assert.Empty(region.Views);
    }

    /// <summary>
    /// 使用者显式调用 <see cref="IRegion.Remove"/> 时，视图的导航作用域同样被释放。
    /// </summary>
    /// <remarks>
    /// <see cref="ItemsControlRegionAdapter"/> 的文档让使用者用 <c>Remove</c> 回收多活动区域的视图。
    /// 若 <c>Remove</c> 只改元数据，这条文档承诺的路径会静默泄漏作用域。
    /// </remarks>
    [Fact]
    public async Task RegionRemove_ExplicitReclaim_DisposesNavigationScope()
    {
        var probes = new List<DisposalProbe>();
        var (service, region) = CreateService(
            services =>
            {
                services.AddScoped<DisposalProbe>();
                services.AddKeyedTransient<IView>("scoped", (provider, _) =>
                {
                    probes.Add(provider.GetRequiredService<DisposalProbe>());

                    return new ScopedView("scoped", new Dictionary<string, Type?>());
                });
            },
            withTeardown: true
        );

        Assert.True((await service.NavigateAsync("MainRegion", "scoped", IParameters.Create())).Result);
        var view = Assert.Single(region.Views);
        Assert.False(probes[0].IsDisposed);

        region.Remove(view);

        Assert.True(probes[0].IsDisposed);
        Assert.Empty(region.Views);
    }

    /// <summary>
    /// 校验失败时，复用的保活视图必须留在区域中。
    /// </summary>
    /// <remarks>
    /// 回收只针对本次新建的视图：复用的视图属于之前的导航，
    /// 移除它会破坏"界面回到导航前"的语义。
    /// </remarks>
    [Fact]
    public async Task NavigateAsync_ValidationFails_KeepsReusedView()
    {
        var keepAlive = new FakeView("demo", new KeepAliveViewModel())
        {
            RequiredNavigationParameters = new Dictionary<string, Type?> { ["Id"] = typeof(int) },
        };

        var (service, region) = CreateService(services =>
            services.AddKeyedSingleton<IView>("demo", keepAlive)
        );

        // 首次导航缺少 Id，失败；视图是本次新建的，会被回收。
        var first = await service.NavigateAsync("MainRegion", "demo", IParameters.Create());
        Assert.False(first.Result);
        Assert.Empty(region.Views);

        // 带上 Id 导航成功，视图留在区域中。
        var second = await service.NavigateAsync("MainRegion", "demo", IParameters.Create("?Id=1"));
        Assert.True(second.Result);
        Assert.Single(region.Views);

        // 再次缺少 Id：视图是复用的，必须保留。
        var third = await service.NavigateAsync("MainRegion", "demo", IParameters.Create());
        Assert.False(third.Result);
        Assert.Single(region.Views);
        Assert.Same(keepAlive, region.Views.Single());
    }

    /// <summary>
    /// 成功导航后视图正常留在区域中。
    /// </summary>
    [Fact]
    public async Task NavigateAsync_Success_KeepsView()
    {
        var view = new FakeView("demo");
        var (service, region) = CreateService(services =>
            services.AddKeyedSingleton<IView>("demo", view)
        );

        var result = await service.NavigateAsync("MainRegion", "demo", IParameters.Create());

        Assert.True(result.Result);
        Assert.Single(region.Views);
        Assert.Single(region.ActiveViews);
    }

    private static (NavigationService Service, Region Region) CreateService(
        Action<IServiceCollection> configure,
        bool withTeardown = false
    )
    {
        // 与 ContentControlRegionAdapter 一致：同一时刻只有一个活动视图。
        var region = new SingleActiveRegion("MainRegion", "MainView");
        var provider = new RegionProvider();
        provider.Add(region);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<object>();
        configure(services);

        var serviceProvider = services.BuildServiceProvider();
        var registry = new ViewScopeRegistry();

        // 只有需要验证"失活即拆除"的用例才挂行为：
        // 没有行为时区域退化为直接清空元数据，回收路径的差异会被掩盖。
        if (withTeardown)
        {
            region.Behaviors.Add(
                nameof(RegionMemberLifetimeBehavior),
                new RegionMemberLifetimeBehavior(provider, registry)
            );
        }

        var service = new NavigationService(
            new RegionFinder(provider),
            provider,
            serviceProvider,
            new FakeUiDispatcher(),
            new NavigationViewResolver(serviceProvider, registry),
            Options.Create(new NavigationOptions())
        );

        return (service, region);
    }

    /// <summary>
    /// 拒绝所有导航请求的视图模型。
    /// </summary>
    private sealed class DecliningViewModel : INavigationAware
    {
        public Task<bool> IsNavigationTargetAsync(INavigationContext navigationContext) => Task.FromResult(false);

        public Task OnNavigatedFromAsync(INavigationContext navigationContext) => Task.CompletedTask;

        public Task OnNavigatedToAsync(INavigationContext navigationContext) => Task.CompletedTask;
    }

    /// <summary>
    /// 声明保活的视图模型。
    /// </summary>
    private sealed class KeepAliveViewModel : IRegionMemberLifetime
    {
        public bool KeepAlive => true;
    }

    /// <summary>
    /// 声明为作用域的视图。
    /// </summary>
    private sealed class ScopedView(string viewName, IReadOnlyDictionary<string, Type?> required)
        : IView,
            IScopedDependency
    {
        public object ViewModel { get; } = new object();

        public string ViewName { get; } = viewName;

        public IReadOnlyDictionary<string, Type?> RequiredNavigationParameters { get; } = required;
    }

    /// <summary>
    /// 由导航作用域创建的可释放探针，用于观察作用域是否被释放。
    /// </summary>
    private sealed class DisposalProbe : IDisposable
    {
        public bool IsDisposed { get; private set; }

        public void Dispose() => IsDisposed = true;
    }
}
