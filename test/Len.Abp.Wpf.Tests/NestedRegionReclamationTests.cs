using Len.Abp.Wpf.Mvvm;
using Len.Abp.Wpf.Navigation;
using Len.Abp.Wpf.Navigation.Regions;
using RegionImpl = Len.Abp.Wpf.Navigation.Regions.Region;
using Len.Abp.Wpf.Navigation.Regions.Behaviors;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;
using Volo.Abp.DependencyInjection;
using Xunit;

namespace Len.Abp.Wpf.Tests;

/// <summary>
/// 验证多层嵌套区域在离开后是否被完整拆除，以及视图与视图模型能否被回收。
/// </summary>
/// <remarks>
/// 用弱引用判断回收：先构造八层嵌套，再切走并触发 GC，
/// 断言区域提供程序已清空、所有视图与视图模型都不再存活。
/// </remarks>
public class NestedRegionReclamationTests
{
    /// <summary>
    /// 八层嵌套离开后，区域全部摘除，视图与视图模型都可被回收。
    /// </summary>
    [Fact]
    public async Task NavigateAway_EightLevels_ReleasesAllRegionsAndInstances()
    {
        var (service, provider, rootRegion) = CreateService();

        var levels = BuildChain(provider, rootRegion, 8);

        // 逐层导航，模拟用户点「进入下一层」。
        Assert.True((await service.NavigateAsync("MainRegion", "level1", IParameters.Create())).Result);
        for (var depth = 1; depth < 8; depth++)
        {
            Assert.True((await service.NavigateAsync($"Level{depth}Region", $"level{depth + 1}", IParameters.Create())).Result);
        }

        // 八层全部就位时，区域数 = 根区域 + 8 个子区域。
        Assert.Equal(9, provider.Regions.Count);

        // 切到其他视图，根视图失活，整棵子树应被递归拆除。
        Assert.True((await service.NavigateAsync("MainRegion", "other", IParameters.Create())).Result);

        Assert.Same(rootRegion, Assert.Single(provider.Regions));

        // 弱引用必须在这里创建，之后不能再持有强引用。
        var viewRefs = levels.Select(level => new WeakReference(level.View)).ToArray();
        var viewModelRefs = levels.Select(level => new WeakReference(level.View.ViewModel)).ToArray();
        levels.Clear();

        CollectGarbage();

        Assert.All(viewRefs, reference => Assert.False(reference.IsAlive));
        Assert.All(viewModelRefs, reference => Assert.False(reference.IsAlive));
    }

    /// <summary>
    /// 构造指定层数的嵌套区域链，返回每层的视图与区域。
    /// </summary>
    /// <remarks>
    /// 单独成方法是为了让循环里的局部变量随返回失效；
    /// 若写在测试方法内，Debug 构建的栈槽会一直持有最深一层的区域，干扰回收断言。
    /// </remarks>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static List<(DisposableView View, RegionImpl Region)> BuildChain(
        RegionProvider provider,
        RegionImpl rootRegion,
        int depthCount
    )
    {
        var levels = new List<(DisposableView View, RegionImpl Region)>();
        var parent = rootRegion;

        for (var depth = 1; depth <= depthCount; depth++)
        {
            var view = new DisposableView($"level{depth}", new DisposableViewModel());
            var region = new RegionImpl($"Level{depth}Region", view.ViewName, view);
            parent.Add(view);
            provider.Add(region);
            AttachLifetimeBehavior(parent, provider);

            levels.Add((view, region));
            parent = region;
        }

        return levels;
    }

    /// <summary>
    /// 反复「进入八层 → 切到其他视图 → 切回」时，每一轮的旧实例都被释放，
    /// 区域数量不会随轮次增长。
    /// </summary>
    /// <remarks>
    /// 这是用户实际操作的路径：切走再切回。若拆除只处理一层或漏掉深层区域，
    /// 区域数会逐轮累积，弱引用也会一直存活。
    /// </remarks>
    [Fact]
    public async Task NavigateAwayAndBack_Repeatedly_DoesNotAccumulate()
    {
        var (service, provider, rootRegion) = CreateService();
        var allViewRefs = new List<WeakReference>();
        var allViewModelRefs = new List<WeakReference>();

        for (var round = 1; round <= 3; round++)
        {
            var (viewRefs, viewModelRefs) = await RunOneRoundAsync(service, provider, rootRegion);
            allViewRefs.AddRange(viewRefs);
            allViewModelRefs.AddRange(viewModelRefs);
        }

        CollectGarbage();

        // 三轮共 24 个视图与视图模型，全部应已释放。
        Assert.Equal(24, allViewRefs.Count);
        Assert.All(allViewRefs, reference => Assert.False(reference.IsAlive));
        Assert.All(allViewModelRefs, reference => Assert.False(reference.IsAlive));
    }

    /// <summary>
    /// 执行一轮「进入八层 → 切走」，返回该轮实例的弱引用。
    /// </summary>
    /// <remarks>
    /// 单独成方法且禁止内联，使本轮的强引用在返回后失效；
    /// 若把这段写在测试方法内，循环局部变量会一直持有最后一轮的视图，干扰回收断言。
    /// </remarks>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<(WeakReference[] Views, WeakReference[] ViewModels)> RunOneRoundAsync(
        NavigationService service,
        RegionProvider provider,
        RegionImpl rootRegion
    )
    {
        var levels = BuildChain(provider, rootRegion, 8);

        Assert.True((await service.NavigateAsync("MainRegion", "level1", IParameters.Create())).Result);
        for (var depth = 1; depth < 8; depth++)
        {
            Assert.True((await service.NavigateAsync($"Level{depth}Region", $"level{depth + 1}", IParameters.Create())).Result);
        }

        Assert.Equal(9, provider.Regions.Count);

        var viewRefs = levels.Select(level => new WeakReference(level.View)).ToArray();
        var viewModelRefs = levels.Select(level => new WeakReference(level.View.ViewModel)).ToArray();

        // 切走：整棵子树应被拆除。
        Assert.True((await service.NavigateAsync("MainRegion", "other", IParameters.Create())).Result);
        Assert.Same(rootRegion, Assert.Single(provider.Regions));

        return (viewRefs, viewModelRefs);
    }

    /// <summary>
    /// 保活的视图离开后仍留在区域中，其嵌套区域不会被拆除。
    /// </summary>
    [Fact]
    public async Task NavigateAway_KeepAliveRoot_KeepsWholeSubtree()    {
        var (service, provider, rootRegion) = CreateService();

        var rootView = new DisposableView("level1", new DisposableViewModel { KeepAlive = true });
        var childView = new DisposableView("level2", new DisposableViewModel());
        var childRegion = new RegionImpl("Level1Region", rootView.ViewName, rootView);
        childRegion.Add(childView);
        rootRegion.Add(rootView);
        provider.Add(childRegion);
        AttachLifetimeBehavior(rootRegion, provider);

        await service.NavigateAsync("MainRegion", "level1", IParameters.Create());
        await service.NavigateAsync("MainRegion", "other", IParameters.Create());

        Assert.Contains(childRegion, provider.Regions);
        Assert.Contains(rootView, rootRegion.Views);
        Assert.False(rootView.IsDisposed);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CollectGarbage()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    private static void AttachLifetimeBehavior(RegionImpl region, RegionProvider provider)
    {
        if (!region.Behaviors.ContainsKey(nameof(RegionMemberLifetimeBehavior)))
        {
            region.Behaviors.Add(nameof(RegionMemberLifetimeBehavior), new RegionMemberLifetimeBehavior(provider));
        }
    }

    private static (NavigationService Service, RegionProvider Provider, SingleActiveRegion RootRegion) CreateService()
    {
        var rootRegion = new SingleActiveRegion("MainRegion", "MainView");
        var provider = new RegionProvider();
        provider.Add(rootRegion);
        AttachLifetimeBehavior(rootRegion, provider);

        var services = new ServiceCollection();
        services.AddKeyedSingleton<IView>("other", new FakeView("other"));
        var serviceProvider = services.BuildServiceProvider();

        return (new NavigationService(new RegionFinder(provider), serviceProvider), provider, rootRegion);
    }

    private sealed class DisposableViewModel : IRegionMemberLifetime, IDisposable
    {
        public bool KeepAlive { get; init; }

        public bool IsDisposed { get; private set; }

        public void Dispose() => IsDisposed = true;
    }

    private sealed class DisposableView(string viewName, object viewModel) : IView, IDisposable
    {
        public object ViewModel { get; } = viewModel;

        public string ViewName { get; } = viewName;

        public bool IsDisposed { get; private set; }

        public void Dispose() => IsDisposed = true;
    }
}
