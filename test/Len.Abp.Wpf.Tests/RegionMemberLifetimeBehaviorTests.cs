using Len.Abp.Wpf.Mvvm;
using Len.Abp.Wpf.Navigation.Regions;
using Len.Abp.Wpf.Navigation.Regions.Behaviors;
using Volo.Abp.DependencyInjection;
using Xunit;

// 附加属性 Len.Abp.Wpf.Navigation.Region 与区域实现 Len.Abp.Wpf.Navigation.Regions.Region 同名。
using RegionImpl = Len.Abp.Wpf.Navigation.Regions.Region;

namespace Len.Abp.Wpf.Tests;

/// <summary>
/// 覆盖 <see cref="RegionMemberLifetimeBehavior"/> 的视图保活策略。
/// </summary>
/// <remarks>
/// 默认不保活：只有显式声明 <c>KeepAlive = true</c> 的视图会留在区域中，
/// 其余视图在失活时被移除并释放。
/// </remarks>
public class RegionMemberLifetimeBehaviorTests
{
    /// <summary>
    /// 视图模型声明 KeepAlive 为 false 时，失活会从区域中移除该视图，
    /// 并按视图实例把该视图拥有的区域从提供程序中移除。
    /// </summary>
    [Fact]
    public void Deactivate_KeepAliveFalse_RemovesView()
    {
        var provider = new RegionProvider();
        var view = new FakeView("demo", new LifetimeViewModel(keepAlive: false));
        // 区域所有者与视图实例一致，才能验证 RemoveSubtree 的联动。
        var region = new RegionImpl("MainRegion", view.ViewName, view);
        provider.Add(region);

        region.Add(view);
        region.Activate(view);

        region.Behaviors.Add(nameof(RegionMemberLifetimeBehavior), new RegionMemberLifetimeBehavior(provider));
        region.Deactivate(view);

        Assert.Empty(region.Views);
        Assert.Empty(provider.Regions);
    }

    /// <summary>
    /// 视图模型声明 KeepAlive 为 true 时，失活保留视图。
    /// </summary>
    [Fact]
    public void Deactivate_KeepAliveTrue_KeepsView()
    {
        var provider = new RegionProvider();
        var region = new RegionImpl("MainRegion", "MainView");
        provider.Add(region);

        var view = new FakeView("demo", new LifetimeViewModel(keepAlive: true));
        region.Add(view);
        region.Activate(view);

        region.Behaviors.Add(nameof(RegionMemberLifetimeBehavior), new RegionMemberLifetimeBehavior(provider));
        region.Deactivate(view);

        Assert.Single(region.Views);
        Assert.Single(provider.Regions);
    }

    /// <summary>
    /// 视图模型未实现 IRegionMemberLifetime 时默认不保活，失活即移除。
    /// </summary>
    /// <remarks>
    /// 早期实现把「未声明契约」当作保活，导致忘记实现接口的视图在区域里无限累积。
    /// </remarks>
    [Fact]
    public void Deactivate_NoLifetimeContract_RemovesView()
    {
        var provider = new RegionProvider();
        var region = new RegionImpl("MainRegion", "MainView");
        provider.Add(region);

        var view = new FakeView("demo", new object());
        region.Add(view);
        region.Activate(view);

        region.Behaviors.Add(nameof(RegionMemberLifetimeBehavior), new RegionMemberLifetimeBehavior(provider));
        region.Deactivate(view);

        Assert.Empty(region.Views);
    }

    /// <summary>
    /// 添加视图（非移除动作）不会触发保活逻辑。
    /// </summary>
    [Fact]
    public void AddView_DoesNotRemoveAnything()
    {
        var provider = new RegionProvider();
        var region = new RegionImpl("MainRegion", "MainView");
        provider.Add(region);
        region.Behaviors.Add(nameof(RegionMemberLifetimeBehavior), new RegionMemberLifetimeBehavior(provider));

        region.Add(new FakeView("demo", new LifetimeViewModel(keepAlive: false)));

        Assert.Single(region.Views);
    }

    /// <summary>
    /// 不保活的视图与视图模型都会被释放。
    /// </summary>
    [Fact]
    public void Deactivate_NotKeepAlive_DisposesViewAndViewModel()
    {
        var provider = new RegionProvider();
        var region = new RegionImpl("MainRegion", "MainView");
        provider.Add(region);

        var viewModel = new DisposableLifetimeViewModel(keepAlive: false);
        var view = new DisposableFakeView("demo", viewModel);
        region.Add(view);
        region.Activate(view);

        region.Behaviors.Add(nameof(RegionMemberLifetimeBehavior), new RegionMemberLifetimeBehavior(provider));
        region.Deactivate(view);

        Assert.True(view.IsDisposed);
        Assert.True(viewModel.IsDisposed);
    }

    /// <summary>
    /// 保活的视图不会被释放。
    /// </summary>
    [Fact]
    public void Deactivate_KeepAlive_DoesNotDispose()
    {
        var provider = new RegionProvider();
        var region = new RegionImpl("MainRegion", "MainView");
        provider.Add(region);

        var viewModel = new DisposableLifetimeViewModel(keepAlive: true);
        var view = new DisposableFakeView("demo", viewModel);
        region.Add(view);
        region.Activate(view);

        region.Behaviors.Add(nameof(RegionMemberLifetimeBehavior), new RegionMemberLifetimeBehavior(provider));
        region.Deactivate(view);

        Assert.False(view.IsDisposed);
        Assert.False(viewModel.IsDisposed);
    }

    /// <summary>
    /// 单例与作用域实例的生命周期归容器所有，切走后仍保留在区域中，供下次导航复用。
    /// </summary>
    /// <param name="singleton">视图是否为单例；false 表示作用域。</param>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Deactivate_ContainerManagedView_IsKeptAndNotDisposed(bool singleton)
    {
        var provider = new RegionProvider();
        var region = new RegionImpl("MainRegion", "MainView");
        provider.Add(region);

        var viewModel = new DisposableLifetimeViewModel(keepAlive: false);
        DisposableFakeView view = singleton
            ? new DisposableSingletonView("demo", viewModel)
            : new DisposableScopedView("demo", viewModel);
        region.Add(view);
        region.Activate(view);

        region.Behaviors.Add(nameof(RegionMemberLifetimeBehavior), new RegionMemberLifetimeBehavior(provider));
        region.Deactivate(view);

        Assert.Single(region.Views);
        Assert.False(view.IsDisposed);
    }

    /// <summary>
    /// 视图是瞬态但视图模型是单例时，视图同样保留，避免单例视图模型被重复绑定到新视图。
    /// </summary>
    [Fact]
    public void Deactivate_SingletonViewModel_KeepsView()
    {
        var provider = new RegionProvider();
        var region = new RegionImpl("MainRegion", "MainView");
        provider.Add(region);

        var viewModel = new SingletonLifetimeViewModel();
        var view = new DisposableFakeView("demo", viewModel);
        region.Add(view);
        region.Activate(view);

        region.Behaviors.Add(nameof(RegionMemberLifetimeBehavior), new RegionMemberLifetimeBehavior(provider));
        region.Deactivate(view);

        Assert.Single(region.Views);
        Assert.False(view.IsDisposed);
    }

    /// <summary>
    /// 不保活的视图被移除时，其内部任意深度的嵌套区域一并拆除，嵌套视图也被释放。
    /// </summary>
    [Fact]
    public void Deactivate_NestedRegions_AreTornDownRecursively()
    {
        var provider = new RegionProvider();
        var rootRegion = new RegionImpl("MainRegion", "MainView");
        provider.Add(rootRegion);

        var rootView = new DisposableFakeView("root", new LifetimeViewModel(keepAlive: false));
        var childView = new DisposableFakeView("child", new LifetimeViewModel(keepAlive: false));
        var grandchildView = new DisposableFakeView("grandchild", new LifetimeViewModel(keepAlive: false));

        var childRegion = new RegionImpl("ChildRegion", "root", rootView);
        var grandchildRegion = new RegionImpl("GrandchildRegion", "child", childView);
        childRegion.Add(childView);
        grandchildRegion.Add(grandchildView);

        rootRegion.Add(rootView);
        rootRegion.Activate(rootView);
        provider.Add(childRegion);
        provider.Add(grandchildRegion);

        rootRegion.Behaviors.Add(nameof(RegionMemberLifetimeBehavior), new RegionMemberLifetimeBehavior(provider));
        rootRegion.Deactivate(rootView);

        // 根区域本身不属于任何视图，保留；其余整棵子树都被拆除。
        Assert.Same(rootRegion, Assert.Single(provider.Regions));
        Assert.True(rootView.IsDisposed);
        Assert.True(childView.IsDisposed);
        Assert.True(grandchildView.IsDisposed);
    }

    /// <summary>
    /// 容器管理的嵌套视图会被复用，拆除父视图时必须保留它的区域。
    /// </summary>
    /// <remarks>
    /// 区域只在视图首次加载时创建，摘掉后不会重建，下次导航到该单例视图会找不到区域。
    /// </remarks>
    [Fact]
    public void Deactivate_ContainerManagedNestedView_KeepsItsRegion()
    {
        var provider = new RegionProvider();
        var rootRegion = new RegionImpl("MainRegion", "MainView");
        provider.Add(rootRegion);

        var rootView = new DisposableFakeView("root", new LifetimeViewModel(keepAlive: false));
        var singletonView = new DisposableSingletonView("child", new LifetimeViewModel(keepAlive: false));
        var childRegion = new RegionImpl("ChildRegion", "child", singletonView);

        rootRegion.Add(rootView);
        rootRegion.Activate(rootView);
        provider.Add(childRegion);

        rootRegion.Behaviors.Add(nameof(RegionMemberLifetimeBehavior), new RegionMemberLifetimeBehavior(provider));
        rootRegion.Deactivate(rootView);

        Assert.True(rootView.IsDisposed);
        Assert.False(singletonView.IsDisposed);
        Assert.Contains(childRegion, provider.Regions);
    }

    /// <summary>
    /// 保活的视图被复用时，其嵌套区域不会被拆除。
    /// </summary>
    [Fact]
    public void Deactivate_KeepAliveView_KeepsNestedRegions()
    {
        var provider = new RegionProvider();
        var rootRegion = new RegionImpl("MainRegion", "MainView");
        provider.Add(rootRegion);

        var rootView = new FakeView("root", new LifetimeViewModel(keepAlive: true));
        var childRegion = new RegionImpl("ChildRegion", "root", rootView);

        rootRegion.Add(rootView);
        rootRegion.Activate(rootView);
        provider.Add(childRegion);

        rootRegion.Behaviors.Add(nameof(RegionMemberLifetimeBehavior), new RegionMemberLifetimeBehavior(provider));
        rootRegion.Deactivate(rootView);

        Assert.Single(rootRegion.Views);
        Assert.Contains(childRegion, provider.Regions);
    }

    private sealed class LifetimeViewModel(bool keepAlive) : IRegionMemberLifetime
    {
        public bool KeepAlive { get; } = keepAlive;
    }

    private sealed class DisposableLifetimeViewModel(bool keepAlive) : IRegionMemberLifetime, IDisposable
    {
        public bool KeepAlive { get; } = keepAlive;

        public bool IsDisposed { get; private set; }

        public void Dispose() => IsDisposed = true;
    }

    private sealed class SingletonLifetimeViewModel : IRegionMemberLifetime, ISingletonDependency
    {
        public bool KeepAlive => false;
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

    private sealed class DisposableScopedView(string viewName, object viewModel)
        : DisposableFakeView(viewName, viewModel),
            IScopedDependency;
}
