using Len.Abp.Wpf.Mvvm;
using Len.Abp.Wpf.Navigation;
using Len.Abp.Wpf.Navigation.Regions;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using Xunit;


namespace Len.Abp.Wpf.Tests;

/// <summary>
/// 覆盖：重复设置区域名称不会创建重复区域。
/// </summary>
/// <remarks>
/// 每次设置 <c>Region.Name</c> 都会解析一个新的瞬态 <see cref="IRegionBuilder"/>，
/// 因此去重必须按元素而不是按 builder 实例。这里注册真实的 <see cref="RegionBuilder"/>，
/// 让测试覆盖"多次解析 → 多次 Build"这条真实路径。
/// </remarks>
[Collection(GlobalServiceProviderCollection.Name)]
public class RegionBuilderDeduplicationTests
{
    /// <summary>
    /// 同一元素多次设置名称后只创建一个区域。
    /// </summary>
    [Fact]
    public void Build_SetNameTwice_CreatesRegionOnce()
    {
        Sta.Run(() =>
        {
            var adapter = new RecordingAdapter();
            InitializeStaticProvider(adapter);
            var host = CreateHost();

            RegionHost.SetName(host, "MainRegion");
            RegionHost.SetName(host, "MainRegion");

            host.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

            Assert.Equal(1, adapter.InitializeCount);
            Assert.Equal("MainRegion", adapter.RegionName);
        });
    }

    /// <summary>
    /// 名称从 A 改为 B 时只创建一个区域，且使用最终名称。
    /// </summary>
    /// <remarks>
    /// 首次设置时元素尚未加载，因此不会创建区域；改名为 B 后 Loaded 触发，
    /// 只应创建 B 的区域。这覆盖了"名称变更"这条路径。
    /// </remarks>
    [Fact]
    public void Build_SetNameTwiceDifferentValues_CreatesRegionWithFinalName()
    {
        Sta.Run(() =>
        {
            var adapter = new RecordingAdapter();
            InitializeStaticProvider(adapter);
            var host = CreateHost();

            RegionHost.SetName(host, "RegionA");
            RegionHost.SetName(host, "RegionB");

            host.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

            Assert.Equal(1, adapter.InitializeCount);
            Assert.Equal("RegionB", adapter.RegionName);
        });
    }

    /// <summary>
    /// 已加载后再设置名称不会创建第二个区域。
    /// </summary>
    [Fact]
    public void Build_SetNameAfterLoaded_DoesNotCreateSecondRegion()
    {
        Sta.Run(() =>
        {
            var adapter = new RecordingAdapter();
            InitializeStaticProvider(adapter);
            var host = CreateHost();

            RegionHost.SetName(host, "MainRegion");
            host.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

            RegionHost.SetName(host, "OtherRegion");
            host.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

            Assert.Equal(1, adapter.InitializeCount);
            Assert.Equal("MainRegion", adapter.RegionName);
        });
    }

    /// <summary>
    /// 不同元素各自创建区域，去重只针对同一元素。
    /// </summary>
    [Fact]
    public void Build_DifferentElements_EachCreatesRegion()
    {
        Sta.Run(() =>
        {
            var adapter = new RecordingAdapter();
            InitializeStaticProvider(adapter);

            var first = CreateHost();
            var second = CreateHost();

            RegionHost.SetName(first, "RegionA");
            RegionHost.SetName(second, "RegionB");

            first.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
            second.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

            Assert.Equal(2, adapter.InitializeCount);
        });
    }

    /// <summary>
    /// 同一元素重复设置名称后，提供程序中不出现重复区域。
    /// </summary>
    /// <remarks>
    /// 用真实的 <see cref="RegionProvider"/> 与注册行为验证最终结果，
    /// 而不只断言适配器被调用一次。
    /// </remarks>
    [Fact]
    public void Build_SetNameTwice_ProviderHasSingleRegion()
    {
        Sta.Run(() =>
        {
            var provider = new RegionProvider();
            InitializeStaticProvider(new RegisteringAdapter(provider), provider);
            var host = CreateHost();

            RegionHost.SetName(host, "MainRegion");
            RegionHost.SetName(host, "MainRegion");

            host.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

            Assert.Single(provider.Regions);
            Assert.Equal("MainRegion", provider.Regions[0].Name);
        });
    }

    /// <summary>
    /// 元素卸载后重新加载时，旧区域被摘除，提供程序中不出现同名区域。
    /// </summary>
    /// <remarks>
    /// 回归：此前的修复让 <c>Unloaded</c> 重置"本轮已创建"标记，
    /// 重新 <c>Loaded</c> 会再建一个区域并注册。旧区域若不摘除会永久残留，
    /// 且 <see cref="RegionFinder"/> 同名时返回最旧的那个，导航可能命中陈旧实例。
    /// </remarks>
    [Fact]
    public void Build_UnloadedThenLoaded_ProviderHasSingleRegion()
    {
        Sta.Run(() =>
        {
            var provider = new RegionProvider();
            InitializeStaticProvider(new RegisteringAdapter(provider), provider);
            var host = CreateHost();

            RegionHost.SetName(host, "MainRegion");

            host.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
            Assert.Single(provider.Regions);

            host.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));
            host.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

            Assert.Single(provider.Regions);
            Assert.Equal("MainRegion", provider.Regions[0].Name);
        });
    }

    /// <summary>
    /// 创建一个位于视图内部的区域宿主。
    /// </summary>
    private static ContentControl CreateHost()
    {
        var view = new FakeViewControl { ViewName = "MainView" };
        var host = new ContentControl();
        view.Content = host;

        return host;
    }

    /// <summary>
    /// 每次 <c>SetName</c> 都会从静态提供程序解析新的 builder，这里注册真实的
    /// <see cref="RegionBuilder"/>，让测试覆盖"多次解析 → 多次 Build"的真实路径。
    /// </summary>
    private static void InitializeStaticProvider(IRegionAdapter adapter, IRegionProvider? provider = null)
    {
        var services = new ServiceCollection();
        var regionProvider = provider ?? new RegionProvider();
        services.AddTransient<IRegionBuilder>(_ => new RegionBuilder(new StubAdapterFinder(adapter), regionProvider));
        Len.Abp.Core.AbpServiceProvider.Initialize(services.BuildServiceProvider());
    }

    private sealed class FakeViewControl : ContentControl, IView
    {
        public object ViewModel { get; } = new object();

        public string ViewName { get; init; } = string.Empty;
    }

    private sealed class RecordingAdapter : IRegionAdapter
    {
        public int InitializeCount { get; private set; }

        public string? RegionName { get; private set; }

        public IRegion Initialize(object regionTarget, string regionName, IView ownerView)
        {
            InitializeCount++;
            RegionName = regionName;

            return new Region(regionName, ownerView.ViewName, ownerView);
        }
    }

    /// <summary>
    /// 创建区域后立即注册到提供程序，模拟 <c>RegionProviderRegistrationBehavior</c>。
    /// </summary>
    private sealed class RegisteringAdapter(IRegionProvider provider) : IRegionAdapter
    {
        public IRegion Initialize(object regionTarget, string regionName, IView ownerView)
        {
            var region = new Region(regionName, ownerView.ViewName, ownerView);
            provider.Add(region);

            return region;
        }
    }

    private sealed class StubAdapterFinder(IRegionAdapter adapter) : IRegionAdapterFinder
    {
        public IRegionAdapter FindByControlType(Type controlType) => adapter;
    }
}
