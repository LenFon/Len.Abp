using Len.Abp.Wpf.Navigation;
using Len.Abp.Wpf.Mvvm;
using Len.Abp.Wpf.Navigation.Regions;
using Len.Abp.Wpf.Navigation.Regions.Behaviors;
using Volo.Abp.DependencyInjection;
using Xunit;


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
        var region = new Region("MainRegion", view.ViewName, view);
        provider.Add(region);

        region.Add(view);
        region.Activate(view);

        region.Behaviors.Add(nameof(RegionMemberLifetimeBehavior), new RegionMemberLifetimeBehavior(provider, new ViewScopeRegistry()));
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
        var region = new Region("MainRegion", "MainView");
        provider.Add(region);

        var view = new FakeView("demo", new LifetimeViewModel(keepAlive: true));
        region.Add(view);
        region.Activate(view);

        region.Behaviors.Add(nameof(RegionMemberLifetimeBehavior), new RegionMemberLifetimeBehavior(provider, new ViewScopeRegistry()));
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
        var region = new Region("MainRegion", "MainView");
        provider.Add(region);

        var view = new FakeView("demo", new object());
        region.Add(view);
        region.Activate(view);

        region.Behaviors.Add(nameof(RegionMemberLifetimeBehavior), new RegionMemberLifetimeBehavior(provider, new ViewScopeRegistry()));
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
        var region = new Region("MainRegion", "MainView");
        provider.Add(region);
        region.Behaviors.Add(nameof(RegionMemberLifetimeBehavior), new RegionMemberLifetimeBehavior(provider, new ViewScopeRegistry()));

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
        var region = new Region("MainRegion", "MainView");
        provider.Add(region);

        var viewModel = new DisposableLifetimeViewModel(keepAlive: false);
        var view = new DisposableFakeView("demo", viewModel);
        region.Add(view);
        region.Activate(view);

        region.Behaviors.Add(nameof(RegionMemberLifetimeBehavior), new RegionMemberLifetimeBehavior(provider, new ViewScopeRegistry()));
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
        var region = new Region("MainRegion", "MainView");
        provider.Add(region);

        var viewModel = new DisposableLifetimeViewModel(keepAlive: true);
        var view = new DisposableFakeView("demo", viewModel);
        region.Add(view);
        region.Activate(view);

        region.Behaviors.Add(nameof(RegionMemberLifetimeBehavior), new RegionMemberLifetimeBehavior(provider, new ViewScopeRegistry()));
        region.Deactivate(view);

        Assert.False(view.IsDisposed);
        Assert.False(viewModel.IsDisposed);
    }

    /// <summary>
    /// 单例视图的生命周期归容器所有，切走后仍保留在区域中，供下次导航复用。
    /// </summary>
    [Fact]
    public void Deactivate_SingletonView_IsKeptAndNotDisposed()
    {
        var provider = new RegionProvider();
        var region = new Region("MainRegion", "MainView");
        provider.Add(region);

        var viewModel = new DisposableLifetimeViewModel(keepAlive: false);
        var view = new DisposableSingletonView("demo", viewModel);
        region.Add(view);
        region.Activate(view);

        region.Behaviors.Add(nameof(RegionMemberLifetimeBehavior), new RegionMemberLifetimeBehavior(provider, new ViewScopeRegistry()));
        region.Deactivate(view);

        Assert.Single(region.Views);
        Assert.False(view.IsDisposed);
    }

    /// <summary>
    /// 作用域视图不自动保活：每次导航由独立作用域创建新实例，保留旧实例只会让作用域无法释放。
    /// </summary>
    /// <remarks>
    /// 若把 Scoped 当作单例保留，旧实例的作用域永远不释放，每次导航还会多留一个视图。
    /// </remarks>
    [Fact]
    public void Deactivate_ScopedView_IsRemovedAndDisposed()
    {
        var provider = new RegionProvider();
        var region = new Region("MainRegion", "MainView");
        provider.Add(region);

        var viewModel = new DisposableLifetimeViewModel(keepAlive: false);
        var view = new DisposableScopedView("demo", viewModel);
        region.Add(view);
        region.Activate(view);

        region.Behaviors.Add(nameof(RegionMemberLifetimeBehavior), new RegionMemberLifetimeBehavior(provider, new ViewScopeRegistry()));
        region.Deactivate(view);

        Assert.Empty(region.Views);
        Assert.True(view.IsDisposed);
    }

    /// <summary>
    /// 视图是瞬态但视图模型是单例时，视图同样保留，避免单例视图模型被重复绑定到新视图。
    /// </summary>
    [Fact]
    public void Deactivate_SingletonViewModel_KeepsView()
    {
        var provider = new RegionProvider();
        var region = new Region("MainRegion", "MainView");
        provider.Add(region);

        var viewModel = new SingletonLifetimeViewModel();
        var view = new DisposableFakeView("demo", viewModel);
        region.Add(view);
        region.Activate(view);

        region.Behaviors.Add(nameof(RegionMemberLifetimeBehavior), new RegionMemberLifetimeBehavior(provider, new ViewScopeRegistry()));
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
        var rootRegion = new Region("MainRegion", "MainView");
        provider.Add(rootRegion);

        var rootView = new DisposableFakeView("root", new LifetimeViewModel(keepAlive: false));
        var childView = new DisposableFakeView("child", new LifetimeViewModel(keepAlive: false));
        var grandchildView = new DisposableFakeView("grandchild", new LifetimeViewModel(keepAlive: false));

        var childRegion = new Region("ChildRegion", "root", rootView);
        var grandchildRegion = new Region("GrandchildRegion", "child", childView);
        childRegion.Add(childView);
        grandchildRegion.Add(grandchildView);

        rootRegion.Add(rootView);
        rootRegion.Activate(rootView);
        provider.Add(childRegion);
        provider.Add(grandchildRegion);

        rootRegion.Behaviors.Add(nameof(RegionMemberLifetimeBehavior), new RegionMemberLifetimeBehavior(provider, new ViewScopeRegistry()));
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
        var rootRegion = new Region("MainRegion", "MainView");
        provider.Add(rootRegion);

        var rootView = new DisposableFakeView("root", new LifetimeViewModel(keepAlive: false));
        var singletonView = new DisposableSingletonView("child", new LifetimeViewModel(keepAlive: false));
        var childRegion = new Region("ChildRegion", "child", singletonView);

        rootRegion.Add(rootView);
        rootRegion.Activate(rootView);
        provider.Add(childRegion);

        rootRegion.Behaviors.Add(nameof(RegionMemberLifetimeBehavior), new RegionMemberLifetimeBehavior(provider, new ViewScopeRegistry()));
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
        var rootRegion = new Region("MainRegion", "MainView");
        provider.Add(rootRegion);

        var rootView = new FakeView("root", new LifetimeViewModel(keepAlive: true));
        var childRegion = new Region("ChildRegion", "root", rootView);

        rootRegion.Add(rootView);
        rootRegion.Activate(rootView);
        provider.Add(childRegion);

        rootRegion.Behaviors.Add(nameof(RegionMemberLifetimeBehavior), new RegionMemberLifetimeBehavior(provider, new ViewScopeRegistry()));
        rootRegion.Deactivate(rootView);

        Assert.Single(rootRegion.Views);
        Assert.Contains(childRegion, provider.Regions);
    }

    /// <summary>
    /// 对从未激活的视图调用 <see cref="IRegion.Deactivate"/> 是幂等空操作，不回收视图。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 回归：<c>ItemsControlRegionAdapter</c> 的文档曾把 <c>Deactivate</c> 列为回收手段，
    /// 但从未激活的视图失活是空操作——它本来就不在 <c>ActiveViews</c> 里，
    /// 不会触发拆除。文档已改为只推荐 <see cref="IRegion.Remove"/>。
    /// </para>
    /// <para>
    /// 这里同时锁定"不改变语义"这条决策：<c>Deactivate</c> 的职责是失活，
    /// 若改成"未激活即回收"，防御性地重复调用会误删视图。
    /// </para>
    /// </remarks>
    [Fact]
    public void Deactivate_NeverActivatedView_IsNoOp()
    {
        var provider = new RegionProvider();
        var region = new Region("MainRegion", "MainView");
        provider.Add(region);

        var view = new DisposableFakeView("demo", new LifetimeViewModel(keepAlive: false));
        region.Add(view);

        region.Behaviors.Add(nameof(RegionMemberLifetimeBehavior), new RegionMemberLifetimeBehavior(provider, new ViewScopeRegistry()));
        region.Deactivate(view);

        Assert.Single(region.Views);
        Assert.False(view.IsDisposed);
    }

    /// <summary>
    /// 显式 <see cref="IRegion.Remove"/> 是回收未激活视图的唯一途径。
    /// </summary>
    /// <remarks>
    /// 与上一条配对：<c>Deactivate</c> 不回收，<c>Remove</c> 回收。
    /// 使用者按 <c>ItemsControlRegionAdapter</c> 的文档回收多活动区域的视图时，
    /// 必须用 <c>Remove</c>。
    /// </remarks>
    [Fact]
    public void Remove_NeverActivatedView_DisposesIt()
    {
        var provider = new RegionProvider();
        var region = new Region("MainRegion", "MainView");
        provider.Add(region);

        var view = new DisposableFakeView("demo", new LifetimeViewModel(keepAlive: false));
        region.Add(view);

        region.Behaviors.Add(nameof(RegionMemberLifetimeBehavior), new RegionMemberLifetimeBehavior(provider, new ViewScopeRegistry()));
        region.Remove(view);

        Assert.Empty(region.Views);
        Assert.True(view.IsDisposed);
    }

    /// <summary>
    /// 显式 <see cref="IRegion.Remove"/> 无视保活声明，仍然释放视图。
    /// </summary>
    /// <remarks>
    /// 保活约束的是"失活时是否自动拆除"，不是"能否显式移除"。
    /// 视图离开区域后不会再被导航复用，保留它只是泄漏。
    /// </remarks>
    [Fact]
    public void Remove_KeepAliveView_StillDisposesIt()
    {
        var provider = new RegionProvider();
        var region = new Region("MainRegion", "MainView");
        provider.Add(region);

        var view = new DisposableFakeView("demo", new DisposableLifetimeViewModel(keepAlive: true));
        region.Add(view);
        region.Activate(view);

        region.Behaviors.Add(nameof(RegionMemberLifetimeBehavior), new RegionMemberLifetimeBehavior(provider, new ViewScopeRegistry()));
        region.Remove(view);

        Assert.Empty(region.Views);
        Assert.True(view.IsDisposed);
    }

    /// <summary>
    /// RemoveAll 走统一拆除路径：瞬态视图被释放，区域被清空。
    /// </summary>
    /// <remarks>
    /// 早期实现直接清空元数据，集合发出 Reset 通知，而行为只处理 Remove，
    /// 拆除与释放全部被跳过，留下整棵视图子树。
    /// </remarks>
    [Fact]
    public void RemoveAll_DisposesTransientViews()
    {
        var provider = new RegionProvider();
        var region = new Region("MainRegion", "MainView");
        provider.Add(region);

        var first = new DisposableFakeView("a", new DisposableLifetimeViewModel(keepAlive: false));
        var second = new DisposableFakeView("b", new DisposableLifetimeViewModel(keepAlive: false));
        region.Add(first);
        region.Add(second);
        region.Activate(first);

        region.Behaviors.Add(nameof(RegionMemberLifetimeBehavior), new RegionMemberLifetimeBehavior(provider, new ViewScopeRegistry()));
        region.RemoveAll();

        Assert.Empty(region.Views);
        Assert.Empty(region.ActiveViews);
        Assert.True(first.IsDisposed);
        Assert.True(second.IsDisposed);
    }

    /// <summary>
    /// RemoveAll 拆除每个视图拥有的嵌套区域子树。
    /// </summary>
    [Fact]
    public void RemoveAll_TearsDownNestedRegions()
    {
        var provider = new RegionProvider();
        var rootRegion = new Region("MainRegion", "MainView");
        provider.Add(rootRegion);

        var rootView = new DisposableFakeView("root", new LifetimeViewModel(keepAlive: false));
        var childView = new DisposableFakeView("child", new LifetimeViewModel(keepAlive: false));
        var childRegion = new Region("ChildRegion", "root", rootView);
        childRegion.Add(childView);

        rootRegion.Add(rootView);
        rootRegion.Activate(rootView);
        provider.Add(childRegion);

        rootRegion.Behaviors.Add(nameof(RegionMemberLifetimeBehavior), new RegionMemberLifetimeBehavior(provider, new ViewScopeRegistry()));
        rootRegion.RemoveAll();

        Assert.Empty(rootRegion.Views);
        Assert.True(rootView.IsDisposed);
        Assert.True(childView.IsDisposed);
        Assert.DoesNotContain(childRegion, provider.Regions);
    }

    /// <summary>
    /// RemoveAll 不释放容器管理的视图，但区域本身仍被清空。
    /// </summary>
    [Fact]
    public void RemoveAll_DoesNotDisposeContainerManagedViews()
    {
        var provider = new RegionProvider();
        var region = new Region("MainRegion", "MainView");
        provider.Add(region);

        var singleton = new DisposableSingletonView("a", new LifetimeViewModel(keepAlive: false));
        region.Add(singleton);
        region.Activate(singleton);

        region.Behaviors.Add(nameof(RegionMemberLifetimeBehavior), new RegionMemberLifetimeBehavior(provider, new ViewScopeRegistry()));
        region.RemoveAll();

        Assert.Empty(region.Views);
        Assert.False(singleton.IsDisposed);
    }

    /// <summary>
    /// RemoveAll 摘除保活视图拥有的嵌套区域，不留孤儿区域。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 回归：<c>TearDownAll</c> 对保活视图直接返回（不调
    /// <see cref="IRegionProvider.RemoveOwnedRegions"/>），而 <c>RemoveAllCore</c>
    /// 又无条件清空元数据。视图与区域的所有权关系断开后，嵌套区域留在提供程序里成为孤儿：
    /// <see cref="RegionFinder"/> 把所有者不在任何区域中的区域当作根视图区域，
    /// 导航因此可能命中用户根本看不到的实例。
    /// </para>
    /// <para>
    /// 这条路径由 <c>RegionBuilder.DiscardRegion</c>（区域改名重建）触发。
    /// </para>
    /// </remarks>
    [Fact]
    public void RemoveAll_KeepAliveView_DetachesItsNestedRegion()
    {
        var provider = new RegionProvider();
        var region = new Region("MainRegion", "MainView");
        provider.Add(region);

        var keepAlive = new KeepAliveCountingFakeView("keep-alive");
        var nestedRegion = new Region("NestedRegion", keepAlive.ViewName, keepAlive);
        nestedRegion.Add(new FakeView("nested"));
        provider.Add(nestedRegion);

        region.Add(keepAlive);
        region.Activate(keepAlive);

        region.Behaviors.Add(
            nameof(RegionMemberLifetimeBehavior),
            new RegionMemberLifetimeBehavior(provider, new ViewScopeRegistry())
        );

        region.RemoveAll();

        // 区域被清空，保活视图不被释放，但它拥有的嵌套区域必须摘除。
        Assert.Empty(region.Views);
        Assert.Equal(0, keepAlive.DisposeCount);
        Assert.DoesNotContain(nestedRegion, provider.Regions);
    }

    /// <summary>
    /// RemoveAll 摘除容器管理的视图拥有的嵌套区域。
    /// </summary>
    /// <remarks>
    /// 单例视图同样被 <c>TearDown</c> 跳过，与保活视图走同一条遗漏路径。
    /// </remarks>
    [Fact]
    public void RemoveAll_SingletonView_DetachesItsNestedRegion()
    {
        var provider = new RegionProvider();
        var region = new Region("MainRegion", "MainView");
        provider.Add(region);

        var singleton = new DisposableSingletonView("singleton", new LifetimeViewModel(keepAlive: false));
        var nestedRegion = new Region("NestedRegion", "singleton", singleton);
        nestedRegion.Add(new FakeView("nested"));
        provider.Add(nestedRegion);

        region.Add(singleton);
        region.Activate(singleton);

        region.Behaviors.Add(
            nameof(RegionMemberLifetimeBehavior),
            new RegionMemberLifetimeBehavior(provider, new ViewScopeRegistry())
        );

        region.RemoveAll();

        Assert.Empty(region.Views);
        Assert.False(singleton.IsDisposed);
        Assert.DoesNotContain(nestedRegion, provider.Regions);
    }

    /// <summary>
    /// 不保活视图的嵌套区域在 RemoveAll 后同样不残留。
    /// </summary>
    /// <remarks>
    /// 与上面两条配对：不保活路径由 <c>TearDownSubtree</c> 摘除，
    /// 保活与单例路径由 <c>DetachOwnedRegions</c> 摘除，两条路径都必须覆盖。
    /// </remarks>
    [Fact]
    public void RemoveAll_TransientView_DetachesItsNestedRegion()
    {
        var provider = new RegionProvider();
        var region = new Region("MainRegion", "MainView");
        provider.Add(region);

        var transient = new CountingFakeView("transient");
        var nestedRegion = new Region("NestedRegion", transient.ViewName, transient);
        nestedRegion.Add(new FakeView("nested"));
        provider.Add(nestedRegion);

        region.Add(transient);
        region.Activate(transient);

        region.Behaviors.Add(
            nameof(RegionMemberLifetimeBehavior),
            new RegionMemberLifetimeBehavior(provider, new ViewScopeRegistry())
        );

        region.RemoveAll();

        Assert.Empty(region.Views);
        Assert.Equal(1, transient.DisposeCount);
        Assert.DoesNotContain(nestedRegion, provider.Regions);
    }

    /// <summary>
    /// 行为未附加时 RemoveAll 仍能清空区域。
    /// </summary>
    [Fact]
    public void RemoveAll_WithoutBehavior_ClearsRegion()
    {
        var region = new Region("MainRegion", "MainView");
        region.Add(new FakeView("a"));
        region.Add(new FakeView("b"));

        region.RemoveAll();

        Assert.Empty(region.Views);
    }

    /// <summary>
    /// 嵌套视图及其视图模型只被释放一次。
    /// </summary>
    /// <remarks>
    /// 回归：<c>TearDownSubtree</c> 递归调用后外层又释放了一次，
    /// 导致每个嵌套视图与视图模型都被 <c>Dispose</c> 两次。
    /// 真实视图模型第二次释放会抛 <see cref="ObjectDisposedException"/>，
    /// 而拆除发生在导航过程中，异常会让整次导航被判定为失败。
    /// </remarks>
    [Fact]
    public void Remove_NestedViews_DisposesEachExactlyOnce()
    {
        var provider = new RegionProvider();
        var rootRegion = new SingleActiveRegion("MainRegion", "MainView");
        provider.Add(rootRegion);

        var level1 = new CountingFakeView("level1");
        var level2 = new CountingFakeView("level2");
        var level3 = new CountingFakeView("level3");

        // 三层嵌套：level1 拥有 Level2Region，level2 拥有 Level3Region。
        var level3Region = new Region("Level3Region", level2.ViewName, level2);
        level3Region.Add(level3);
        provider.Add(level3Region);

        var level2Region = new Region("Level2Region", level1.ViewName, level1);
        level2Region.Add(level2);
        provider.Add(level2Region);

        rootRegion.Add(level1);
        rootRegion.Activate(level1);

        rootRegion.Behaviors.Add(
            nameof(RegionMemberLifetimeBehavior),
            new RegionMemberLifetimeBehavior(provider, new ViewScopeRegistry())
        );

        rootRegion.Remove(level1);

        Assert.Equal(1, level1.DisposeCount);
        Assert.Equal(1, level2.DisposeCount);
        Assert.Equal(1, level3.DisposeCount);
        Assert.Equal(1, level1.ViewModel.DisposeCount);
        Assert.Equal(1, level2.ViewModel.DisposeCount);
        Assert.Equal(1, level3.ViewModel.DisposeCount);
    }

    /// <summary>
    /// RemoveAll 同样只释放每个嵌套视图一次。
    /// </summary>
    /// <remarks>
    /// 与上一条配对：<c>Remove</c> 与 <c>RemoveAll</c> 走同一条拆除路径，
    /// 修复必须同时覆盖两者。
    /// </remarks>
    [Fact]
    public void RemoveAll_NestedViews_DisposesEachExactlyOnce()
    {
        var provider = new RegionProvider();
        var rootRegion = new SingleActiveRegion("MainRegion", "MainView");
        provider.Add(rootRegion);

        var rootView = new CountingFakeView("root");
        var childView = new CountingFakeView("child");
        var childRegion = new Region("ChildRegion", rootView.ViewName, rootView);
        childRegion.Add(childView);
        provider.Add(childRegion);

        rootRegion.Add(rootView);
        rootRegion.Activate(rootView);

        rootRegion.Behaviors.Add(
            nameof(RegionMemberLifetimeBehavior),
            new RegionMemberLifetimeBehavior(provider, new ViewScopeRegistry())
        );

        rootRegion.RemoveAll();

        Assert.Equal(1, rootView.DisposeCount);
        Assert.Equal(1, childView.DisposeCount);
        Assert.Equal(1, rootView.ViewModel.DisposeCount);
        Assert.Equal(1, childView.ViewModel.DisposeCount);
    }

    /// <summary>
    /// 按快照遍历多个区域时，嵌套视图仍只被释放一次。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 回归残余路径：<see cref="RegionTeardownService"/> 先取
    /// <c>provider.Regions.ToArray()</c> 快照再逐个 <c>RemoveAll</c>。
    /// 父区域拆除子树时会把嵌套区域从提供程序摘除并释放其中的视图，
    /// 但快照里仍持有该嵌套区域，随后它自己的 <c>RemoveAll</c> 会再次拆除同一批视图。
    /// </para>
    /// <para>
    /// 这条路径跨越两个不同的行为实例（父区域的与嵌套区域的），
    /// 因此去重不能放在实例字段上——必须按实例全局记录。
    /// </para>
    /// </remarks>
    [Fact]
    public void RemoveAll_SnapshotOfMultipleRegions_DisposesNestedViewOnce()
    {
        var provider = new RegionProvider();
        var rootRegion = new SingleActiveRegion("MainRegion", "MainView");
        provider.Add(rootRegion);

        var rootView = new CountingFakeView("root");
        var childView = new CountingFakeView("child");
        var childRegion = new Region("ChildRegion", rootView.ViewName, rootView);
        childRegion.Add(childView);
        provider.Add(childRegion);

        rootRegion.Add(rootView);
        rootRegion.Activate(rootView);

        rootRegion.Behaviors.Add(
            nameof(RegionMemberLifetimeBehavior),
            new RegionMemberLifetimeBehavior(provider, new ViewScopeRegistry())
        );
        childRegion.Behaviors.Add(
            nameof(RegionMemberLifetimeBehavior),
            new RegionMemberLifetimeBehavior(provider, new ViewScopeRegistry())
        );

        // 与 RegionTeardownService 相同的遍历方式：先快照，再逐个清空。
        foreach (var region in provider.Regions.ToArray())
        {
            region.RemoveAll();
        }

        Assert.Equal(1, rootView.DisposeCount);
        Assert.Equal(1, childView.DisposeCount);
        Assert.Equal(1, rootView.ViewModel.DisposeCount);
        Assert.Equal(1, childView.ViewModel.DisposeCount);
    }

    /// <summary>
    /// 父视图被拆除时，声明保活的嵌套视图随子树一起释放。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 锁定语义决策：<b>保活只对区域直接承载的视图生效</b>。
    /// 父视图被拆除时，它拥有的区域会先被 <see cref="IRegionProvider.RemoveOwnedRegions"/>
    /// 整体摘除，承载嵌套视图的区域从此不可达。此时跳过释放只会留下
    /// 既无法导航到、也不会被回收的孤儿实例，因此必须一并释放。
    /// </para>
    /// <para>
    /// 需要跨导航保留嵌套视图时，应把保活声明放在父视图上——
    /// 那样整棵子树都不会进入拆除路径（见 <c>Deactivate_KeepAliveView_KeepsNestedRegions</c>）。
    /// </para>
    /// </remarks>
    [Fact]
    public void Remove_KeepAliveNestedView_IsReleasedWithParent()
    {
        var provider = new RegionProvider();
        var rootRegion = new SingleActiveRegion("MainRegion", "MainView");
        provider.Add(rootRegion);

        var parent = new CountingFakeView("parent");
        var keepAliveChild = new KeepAliveCountingFakeView("nested");
        var nestedRegion = new Region("NestedRegion", parent.ViewName, parent);
        nestedRegion.Add(keepAliveChild);
        provider.Add(nestedRegion);

        rootRegion.Add(parent);
        rootRegion.Activate(parent);

        rootRegion.Behaviors.Add(
            nameof(RegionMemberLifetimeBehavior),
            new RegionMemberLifetimeBehavior(provider, new ViewScopeRegistry())
        );

        rootRegion.Remove(parent);

        // 承载它的区域已被摘除，视图必须随之释放，不能留下孤儿。
        Assert.Equal(1, keepAliveChild.DisposeCount);
        Assert.Equal(1, keepAliveChild.ViewModel.DisposeCount);
        Assert.DoesNotContain(nestedRegion, provider.Regions);
    }

    private sealed class LifetimeViewModel(bool keepAlive) : IRegionMemberLifetime
    {
        public bool KeepAlive { get; } = keepAlive;
    }

    /// <summary>
    /// 记录 <see cref="Dispose"/> 调用次数的视图模型。
    /// </summary>
    /// <remarks>
    /// 只记录"是否释放过"的替身发现不了重复释放：第二次 <c>Dispose</c> 会静默通过。
    /// 真实视图模型（例如持有 <c>CancellationTokenSource</c> 的实现）第二次释放会抛
    /// <see cref="ObjectDisposedException"/>，而拆除发生在导航过程中，异常会让导航失败。
    /// </remarks>
    private sealed class CountingLifetimeViewModel : IRegionMemberLifetime, IDisposable
    {
        public int DisposeCount { get; private set; }

        public bool KeepAlive => false;

        public void Dispose() => DisposeCount++;
    }

    /// <summary>
    /// 记录 <see cref="Dispose"/> 调用次数的视图。
    /// </summary>
    private sealed class CountingFakeView(string viewName) : IView, IDisposable
    {
        public int DisposeCount { get; private set; }

        public CountingLifetimeViewModel ViewModel { get; } = new();

        object IView.ViewModel => ViewModel;

        public string ViewName { get; } = viewName;

        public void Dispose() => DisposeCount++;
    }

    /// <summary>
    /// 声明保活、并记录 <see cref="Dispose"/> 调用次数的视图。
    /// </summary>
    /// <remarks>
    /// 用于锁定"保活只对区域直接承载的视图生效"：作为嵌套视图时，
    /// 承载它的区域会先被摘除，因此它必须随子树一起释放。
    /// </remarks>
    private sealed class KeepAliveCountingFakeView(string viewName) : IView, IDisposable
    {
        public int DisposeCount { get; private set; }

        public KeepAliveCountingViewModel ViewModel { get; } = new();

        object IView.ViewModel => ViewModel;

        public string ViewName { get; } = viewName;

        public void Dispose() => DisposeCount++;
    }

    /// <summary>
    /// 声明保活、并记录 <see cref="Dispose"/> 调用次数的视图模型。
    /// </summary>
    private sealed class KeepAliveCountingViewModel : IRegionMemberLifetime, IDisposable
    {
        public int DisposeCount { get; private set; }

        public bool KeepAlive => true;

        public void Dispose() => DisposeCount++;
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
