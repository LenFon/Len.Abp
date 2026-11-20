using Len.Abp.Wpf.Mvvm;
using Len.Abp.Wpf.Navigation;
using Len.Abp.Wpf.Navigation.Regions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;


namespace Len.Abp.Wpf.Tests;

/// <summary>
/// 覆盖 <see cref="INavigationService"/> 的取消支持：便利重载、取消检查点与状态一致性。
/// </summary>
public class NavigationCancellationTests
{
    /// <summary>
    /// 不传参数的便利重载等价于传空参数集合。
    /// </summary>
    [Fact]
    public async Task NavigateAsync_WithoutParameters_IsEquivalentToEmptyParameters()
    {
        var view = new FakeView("demo");
        var (service, region) = CreateService(view);

        var result = await service.NavigateAsync("MainRegion", "demo");

        Assert.True(result.Result);
        Assert.Null(result.Exception);
        Assert.Equal(0, result.Context.Parameters.Count);
        Assert.True(region.ActiveViews.Contains(view));
    }

    /// <summary>
    /// 已取消的令牌在入口直接抛出，不解析视图也不改变区域状态。
    /// </summary>
    [Fact]
    public async Task NavigateAsync_PreCancelled_ThrowsWithoutTouchingRegion()
    {
        var view = new FakeView("demo");
        var (service, region) = CreateService(view);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await service.NavigateAsync("MainRegion", "demo", IParameters.Create(), cts.Token)
        );

        Assert.Empty(region.Views);
        Assert.Empty(region.ActiveViews);
    }

    /// <summary>
    /// 便利重载同样响应取消。
    /// </summary>
    [Fact]
    public async Task NavigateAsync_ConvenienceOverload_PreCancelled_Throws()
    {
        var (service, _) = CreateService(new FakeView("demo"));
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await service.NavigateAsync("MainRegion", "demo", cts.Token)
        );
    }

    /// <summary>
    /// URI 重载同样响应取消。
    /// </summary>
    [Fact]
    public async Task NavigateAsync_UriOverload_PreCancelled_Throws()
    {
        var (service, _) = CreateService(new FakeView("demo"));
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () =>
                await service.NavigateAsync(
                    "MainRegion",
                    new Uri("demo", UriKind.Relative),
                    IParameters.Create(),
                    cts.Token
                )
        );
    }

    /// <summary>
    /// 视图解析后、激活前取消：本次新建的视图被回收，区域回到导航前的状态。
    /// </summary>
    /// <remarks>
    /// 取消发生在 <c>IsNavigationTargetAsync</c> 里——它是解析阶段唯一的异步回调，
    /// 此时视图已加入区域但尚未激活，正好覆盖"解析后取消"这个检查点。
    /// 新建的视图必须回收：不保活的作用域视图不会被后续导航复用，
    /// 留着只会随每次取消累积（连同导航作用域）。
    /// </remarks>
    [Fact]
    public async Task NavigateAsync_CancelledAfterResolve_DoesNotActivateView()
    {
        using var cts = new CancellationTokenSource();
        var existing = new FakeView("demo", new CancellingViewModel(cts) { IsTarget = false });
        var fresh = new FakeView("demo");
        var (service, region) = CreateService(fresh);
        region.Add(existing);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await service.NavigateAsync("MainRegion", "demo", IParameters.Create(), cts.Token)
        );

        Assert.Single(region.Views);
        Assert.True(region.Views.Contains(existing));
        Assert.False(region.Views.Contains(fresh));
        Assert.Empty(region.ActiveViews);
    }

    /// <summary>
    /// 取消时复用的已有视图不会被移出区域。
    /// </summary>
    [Fact]
    public async Task NavigateAsync_CancelledAfterResolve_KeepsReusedView()
    {
        using var cts = new CancellationTokenSource();
        var existing = new FakeView("demo", new CancellingViewModel(cts) { IsTarget = true });
        var (service, region) = CreateService();
        region.Add(existing);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await service.NavigateAsync("MainRegion", "demo", IParameters.Create(), cts.Token)
        );

        Assert.Single(region.Views);
        Assert.True(region.Views.Contains(existing));
        Assert.Empty(region.ActiveViews);
    }

    /// <summary>
    /// 取消后当前活动视图不会收到 OnNavigatedFrom，界面保持原状。
    /// </summary>
    [Fact]
    public async Task NavigateAsync_CancelledAfterResolve_DoesNotNotifyCurrentView()
    {
        using var cts = new CancellationTokenSource();
        var currentViewModel = new CountingViewModel();
        var current = new FakeView("current", currentViewModel);
        var placeholder = new FakeView("target", new CancellingViewModel(cts) { IsTarget = false });
        var fresh = new FakeView("target");
        var (service, region) = CreateService(fresh);
        region.Add(current);
        region.Add(placeholder);

        var first = await service.NavigateAsync("MainRegion", "current", IParameters.Create());
        Assert.True(first.Result);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await service.NavigateAsync("MainRegion", "target", IParameters.Create(), cts.Token)
        );

        Assert.Equal(0, currentViewModel.NavigatedFromCount);
        Assert.True(region.ActiveViews.Contains(current));
        Assert.False(region.Views.Contains(fresh));
    }

    /// <summary>
    /// 视图模型自己抛出的 OperationCanceledException 仍被包装成失败结果。
    /// </summary>
    /// <remarks>
    /// 只有"令牌确实被取消"才穿透。视图模型内部的超时取消不应让调用方误以为
    /// 是调用方取消了导航，否则调用方的取消处理逻辑会被误触发。
    /// </remarks>
    [Fact]
    public async Task NavigateAsync_ViewModelThrowsCancellation_ReturnsFailedResult()
    {
        var view = new FakeView("demo", new ThrowingCancellationViewModel());
        var (service, _) = CreateService(view);

        var result = await service.NavigateAsync("MainRegion", "demo", IParameters.Create());

        Assert.False(result.Result);
        Assert.IsType<OperationCanceledException>(result.Exception, exactMatch: false);
    }

    /// <summary>
    /// 未取消的令牌不影响正常导航。
    /// </summary>
    [Fact]
    public async Task NavigateAsync_NotCancelled_Succeeds()
    {
        var viewModel = new CountingViewModel();
        var view = new FakeView("demo", viewModel);
        var (service, region) = CreateService(view);

        var result = await service.NavigateAsync(
            "MainRegion",
            "demo",
            IParameters.Create(),
            CancellationToken.None
        );

        Assert.True(result.Result);
        Assert.Equal(1, viewModel.NavigatedToCount);
        Assert.True(region.ActiveViews.Contains(view));
    }

    private static (NavigationService Service, Region Region) CreateService(params FakeView[] views)
    {
        var region = new SingleActiveRegion("MainRegion", "MainView");
        var provider = new RegionProvider();
        provider.Add(region);

        var services = new ServiceCollection();
        foreach (var view in views)
        {
            services.AddKeyedSingleton<IView>(view.ViewName, view);
        }

        var serviceProvider = services.BuildServiceProvider();
        var service = new NavigationService(new RegionFinder(provider), provider, serviceProvider, new FakeUiDispatcher(), new NavigationViewResolver(serviceProvider, new ViewScopeRegistry()), Options.Create(new NavigationOptions()));

        return (service, region);
    }

    /// <summary>
    /// 在解析阶段取消令牌的视图模型，用于覆盖"解析后、激活前"的检查点。
    /// </summary>
    /// <param name="cts">要取消的令牌源。</param>
    private sealed class CancellingViewModel(CancellationTokenSource cts) : INavigationAware
    {
        /// <summary>
        /// 获取或设置是否接受本次导航。为 true 时视图被复用，为 false 时解析新实例。
        /// </summary>
        public bool IsTarget { get; init; }

        public async Task<bool> IsNavigationTargetAsync(INavigationContext navigationContext)
        {
            await cts.CancelAsync();
            return IsTarget;
        }

        public Task OnNavigatedFromAsync(INavigationContext navigationContext) => Task.CompletedTask;

        public Task OnNavigatedToAsync(INavigationContext navigationContext) => Task.CompletedTask;
    }

    /// <summary>
    /// 只统计回调次数的视图模型。
    /// </summary>
    private sealed class CountingViewModel : INavigationAware
    {
        public int NavigatedToCount { get; private set; }

        public int NavigatedFromCount { get; private set; }

        public Task<bool> IsNavigationTargetAsync(INavigationContext navigationContext) =>
            Task.FromResult(true);

        public Task OnNavigatedFromAsync(INavigationContext navigationContext)
        {
            NavigatedFromCount++;
            return Task.CompletedTask;
        }

        public Task OnNavigatedToAsync(INavigationContext navigationContext)
        {
            NavigatedToCount++;
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// 在 OnNavigatedToAsync 里抛出取消异常，但令牌并未取消。
    /// </summary>
    private sealed class ThrowingCancellationViewModel : INavigationAware
    {
        public Task<bool> IsNavigationTargetAsync(INavigationContext navigationContext) =>
            Task.FromResult(true);

        public Task OnNavigatedFromAsync(INavigationContext navigationContext) =>
            Task.CompletedTask;

        public Task OnNavigatedToAsync(INavigationContext navigationContext) =>
            throw new OperationCanceledException("视图模型内部超时");
    }
}
