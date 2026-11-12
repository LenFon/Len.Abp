using Len.Abp.Wpf.Mvvm;
using Len.Abp.Wpf.Navigation;
using Len.Abp.Wpf.Navigation.Regions;
using Len.Abp.Wpf.Navigation.Regions.Behaviors;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.DependencyInjection;
using Xunit;

// 附加属性 Len.Abp.Wpf.Navigation.Region 与区域实现 Len.Abp.Wpf.Navigation.Regions.Region 同名。
using RegionImpl = Len.Abp.Wpf.Navigation.Regions.Region;

namespace Len.Abp.Wpf.Tests;

/// <summary>
/// 覆盖多层嵌套区域的导航与拆除：区域可以无限层嵌套，每层都能独立导航。
/// </summary>
/// <remarks>
/// 这里直接构造区域与导航服务，不经过 XAML 与 <c>Loaded</c>，
/// 以便把「区域树」的语义与控件加载时机分开验证。
/// </remarks>
public class NestedRegionTests
{
    /// <summary>
    /// 三层嵌套：每层都能按名称找到自己的区域并完成导航。
    /// </summary>
    [Fact]
    public async Task NavigateAsync_ThreeLevels_EachLevelNavigates()
    {
        var rootRegion = new SingleActiveRegion("MainRegion", "MainView");
        var provider = new RegionProvider();
        provider.Add(rootRegion);

        var (level1, level1Region) = AddNestedLevel(provider, rootRegion, "level1", "Level1Region");
        var (level2, level2Region) = AddNestedLevel(provider, level1Region, "level2", "Level2Region");
        var (level3, level3Region) = AddNestedLevel(provider, level2Region, "level3", "Level3Region");

        var service = CreateService(provider, rootRegion, level1, level2, level3);

        Assert.True((await service.NavigateAsync("MainRegion", "level1", IParameters.Create())).Result);
        Assert.True((await service.NavigateAsync("Level1Region", "level2", IParameters.Create())).Result);
        Assert.True((await service.NavigateAsync("Level2Region", "level3", IParameters.Create())).Result);

        Assert.Same(level1, Assert.Single(rootRegion.ActiveViews));
        Assert.Same(level2, Assert.Single(level1Region.ActiveViews));
        Assert.Same(level3, Assert.Single(level2Region.ActiveViews));
        Assert.Contains(level3Region, provider.Regions);
    }

    /// <summary>
    /// 父视图失活时，整棵嵌套子树被拆除，任意深度的嵌套视图都被释放。
    /// </summary>
    [Fact]
    public async Task NavigateAway_FromNestedRoot_TearsDownWholeSubtree()
    {
        var rootRegion = new SingleActiveRegion("MainRegion", "MainView");
        var provider = new RegionProvider();
        provider.Add(rootRegion);

        var (level1, level1Region) = AddNestedLevel(provider, rootRegion, "level1", "Level1Region");
        var (level2, level2Region) = AddNestedLevel(provider, level1Region, "level2", "Level2Region");
        var (level3, level3Region) = AddNestedLevel(provider, level2Region, "level3", "Level3Region");
        var other = new FakeView("other");

        var service = CreateService(provider, rootRegion, level1, level2, level3, other);

        await service.NavigateAsync("MainRegion", "level1", IParameters.Create());
        await service.NavigateAsync("Level1Region", "level2", IParameters.Create());
        await service.NavigateAsync("Level2Region", "level3", IParameters.Create());

        // 切回另一个根视图，level1 及其整棵子树应被拆除。
        Assert.True((await service.NavigateAsync("MainRegion", "other", IParameters.Create())).Result);

        Assert.DoesNotContain(level1Region, provider.Regions);
        Assert.DoesNotContain(level2Region, provider.Regions);
        Assert.DoesNotContain(level3Region, provider.Regions);
        Assert.True(level1.IsDisposed);
        Assert.True(level2.IsDisposed);
        Assert.True(level3.IsDisposed);
    }

    /// <summary>
    /// 容器管理的嵌套视图被复用时，其区域必须保留，否则再次导航会找不到区域。
    /// </summary>
    [Fact]
    public async Task NavigateAway_ContainerManagedNestedView_KeepsRegionForReuse()
    {
        var rootRegion = new SingleActiveRegion("MainRegion", "MainView");
        var provider = new RegionProvider();
        provider.Add(rootRegion);

        var singletonView = new DisposableSingletonView("level1", new LifetimeViewModel(keepAlive: false));
        var nestedRegion = new SingleActiveRegion("Level1Region", "level1", singletonView);
        rootRegion.Add(singletonView);
        rootRegion.Activate(singletonView);
        provider.Add(nestedRegion);

        var other = new FakeView("other");
        var service = CreateService(provider, rootRegion, singletonView, other);

        Assert.True((await service.NavigateAsync("MainRegion", "other", IParameters.Create())).Result);

        Assert.Contains(nestedRegion, provider.Regions);
        Assert.False(singletonView.IsDisposed);
    }

    /// <summary>
    /// 在父区域中放一个视图，并为它创建一个子区域，模拟嵌套区域的创建。
    /// </summary>
    private static (DisposableFakeView View, SingleActiveRegion Region) AddNestedLevel(
        RegionProvider provider,
        SingleActiveRegion parentRegion,
        string viewName,
        string regionName
    )
    {
        var view = new DisposableFakeView(viewName, new LifetimeViewModel(keepAlive: false));
        var region = new SingleActiveRegion(regionName, viewName, view);

        parentRegion.Add(view);
        provider.Add(region);
        AttachLifetimeBehavior(parentRegion, provider);

        return (view, region);
    }

    private static void AttachLifetimeBehavior(SingleActiveRegion region, RegionProvider provider)
    {
        if (!region.Behaviors.ContainsKey(nameof(RegionMemberLifetimeBehavior)))
        {
            region.Behaviors.Add(nameof(RegionMemberLifetimeBehavior), new RegionMemberLifetimeBehavior(provider));
        }
    }

    private static NavigationService CreateService(
        RegionProvider provider,
        SingleActiveRegion rootRegion,
        params IView[] views
    )
    {
        AttachLifetimeBehavior(rootRegion, provider);

        var services = new ServiceCollection();
        foreach (var view in views)
        {
            services.AddKeyedSingleton<IView>(view.ViewName, view);
        }

        var serviceProvider = services.BuildServiceProvider();

        return new NavigationService(new RegionFinder(provider), serviceProvider);
    }

    private sealed class LifetimeViewModel(bool keepAlive) : IRegionMemberLifetime
    {
        public bool KeepAlive { get; } = keepAlive;
    }

    private class DisposableFakeView(string viewName, object viewModel) : IView, IDisposable
    {
        public object ViewModel { get; } = viewModel;

        public string ViewName { get; } = viewName;

        public bool IsDisposed { get; private set; }

        public void Dispose() => IsDisposed = true;
    }

    private sealed class DisposableSingletonView(string viewName, object viewModel)
        : DisposableFakeView(viewName, viewModel),
            ISingletonDependency;
}
