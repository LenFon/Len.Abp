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
/// 覆盖：导航作用域。
/// </summary>
/// <remarks>
/// 每次导航为视图创建独立 <see cref="IServiceScope"/>，因此 <c>Scoped</c> 视图跨导航得到新实例，
/// 视图被拆除时作用域随之释放。
/// </remarks>
public class NavigationScopeTests
{
    /// <summary>
    /// Scoped 视图跨导航得到新实例。
    /// </summary>
    [Fact]
    public async Task NavigateAsync_ScopedView_ResolvesNewInstancePerNavigation()
    {
        var (service, _, factory) = CreateService();

        await service.NavigateAsync("MainRegion", "scoped");
        await service.NavigateAsync("MainRegion", "scoped");

        var instances = factory.CreatedFor("scoped");
        Assert.Equal(2, instances.Count);
        Assert.NotSame(instances[0], instances[1]);
    }

    /// <summary>
    /// Scoped 视图每次导航都是新实例，旧实例被释放，区域中只保留一个。
    /// </summary>
    [Fact]
    public async Task NavigateAsync_ScopedView_ReplacesPreviousInstance()
    {
        var (service, region, factory) = CreateService();

        await service.NavigateAsync("MainRegion", "scoped");
        var firstInstance = factory.CreatedFor("scoped")[0];

        await service.NavigateAsync("MainRegion", "scoped");
        var secondInstance = factory.CreatedFor("scoped")[1];

        Assert.NotSame(firstInstance, secondInstance);
        Assert.True(firstInstance.IsDisposed);
        Assert.False(secondInstance.IsDisposed);
        Assert.Single(region.Views);
        Assert.Same(secondInstance, region.Views.Single());
    }

    /// <summary>
    /// 视图被移除后，其导航作用域被释放。
    /// </summary>
    [Fact]
    public async Task NavigateAsync_ScopedView_DisposesScopeWhenRemoved()
    {
        var (service, _, factory) = CreateService();

        await service.NavigateAsync("MainRegion", "scoped");
        var instance = factory.CreatedFor("scoped")[0];

        // 导航到另一个视图，旧视图失活并被拆除。
        await service.NavigateAsync("MainRegion", "other");

        Assert.True(instance.IsDisposed);
        Assert.Equal(1, factory.DisposedScopeCount);
    }

    /// <summary>
    /// 保活的 Scoped 视图复用同一实例与同一作用域。
    /// </summary>
    [Fact]
    public async Task NavigateAsync_KeepAliveScopedView_ReusesInstanceAndScope()
    {
        var (service, region, factory) = CreateService();

        await service.NavigateAsync("MainRegion", "scoped-keepalive");
        var firstInstance = factory.CreatedFor("scoped-keepalive")[0];

        await service.NavigateAsync("MainRegion", "other");
        await service.NavigateAsync("MainRegion", "scoped-keepalive");

        Assert.Single(factory.CreatedFor("scoped-keepalive"));
        Assert.Same(firstInstance, region.Views.Single(view => view.ViewName == "scoped-keepalive"));
        Assert.False(firstInstance.IsDisposed);

        // 中间导航到 other 时，other 的作用域被释放；保活视图的作用域不受影响。
        Assert.Equal(1, factory.DisposedScopeCount);
    }

    /// <summary>
    /// 瞬态视图行为不变：每次导航新实例，移除时释放。
    /// </summary>
    [Fact]
    public async Task NavigateAsync_TransientView_StillResolvesNewInstance()
    {
        var (service, region, factory) = CreateService();

        await service.NavigateAsync("MainRegion", "transient");
        var firstInstance = factory.CreatedFor("transient")[0];

        await service.NavigateAsync("MainRegion", "other");

        Assert.True(firstInstance.IsDisposed);
        Assert.DoesNotContain(region.Views, view => view.ViewName == "transient");
    }

    /// <summary>
    /// 单例视图行为不变：反复导航复用同一实例，移除时不释放。
    /// </summary>
    [Fact]
    public async Task NavigateAsync_SingletonView_ReusesInstance()
    {
        var (service, region, factory) = CreateService();

        await service.NavigateAsync("MainRegion", "singleton");
        await service.NavigateAsync("MainRegion", "other");
        await service.NavigateAsync("MainRegion", "singleton");

        Assert.Single(factory.CreatedFor("singleton"));
        Assert.False(factory.CreatedFor("singleton")[0].IsDisposed);
        Assert.Contains(region.Views, view => view.ViewName == "singleton");
    }

    /// <summary>
    /// 视图解析失败时，为它创建的作用域被释放，不泄漏。
    /// </summary>
    [Fact]
    public async Task NavigateAsync_UnresolvableView_DisposesScope()
    {
        var (service, _, factory) = CreateService();

        var result = await service.NavigateAsync("MainRegion", "missing");

        Assert.False(result.Result);
        Assert.NotNull(result.Exception);
        Assert.Equal(1, factory.CreatedScopeCount);
        Assert.Equal(1, factory.DisposedScopeCount);
    }

    /// <summary>
    /// 作用域视图的 Scoped 依赖也来自同一个导航作用域。
    /// </summary>
    [Fact]
    public async Task NavigateAsync_ScopedView_ResolvesScopedDependencyFromSameScope()
    {
        var (service, _, factory) = CreateService();

        await service.NavigateAsync("MainRegion", "scoped");

        var instance = factory.CreatedFor("scoped")[0];
        Assert.NotNull(instance.ScopedDependency);
        Assert.Equal(instance.ScopeId, instance.ScopedDependency.ScopeId);
    }

    private static (NavigationService Service, Region Region, RecordingViewFactory Factory) CreateService()
    {
        // 与 ContentControlRegionAdapter 一致：同一时刻只有一个活动视图，
        // 旧视图失活后才会触发拆除。
        var region = new SingleActiveRegion("MainRegion", "MainView");
        var provider = new RegionProvider();
        provider.Add(region);

        var factory = new RecordingViewFactory();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(factory);
        services.AddScoped<ScopedDependency>();

        services.AddKeyedTransient<IView>("scoped", (sp, _) =>
            factory.CreateScoped(sp, "scoped", keepAlive: false));
        services.AddKeyedTransient<IView>("scoped-keepalive", (sp, _) =>
            factory.CreateScoped(sp, "scoped-keepalive", keepAlive: true));
        services.AddKeyedTransient<IView>("transient", (sp, _) =>
            factory.CreateTransient("transient"));
        services.AddKeyedSingleton<IView>("singleton", (sp, _) =>
            factory.CreateTransient("singleton", isSingleton: true));
        services.AddKeyedTransient<IView>("other", (sp, _) =>
            factory.CreateTransient("other"));

        var innerProvider = services.BuildServiceProvider();

        // 用装饰器观察作用域的创建与释放：导航服务通过 IServiceScopeFactory 创建作用域。
        var scopeFactory = new RecordingScopeFactory(
            innerProvider.GetRequiredService<IServiceScopeFactory>(),
            factory
        );
        var serviceProvider = new ScopeFactoryOverrideProvider(innerProvider, scopeFactory);

        var registry = new ViewScopeRegistry();

        region.Behaviors.Add(
            nameof(RegionMemberLifetimeBehavior),
            new RegionMemberLifetimeBehavior(provider, registry)
        );

        var service = new NavigationService(
            new RegionFinder(provider),
            provider,
            serviceProvider,
            new FakeUiDispatcher(),
            new NavigationViewResolver(serviceProvider, registry),
            Options.Create(new NavigationOptions())
        );

        return (service, region, factory);
    }

    /// <summary>
    /// 记录作用域创建与释放的工厂装饰器。
    /// </summary>
    private sealed class RecordingScopeFactory(
        IServiceScopeFactory inner,
        RecordingViewFactory recorder
    ) : IServiceScopeFactory
    {
        public IServiceScope CreateScope()
        {
            recorder.OnScopeCreated();
            return new RecordingScope(inner.CreateScope(), recorder);
        }
    }

    /// <summary>
    /// 在释放内部作用域时通知记录器。
    /// </summary>
    private sealed class RecordingScope(IServiceScope inner, RecordingViewFactory recorder) : IServiceScope
    {
        public IServiceProvider ServiceProvider => inner.ServiceProvider;

        public void Dispose()
        {
            recorder.OnScopeDisposed();
            inner.Dispose();
        }
    }

    /// <summary>
    /// 让 <see cref="IServiceScopeFactory"/> 解析到装饰器的服务提供程序。
    /// </summary>
    private sealed class ScopeFactoryOverrideProvider(
        IServiceProvider inner,
        IServiceScopeFactory scopeFactory
    ) : IServiceProvider
    {
        public object? GetService(Type serviceType) =>
            serviceType == typeof(IServiceScopeFactory) ? scopeFactory : inner.GetService(serviceType);
    }

    /// <summary>
    /// 记录创建的视图实例，便于断言实例替换与释放。
    /// </summary>
    private sealed class RecordingViewFactory
    {
        public List<TestView> Created { get; } = [];

        public int CreatedScopeCount { get; private set; }

        public int DisposedScopeCount { get; private set; }

        public IReadOnlyList<TestView> CreatedFor(string viewName) =>
            [.. Created.Where(view => view.ViewName == viewName)];

        public ScopedTestView CreateScoped(IServiceProvider sp, string viewName, bool keepAlive)
        {
            var dependency = sp.GetRequiredService<ScopedDependency>();
            var view = new ScopedTestView(viewName, keepAlive)
            {
                ScopedDependency = dependency,
                ScopeId = dependency.ScopeId,
            };

            Created.Add(view);
            return view;
        }

        public TestView CreateTransient(string viewName, bool isSingleton = false)
        {
            var view = new TestView(viewName, keepAlive: false) { IsSingleton = isSingleton };
            Created.Add(view);
            return view;
        }

        /// <summary>
        /// 记录一次作用域创建。
        /// </summary>
        public void OnScopeCreated() => CreatedScopeCount++;

        /// <summary>
        /// 记录一次作用域释放。
        /// </summary>
        public void OnScopeDisposed() => DisposedScopeCount++;
    }

    /// <summary>
    /// 作用域依赖，用于验证视图与依赖来自同一作用域。
    /// </summary>
    private sealed class ScopedDependency : IScopedDependency
    {
        private static int _counter;

        public int ScopeId { get; } = Interlocked.Increment(ref _counter);
    }

    /// <summary>
    /// 可观察释放状态的视图。
    /// </summary>
    /// <param name="viewName">视图名称。</param>
    /// <param name="keepAlive">视图模型是否声明保活。</param>
    private class TestView(string viewName, bool keepAlive) : IView, IDisposable
    {
        public object ViewModel { get; } = new TestViewModel(keepAlive);

        public string ViewName { get; } = viewName;

        public bool IsSingleton { get; init; }

        public ScopedDependency? ScopedDependency { get; init; }

        public int ScopeId { get; init; }

        public bool IsDisposed { get; private set; }

        public void Dispose() => IsDisposed = true;
    }

    /// <summary>
    /// 声明为作用域的视图，模拟生成器为 <c>Lifetime = Scoped</c> 视图产出的标记接口。
    /// </summary>
    private sealed class ScopedTestView(string viewName, bool keepAlive)
        : TestView(viewName, keepAlive),
            IScopedDependency;

    /// <summary>
    /// 视图模型，可声明保活。
    /// </summary>
    private sealed class TestViewModel(bool keepAlive) : IRegionMemberLifetime
    {
        public bool KeepAlive { get; } = keepAlive;
    }
}
