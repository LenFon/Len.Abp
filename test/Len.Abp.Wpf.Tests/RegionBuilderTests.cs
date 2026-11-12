using Len.Abp.Wpf.Mvvm;
using Len.Abp.Wpf.Navigation.Regions;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using Xunit;

// 附加属性 Len.Abp.Wpf.Navigation.Region 与区域实现 Len.Abp.Wpf.Navigation.Regions.Region 同名。
using RegionImpl = Len.Abp.Wpf.Navigation.Regions.Region;

namespace Len.Abp.Wpf.Tests;

/// <summary>
/// 覆盖 <see cref="RegionBuilder"/> 在控件加载后创建区域。
/// </summary>
[Collection(GlobalServiceProviderCollection.Name)]
public class RegionBuilderTests
{
    /// <summary>
    /// 设置 <c>Region.Name</c> 附加属性会触发静态服务提供程序解析 <see cref="IRegionBuilder"/>，
    /// 这里注册一个空实现，使测试聚焦于被测的 <see cref="RegionBuilder"/> 实例。
    /// </summary>
    private static void InitializeStaticProvider()
    {
        var services = new ServiceCollection();
        services.AddTransient<IRegionBuilder>(_ => new NoOpRegionBuilder());
        Len.Abp.Core.AbpServiceProvider.Initialize(services.BuildServiceProvider());
    }

    private sealed class NoOpRegionBuilder : IRegionBuilder
    {
        public void Build(DependencyObject element) { }
    }

    /// <summary>
    /// 控件加载后按附加属性创建区域并交给适配器。
    /// </summary>
    [Fact]
    public void Build_OnLoaded_CreatesRegion()
    {
        Sta.Run(() =>
        {
            var adapter = new RecordingAdapter();
            var finder = new StubAdapterFinder(adapter);
            InitializeStaticProvider();
            var builder = new RegionBuilder(finder);
            var view = new FakeViewControl { ViewName = "MainView" };
            var host = new ContentControl();
            view.Content = host;
            Navigation.Region.SetName(host, "MainRegion");

            builder.Build(host);
            host.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

            Assert.Equal("MainRegion", adapter.RegionName);
            Assert.Equal("MainView", adapter.OwnerView?.ViewName);
            Assert.Same(host, adapter.Target);
        });
    }

    /// <summary>
    /// 目标不是 FrameworkElement 时不会挂接加载事件，也不会创建区域。
    /// </summary>
    [Fact]
    public void Build_NonFrameworkElement_DoesNotCreateRegion()
    {
        Sta.Run(() =>
        {
            var adapter = new RecordingAdapter();
            var builder = new RegionBuilder(new StubAdapterFinder(adapter));

            builder.Build(new DependencyObject());

            Assert.Null(adapter.RegionName);
        });
    }

    /// <summary>
    /// 控件不属于任何视图时创建区域抛出 <see cref="RegionCreationException"/>。
    /// </summary>
    [Fact]
    public void Build_ElementWithoutView_ThrowsRegionCreationException()
    {
        Sta.Run(() =>
        {
            InitializeStaticProvider();
            var adapter = new RecordingAdapter();
            var builder = new RegionBuilder(new StubAdapterFinder(adapter));
            var host = new ContentControl();
            Navigation.Region.SetName(host, "MainRegion");

            builder.Build(host);

            var ex = Assert.Throws<RegionCreationException>(
                () => host.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent))
            );

            Assert.Contains("MainRegion", ex.Message);
        });
    }

    /// <summary>
    /// 适配器查找失败时包装为 <see cref="RegionCreationException"/>。
    /// </summary>
    [Fact]
    public void Build_AdapterNotFound_ThrowsRegionCreationException()
    {
        Sta.Run(() =>
        {
            InitializeStaticProvider();
            var builder = new RegionBuilder(new ThrowingAdapterFinder());
            var view = new FakeViewControl { ViewName = "MainView" };
            var host = new ContentControl();
            view.Content = host;
            Navigation.Region.SetName(host, "MainRegion");

            builder.Build(host);

            var ex = Assert.Throws<RegionCreationException>(
                () => host.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent))
            );

            Assert.IsType<KeyNotFoundException>(ex.InnerException);
        });
    }

    /// <summary>
    /// 重复触发加载事件只创建一次区域。
    /// </summary>
    [Fact]
    public void Build_LoadedTwice_CreatesRegionOnce()
    {
        Sta.Run(() =>
        {
            InitializeStaticProvider();
            var adapter = new RecordingAdapter();
            var builder = new RegionBuilder(new StubAdapterFinder(adapter));
            var view = new FakeViewControl { ViewName = "MainView" };
            var host = new ContentControl();
            view.Content = host;
            Navigation.Region.SetName(host, "MainRegion");

            builder.Build(host);
            host.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
            host.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

            Assert.Equal(1, adapter.InitializeCount);
        });
    }

    private sealed class FakeViewControl : ContentControl, IView
    {
        public object ViewModel { get; } = new object();

        public string ViewName { get; init; } = string.Empty;
    }

    private sealed class RecordingAdapter : IRegionAdapter
    {
        public int InitializeCount { get; private set; }

        public object? Target { get; private set; }

        public string? RegionName { get; private set; }

        public IView? OwnerView { get; private set; }

        public IRegion Initialize(object regionTarget, string regionName, IView ownerView)
        {
            InitializeCount++;
            Target = regionTarget;
            RegionName = regionName;
            OwnerView = ownerView;

            return new RegionImpl(regionName, ownerView.ViewName, ownerView);
        }
    }

    private sealed class StubAdapterFinder(IRegionAdapter adapter) : IRegionAdapterFinder
    {
        public IRegionAdapter FindByControlType(Type controlType) => adapter;
    }

    private sealed class ThrowingAdapterFinder : IRegionAdapterFinder
    {
        public IRegionAdapter FindByControlType(Type controlType) =>
            throw new KeyNotFoundException("未找到适配器");
    }
}
