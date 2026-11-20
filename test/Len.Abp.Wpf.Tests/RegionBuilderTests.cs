using Len.Abp.Wpf.Mvvm;
using Len.Abp.Wpf.Navigation;
using Len.Abp.Wpf.Navigation.Regions;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using Xunit;


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
            var builder = new RegionBuilder(finder, new RegionProvider());
            var view = new FakeViewControl { ViewName = "MainView" };
            var host = new ContentControl();
            view.Content = host;
            RegionHost.SetName(host, "MainRegion");

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
            var builder = new RegionBuilder(new StubAdapterFinder(adapter), new RegionProvider());

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
            var builder = new RegionBuilder(new StubAdapterFinder(adapter), new RegionProvider());
            var host = new ContentControl();
            RegionHost.SetName(host, "MainRegion");

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
            var builder = new RegionBuilder(new ThrowingAdapterFinder(), new RegionProvider());
            var view = new FakeViewControl { ViewName = "MainView" };
            var host = new ContentControl();
            view.Content = host;
            RegionHost.SetName(host, "MainRegion");

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
            var builder = new RegionBuilder(new StubAdapterFinder(adapter), new RegionProvider());
            var view = new FakeViewControl { ViewName = "MainView" };
            var host = new ContentControl();
            view.Content = host;
            RegionHost.SetName(host, "MainRegion");

            builder.Build(host);
            host.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
            host.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

            Assert.Equal(1, adapter.InitializeCount);
        });
    }

    /// <summary>
    /// 元素卸载后重新加载时恢复区域，不重建。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 回归：静态去重表曾把"已处理"永久记录，元素被移到新窗口后
    /// <c>Loaded</c> 再次触发也不会创建区域，界面上的区域永久失效。
    /// 修复后卸载会清掉"本轮已创建"标记。
    /// </para>
    /// <para>
    /// 回归：卸载后重新加载曾改为<b>重建</b>区域，但适配器在初始化时
    /// 把 <c>Content</c> / <c>ItemsSource</c> 单向指向区域中的视图，卸载不清除这些引用，
    /// 重建时适配器判定"控件状态已被占用"并抛异常，区域从此永久失效。
    /// 现在改为复用同一个区域：卸载只摘除，重新加载登记回去。
    /// </para>
    /// </remarks>
    [Fact]
    public void Build_UnloadedThenLoaded_RestoresSameRegion()
    {
        Sta.Run(() =>
        {
            InitializeStaticProvider();
            var adapter = new RecordingAdapter();
            var builder = new RegionBuilder(new StubAdapterFinder(adapter), new RegionProvider());
            var view = new FakeViewControl { ViewName = "MainView" };
            var host = new ContentControl();
            view.Content = host;
            RegionHost.SetName(host, "MainRegion");

            builder.Build(host);
            host.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
            host.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));
            host.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

            // 只创建一次：重新加载复用卸载时摘除的区域。
            Assert.Equal(1, adapter.InitializeCount);
        });
    }

    /// <summary>
    /// 加载事件来自非 <see cref="FrameworkElement"/> 时直接忽略。
    /// </summary>
    /// <remarks>
    /// <c>Loaded</c> 的发送方总是 <see cref="FrameworkElement"/>，但处理器是公开可调用的
    /// 事件订阅，类型守卫保证误用时不抛 <see cref="InvalidCastException"/>。
    /// </remarks>
    [Fact]
    public void OnElementLoaded_NonFrameworkElementSender_IsIgnored()
    {
        Sta.Run(() =>
        {
            var adapter = new RecordingAdapter();
            var builder = new RegionBuilder(new StubAdapterFinder(adapter), new RegionProvider());

            builder.OnElementLoaded(new DependencyObject(), new RoutedEventArgs());

            Assert.Null(adapter.RegionName);
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

            return new Region(regionName, ownerView.ViewName, ownerView);
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
