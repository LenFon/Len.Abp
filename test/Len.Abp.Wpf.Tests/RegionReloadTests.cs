using Len.Abp.Wpf.Mvvm;
using Len.Abp.Wpf.Navigation;
using Len.Abp.Wpf.Navigation.Regions;
using Len.Abp.Wpf.Navigation.Regions.Behaviors;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using Volo.Abp.DependencyInjection;
using Xunit;

namespace Len.Abp.Wpf.Tests;

/// <summary>
/// 覆盖：区域宿主卸载后重新加载时区域必须仍然可用。
/// </summary>
/// <remarks>
/// <para>
/// 适配器在初始化时把控件状态单向指向区域内容（<c>Content</c> / <c>ItemsSource</c>），
/// 卸载不会清除这些引用。曾有一版实现在卸载摘除区域后重新加载时重建区域，
/// 而重建时适配器看到控件状态仍被旧视图占用，直接抛 <see cref="RegionCreationException"/>——
/// 区域从此永久失效。
/// </para>
/// <para>
/// 修复后卸载只摘除、重新加载复用同一个区域，因此控件状态与区域始终一致。
/// 这里用真实的适配器与真实的 <see cref="RegionBuilder"/>，覆盖"组合才出错"的路径。
/// </para>
/// </remarks>
public class RegionReloadTests
{
    /// <summary>
    /// <see cref="ContentControl"/> 区域卸载后重新加载，区域恢复且不抛异常。
    /// </summary>
    [Fact]
    public void ContentControl_UnloadedThenLoaded_RegionIsRestored()
    {
        Sta.Run(() =>
        {
            var (builder, provider) = CreateBuilder();
            var (view, host) = CreateContentHost();

            builder.Build(host);
            RegionHost.SetName(host, "MainRegion");
            RaiseLoaded(host);

            var region = Assert.Single(provider.Regions);
            var inner = new FakeView("inner");
            region.Add(inner);
            Assert.Same(inner, host.Content);

            RaiseUnloaded(host);
            Assert.Empty(provider.Regions);

            RaiseLoaded(host);

            Assert.Same(region, Assert.Single(provider.Regions));
            Assert.Same(inner, host.Content);
            Assert.Same(view, LogicalTreeHelper.GetParent(host));
        });
    }

    /// <summary>
    /// <see cref="TabControl"/> 区域卸载后重新加载，选项卡与区域都恢复。
    /// </summary>
    [Fact]
    public void TabControl_UnloadedThenLoaded_RegionIsRestored()
    {
        Sta.Run(() =>
        {
            var (builder, provider) = CreateBuilder();
            var view = new FakeViewControl { ViewName = "MainView" };
            var host = new TabControl();
            view.Content = host;

            builder.Build(host);
            RegionHost.SetName(host, "MainRegion");
            RaiseLoaded(host);

            var region = Assert.Single(provider.Regions);
            region.Add(new FakeView("inner"));
            Assert.Single(GetTabs(host));

            RaiseUnloaded(host);
            Assert.Empty(provider.Regions);

            RaiseLoaded(host);

            Assert.Same(region, Assert.Single(provider.Regions));
            Assert.Single(GetTabs(host));
        });
    }

    /// <summary>
    /// 反复卸载重载不会累积区域，也不会重复创建。
    /// </summary>
    [Fact]
    public void ContentControl_RepeatedReload_DoesNotAccumulateRegions()
    {
        Sta.Run(() =>
        {
            var (builder, provider) = CreateBuilder();
            var (_, host) = CreateContentHost();

            builder.Build(host);
            RegionHost.SetName(host, "MainRegion");

            for (var round = 0; round < 5; round++)
            {
                RaiseLoaded(host);
                Assert.Single(provider.Regions);

                RaiseUnloaded(host);
                Assert.Empty(provider.Regions);
            }

            RaiseLoaded(host);
            Assert.Single(provider.Regions);
        });
    }

    /// <summary>
    /// 区域中的保活视图在重载后仍留在区域内，不被拆除。
    /// </summary>
    /// <remarks>
    /// 复用区域而不是重建，保活视图自然保留。若改成"卸载即拆除"，保活语义会被破坏。
    /// </remarks>
    [Fact]
    public void ContentControl_Reload_KeepsKeepAliveView()
    {
        Sta.Run(() =>
        {
            var (builder, provider) = CreateBuilder();
            var (_, host) = CreateContentHost();

            builder.Build(host);
            RegionHost.SetName(host, "MainRegion");
            RaiseLoaded(host);

            var region = Assert.Single(provider.Regions);
            var keepAlive = new FakeView("keep-alive", new KeepAliveViewModel());
            region.Add(keepAlive);
            region.Activate(keepAlive);

            RaiseUnloaded(host);
            RaiseLoaded(host);

            Assert.Contains(keepAlive, region.Views);
            Assert.Same(keepAlive, host.Content);
        });
    }

    /// <summary>
    /// 卸载期间区域名称变更时，按新名称重建区域，且控件状态被正确解除占用。
    /// </summary>
    /// <remarks>
    /// 复用只对同名区域成立。改名后旧区域不再适用，必须先清空区域并让适配器
    /// 解除对控件的占用，否则重建时适配器会看到残留的 <c>Content</c> 而抛异常。
    /// </remarks>
    [Fact]
    public void ContentControl_RenamedWhileUnloaded_RebuildsWithNewName()
    {
        Sta.Run(() =>
        {
            var (builder, provider) = CreateBuilder();
            var (_, host) = CreateContentHost();

            builder.Build(host);
            RegionHost.SetName(host, "MainRegion");
            RaiseLoaded(host);

            var original = Assert.Single(provider.Regions);
            original.Add(new FakeView("inner"));

            RaiseUnloaded(host);
            RegionHost.SetName(host, "OtherRegion");
            RaiseLoaded(host);

            var rebuilt = Assert.Single(provider.Regions);
            Assert.NotSame(original, rebuilt);
            Assert.Equal("OtherRegion", rebuilt.Name);
            Assert.Empty(rebuilt.Views);
            Assert.Null(host.Content);
        });
    }

    /// <summary>
    /// 卸载期间改名重建时，保活视图拥有的嵌套区域不残留在提供程序中。
    /// </summary>
    /// <remarks>
    /// 回归：<c>DiscardRegion</c> 调用 <c>region.RemoveAll()</c>，而保活视图不会被
    /// <c>TearDown</c> 移除，因此它的嵌套区域从未被 <c>RemoveOwnedRegions</c> 摘除。
    /// 清空元数据后视图与区域的所有权关系断开，区域留在提供程序里成为孤儿——
    /// <see cref="RegionFinder"/> 会把所有者不在任何区域中的区域当作根视图区域，
    /// 导航因此可能命中用户根本看不到的实例。
    /// </remarks>
    [Fact]
    public void ContentControl_RenamedWhileUnloaded_DetachesKeepAliveNestedRegion()
    {
        Sta.Run(() =>
        {
            var (builder, provider) = CreateBuilder();
            var (_, host) = CreateContentHost();

            builder.Build(host);
            RegionHost.SetName(host, "MainRegion");
            RaiseLoaded(host);

            var original = Assert.Single(provider.Regions);

            // 保活视图拥有一个嵌套区域，模拟"页面上有子区域"的常见结构。
            var keepAlive = new FakeView("keep-alive", new KeepAliveViewModel());
            var nestedRegion = new Region("NestedRegion", keepAlive.ViewName, keepAlive);
            nestedRegion.Add(new FakeView("nested"));
            provider.Add(nestedRegion);
            original.Add(keepAlive);
            original.Activate(keepAlive);

            RaiseUnloaded(host);
            RegionHost.SetName(host, "OtherRegion");
            RaiseLoaded(host);

            // 旧区域被丢弃，它承载的保活视图的嵌套区域也必须一并摘除。
            Assert.DoesNotContain(nestedRegion, provider.Regions);
            Assert.Single(provider.Regions);
        });
    }

    /// <summary>
    /// 卸载期间改名重建时，旧区域中的保活视图被释放。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 回归：<c>DiscardRegion</c> 调用 <c>region.RemoveAll()</c>，而 <c>RemoveAll</c>
    /// 走的是普通拆除路径，保活视图会被跳过——只摘掉不释放。区域被丢弃后不会再回到
    /// 提供程序，其中的视图也不会再被任何区域复用，跳过释放只会让作用域与可释放资源
    /// 永远没人回收。
    /// </para>
    /// <para>
    /// 修法是丢弃时走强制拆除（<c>RemoveAll(force: true)</c>）。普通清空（宿主退出时
    /// 由 <see cref="RegionTeardownService"/> 遍历）保留保活语义，避免误伤仍可复用的实例。
    /// </para>
    /// </remarks>
    [Fact]
    public void ContentControl_RenamedWhileUnloaded_DisposesKeepAliveView()
    {
        Sta.Run(() =>
        {
            var (builder, provider) = CreateBuilder();
            var (_, host) = CreateContentHost();

            builder.Build(host);
            RegionHost.SetName(host, "MainRegion");
            RaiseLoaded(host);

            var original = Assert.Single(provider.Regions);

            var viewModel = new DisposableKeepAliveViewModel();
            var keepAlive = new DisposableKeepAliveView("keep-alive", viewModel);
            original.Add(keepAlive);
            original.Activate(keepAlive);

            RaiseUnloaded(host);
            RegionHost.SetName(host, "OtherRegion");
            RaiseLoaded(host);

            // 区域被丢弃，保活声明失去意义：视图与视图模型都必须释放。
            Assert.True(keepAlive.IsDisposed);
            Assert.True(viewModel.IsDisposed);
        });
    }

    /// <summary>
    /// 普通清空（非丢弃）仍保留保活视图，不释放。
    /// </summary>
    /// <remarks>
    /// 对照组：强制拆除只用于区域被永久丢弃。宿主退出时的遍历清空不能顺手释放
    /// 保活视图——那会破坏"保活"的语义，让仍可复用的实例提前失效。
    /// </remarks>
    [Fact]
    public void RemoveAll_WithoutForce_KeepsKeepAliveView()
    {
        var provider = new RegionProvider();
        var region = new Region("MainRegion", "MainView");
        provider.Add(region);

        region.Behaviors.Add(
            nameof(RegionMemberLifetimeBehavior),
            new RegionMemberLifetimeBehavior(provider, new ViewScopeRegistry())
        );

        var viewModel = new DisposableKeepAliveViewModel();
        var keepAlive = new DisposableKeepAliveView("keep-alive", viewModel);
        region.Add(keepAlive);
        region.Activate(keepAlive);

        region.RemoveAll();

        Assert.False(keepAlive.IsDisposed);
        Assert.False(viewModel.IsDisposed);
        Assert.Empty(region.Views);
    }

    /// <summary>
    /// 卸载期间改名后重建 <see cref="TabControl"/> 区域，选项卡集合被解除占用。
    /// </summary>
    [Fact]
    public void TabControl_RenamedWhileUnloaded_RebuildsWithNewName()
    {
        Sta.Run(() =>
        {
            var (builder, provider) = CreateBuilder();
            var view = new FakeViewControl { ViewName = "MainView" };
            var host = new TabControl();
            view.Content = host;

            builder.Build(host);
            RegionHost.SetName(host, "MainRegion");
            RaiseLoaded(host);

            var original = Assert.Single(provider.Regions);
            original.Add(new FakeView("inner"));

            RaiseUnloaded(host);
            RegionHost.SetName(host, "OtherRegion");
            RaiseLoaded(host);

            var rebuilt = Assert.Single(provider.Regions);
            Assert.NotSame(original, rebuilt);
            Assert.Equal("OtherRegion", rebuilt.Name);
            Assert.Empty(GetTabs(host));
        });
    }

    /// <summary>
    /// 改名重建后，旧区域不应再驱动控件，用户切换选项卡也不应抛异常。
    /// </summary>
    /// <remarks>
    /// 回归：适配器在 <c>Adapt</c> 里订阅了区域的集合变更与控件的 <c>SelectionChanged</c>，
    /// 而 <c>Detach</c> 只清空 <c>ItemsSource</c>。旧区域被丢弃后处理器仍挂在控件上，
    /// 用户切换选项卡会对已清空的旧区域调用 <c>Activate</c> 并抛
    /// <see cref="ArgumentException"/>；控件持有闭包也让旧区域与视图无法回收。
    /// </remarks>
    [Fact]
    public void TabControl_RenamedWhileUnloaded_OldRegionStopsDrivingControl()
    {
        Sta.Run(() =>
        {
            var (builder, provider) = CreateBuilder();
            var view = new FakeViewControl { ViewName = "MainView" };
            var host = new TabControl();
            view.Content = host;

            builder.Build(host);
            RegionHost.SetName(host, "MainRegion");
            RaiseLoaded(host);

            var original = Assert.Single(provider.Regions);
            original.Add(new FakeView("inner"));

            RaiseUnloaded(host);
            RegionHost.SetName(host, "OtherRegion");
            RaiseLoaded(host);

            // 旧区域已被丢弃：新增视图不应再出现在控件里。
            original.Add(new FakeView("late"));
            Assert.Empty(GetTabs(host));

            // 用户切换选项卡：旧区域的 SelectionChanged 处理器必须已解除。
            var rebuilt = Assert.Single(provider.Regions);
            rebuilt.Add(new FakeView("page"));
            host.SelectedIndex = 0;
        });
    }

    /// <summary>
    /// 使用者自己设置 <c>Content</c> 时仍抛异常，守卫不能被削弱。
    /// </summary>
    /// <remarks>
    /// 修复只解除"适配器自己写入"的状态。使用者显式占用控件时，
    /// 静默覆盖会丢掉他们的内容，因此必须继续报错。
    /// </remarks>
    [Fact]
    public void ContentControl_UserSetContent_StillThrows()
    {
        Sta.Run(() =>
        {
            var (builder, _) = CreateBuilder();
            var (_, host) = CreateContentHost();
            host.Content = new object();

            builder.Build(host);
            RegionHost.SetName(host, "MainRegion");

            var ex = Assert.Throws<RegionCreationException>(() => RaiseLoaded(host));

            Assert.IsType<ArgumentException>(ex.InnerException);
        });
    }

    private static (RegionBuilder Builder, RegionProvider Provider) CreateBuilder()
    {
        var provider = new RegionProvider();
        var adapter = new RealAdapter(provider);
        var builder = new RegionBuilder(new StubAdapterFinder(adapter), provider);

        // RegionHost.SetName 会从静态提供程序解析 IRegionBuilder，这里注册同一个实例。
        var services = new ServiceCollection();
        services.AddSingleton<IRegionBuilder>(builder);
        Len.Abp.Core.AbpServiceProvider.Initialize(services.BuildServiceProvider());

        return (builder, provider);
    }

    private static (FakeViewControl View, ContentControl Host) CreateContentHost()
    {
        var view = new FakeViewControl { ViewName = "MainView" };
        var host = new ContentControl();
        view.Content = host;

        return (view, host);
    }

    private static void RaiseLoaded(FrameworkElement element) =>
        element.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

    private static void RaiseUnloaded(FrameworkElement element) =>
        element.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));

    private static List<TabItem> GetTabs(TabControl tabControl) => [.. tabControl.Items.Cast<TabItem>()];

    /// <summary>
    /// 按控件类型选用真实适配器，并附加注册与保活行为。
    /// </summary>
    /// <remarks>
    /// 必须同时转发 <see cref="IRegionAdapterDetach"/>：改名重建依赖它解除控件占用，
    /// 只实现 <see cref="IRegionAdapter"/> 会让重建路径测不到。
    /// </remarks>
    private sealed class RealAdapter(IRegionProvider provider) : IRegionAdapter, IRegionAdapterDetach
    {
        private readonly ContentControlRegionAdapter _contentAdapter = new(
            new AbpLazyServiceProvider(new ServiceCollection().AddOptions().BuildServiceProvider())
        );

        private readonly TabControlRegionAdapter _tabAdapter = new(
            new AbpLazyServiceProvider(new ServiceCollection().AddOptions().BuildServiceProvider())
        );

        public IRegion Initialize(object regionTarget, string regionName, IView ownerView)
        {
            var adapter = regionTarget is TabControl ? (IRegionAdapter)_tabAdapter : _contentAdapter;
            var region = adapter.Initialize(regionTarget, regionName, ownerView);

            region.Behaviors.Add(
                nameof(RegionProviderRegistrationBehavior),
                new RegionProviderRegistrationBehavior(provider)
            );
            region.Behaviors.Add(
                nameof(RegionMemberLifetimeBehavior),
                new RegionMemberLifetimeBehavior(provider, new ViewScopeRegistry())
            );

            return region;
        }

        public void Detach(IRegion region, object regionTarget) => Select(regionTarget).Detach(region, regionTarget);

        private IRegionAdapterDetach Select(object regionTarget) =>
            regionTarget is TabControl ? _tabAdapter : _contentAdapter;
    }

    private sealed class StubAdapterFinder(IRegionAdapter adapter) : IRegionAdapterFinder
    {
        public IRegionAdapter FindByControlType(Type controlType) => adapter;
    }

    private sealed class KeepAliveViewModel : IRegionMemberLifetime
    {
        public bool KeepAlive => true;
    }

    /// <summary>
    /// 声明保活且可释放的视图模型。
    /// </summary>
    private sealed class DisposableKeepAliveViewModel : IRegionMemberLifetime, IDisposable
    {
        public bool KeepAlive => true;

        public bool IsDisposed { get; private set; }

        public void Dispose() => IsDisposed = true;
    }

    /// <summary>
    /// 声明保活且可释放的视图。
    /// </summary>
    private sealed class DisposableKeepAliveView(string viewName, object viewModel) : IView, IDisposable
    {
        public object ViewModel { get; } = viewModel;

        public string ViewName { get; } = viewName;

        public bool IsDisposed { get; private set; }

        public void Dispose() => IsDisposed = true;
    }

    private sealed class FakeViewControl : ContentControl, IView
    {
        public object ViewModel { get; } = new object();

        public string ViewName { get; init; } = string.Empty;
    }
}
