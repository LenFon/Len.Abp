using Len.Abp.Wpf.Mvvm;
using Len.Abp.Wpf.Navigation;
using Len.Abp.Wpf.Navigation.Regions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;


namespace Len.Abp.Wpf.Tests;

/// <summary>
/// 覆盖：导航在公开入口封送到 UI 线程。
/// </summary>
/// <remarks>
/// 测试用假调度器而不是真实 <c>Dispatcher</c>：真实调度器在目标线程未泵消息时会永久挂起。
/// 假调度器把 <c>InvokeAsync</c> 实现为同步执行，既能验证封送路径被走到，
/// 又不会阻塞测试。
/// </remarks>
public class NavigationDispatcherTests
{
    /// <summary>
    /// 非 UI 线程调用时走封送路径，导航成功。
    /// </summary>
    [Fact]
    public async Task NavigateAsync_FromNonUiThread_MarshalsAndSucceeds()
    {
        var view = new FakeView("demo");
        var dispatcher = new FakeUiDispatcher { IsUiThread = false };
        var (service, region) = CreateService(dispatcher, view);

        var result = await service.NavigateAsync("MainRegion", "demo", IParameters.Create());

        Assert.True(result.Result);
        Assert.Null(result.Exception);
        Assert.Equal(1, dispatcher.InvokeCount);
        Assert.True(region.ActiveViews.Contains(view));
    }

    /// <summary>
    /// 封送后视图解析发生在 UI 线程上。
    /// </summary>
    /// <remarks>
    /// 用假调度器把"UI 线程"建模为一个标记：<c>InvokeAsync</c> 执行期间置位，
    /// 视图工厂读取该标记，从而断言视图确实在封送后的上下文里构造。
    /// </remarks>
    [Fact]
    public async Task NavigateAsync_FromNonUiThread_ResolvesViewInsideMarshalledScope()
    {
        var dispatcher = new FakeUiDispatcher { IsUiThread = false };
        var factory = new ThreadRecordingViewFactory();
        var (service, _) = CreateService(dispatcher, factory);

        await service.NavigateAsync("MainRegion", "demo", IParameters.Create());

        Assert.True(factory.ResolvedInsideMarshalledScope);
    }

    /// <summary>
    /// UI 线程内调用走快路径，不触发封送。
    /// </summary>
    [Fact]
    public async Task NavigateAsync_OnUiThread_DoesNotMarshal()
    {
        var view = new FakeView("demo");
        var dispatcher = new FakeUiDispatcher { IsUiThread = true };
        var (service, _) = CreateService(dispatcher, view);

        var result = await service.NavigateAsync("MainRegion", "demo", IParameters.Create());

        Assert.True(result.Result);
        Assert.Equal(0, dispatcher.InvokeCount);
    }

    /// <summary>
    /// 封送后 OnNavigatedFrom 只被调用一次。
    /// </summary>
    /// <remarks>
    /// 若内部各层重复封送，每次重入都会再走一遍通知流程，计数会翻倍。
    /// </remarks>
    [Fact]
    public async Task NavigateAsync_FromNonUiThread_NotifiesNavigatedFromOnce()
    {
        var firstViewModel = new CountingViewModel();
        var first = new FakeView("first", firstViewModel);
        var second = new FakeView("second");
        var dispatcher = new FakeUiDispatcher { IsUiThread = false };
        var (service, region) = CreateService(dispatcher, first, second);
        region.Add(first);
        region.Activate(first);

        await service.NavigateAsync("MainRegion", "second", IParameters.Create());

        Assert.Equal(1, firstViewModel.NavigatedFromCount);
        Assert.Equal(1, dispatcher.InvokeCount);
    }

    /// <summary>
    /// 便利重载与 URI 重载同样封送。
    /// </summary>
    [Fact]
    public async Task NavigateAsync_ConvenienceOverload_FromNonUiThread_Marshals()
    {
        var dispatcher = new FakeUiDispatcher { IsUiThread = false };
        var (service, _) = CreateService(dispatcher, new FakeView("demo"));

        var result = await service.NavigateAsync("MainRegion", "demo");

        Assert.True(result.Result);
        Assert.Equal(1, dispatcher.InvokeCount);
    }

    /// <summary>
    /// 取消令牌在封送之前检查，已取消时不进入 UI 线程。
    /// </summary>
    [Fact]
    public async Task NavigateAsync_PreCancelled_DoesNotMarshal()
    {
        var dispatcher = new FakeUiDispatcher { IsUiThread = false };
        var (service, _) = CreateService(dispatcher, new FakeView("demo"));
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await service.NavigateAsync("MainRegion", "demo", IParameters.Create(), cts.Token)
        );

        Assert.Equal(0, dispatcher.InvokeCount);
    }

    /// <summary>
    /// 封送过程中抛出的异常会传播给调用方。
    /// </summary>
    [Fact]
    public async Task NavigateAsync_MarshalledActionThrows_PropagatesException()
    {
        var dispatcher = new ThrowingUiDispatcher();
        var (service, _) = CreateService(dispatcher, new FakeView("demo"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await service.NavigateAsync("MainRegion", "demo", IParameters.Create())
        );
    }

    private static (NavigationService Service, Region Region) CreateService(
        IUiDispatcher dispatcher,
        params FakeView[] views
    )
    {
        var region = new SingleActiveRegion("MainRegion", "MainView");
        var provider = new RegionProvider();
        provider.Add(region);

        var services = new ServiceCollection();
        services.AddLogging();
        foreach (var view in views)
        {
            services.AddKeyedSingleton<IView>(view.ViewName, view);
        }

        var serviceProvider = services.BuildServiceProvider();

        return (new NavigationService(new RegionFinder(provider), provider, serviceProvider, dispatcher, new NavigationViewResolver(serviceProvider, new ViewScopeRegistry()), Options.Create(new NavigationOptions())), region);
    }

    private static (NavigationService Service, Region Region) CreateService(
        FakeUiDispatcher dispatcher,
        ThreadRecordingViewFactory factory
    )
    {
        var region = new SingleActiveRegion("MainRegion", "MainView");
        var provider = new RegionProvider();
        provider.Add(region);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddKeyedSingleton<IView>("demo", (_, _) => factory.Create(dispatcher));

        var serviceProvider = services.BuildServiceProvider();

        return (new NavigationService(new RegionFinder(provider), provider, serviceProvider, dispatcher, new NavigationViewResolver(serviceProvider, new ViewScopeRegistry()), Options.Create(new NavigationOptions())), region);
    }

    /// <summary>
    /// 记录视图是否在封送作用域内被解析。
    /// </summary>
    private sealed class ThreadRecordingViewFactory
    {
        public bool ResolvedInsideMarshalledScope { get; private set; }

        public FakeView Create(FakeUiDispatcher dispatcher)
        {
            // 假调度器的 InvokeAsync 同步执行，执行期间 InvokeCount 已递增；
            // 视图工厂在此时被调用，说明解析发生在封送之后。
            ResolvedInsideMarshalledScope = dispatcher.InvokeCount > 0;

            return new FakeView("demo");
        }
    }

    /// <summary>
    /// 只统计回调次数的视图模型。
    /// </summary>
    private sealed class CountingViewModel : INavigationAware
    {
        public int NavigatedFromCount { get; private set; }

        public Task<bool> IsNavigationTargetAsync(INavigationContext navigationContext) =>
            Task.FromResult(true);

        public Task OnNavigatedFromAsync(INavigationContext navigationContext)
        {
            NavigatedFromCount++;
            return Task.CompletedTask;
        }

        public Task OnNavigatedToAsync(INavigationContext navigationContext) => Task.CompletedTask;
    }

    /// <summary>
    /// 封送时抛异常的调度器。
    /// </summary>
    private sealed class ThrowingUiDispatcher : IUiDispatcher
    {
        public bool CheckAccess() => false;

        public Task<T> InvokeAsync<T>(Func<Task<T>> action) =>
            throw new InvalidOperationException("WPF 应用尚未启动，无法导航。");
    }
}
