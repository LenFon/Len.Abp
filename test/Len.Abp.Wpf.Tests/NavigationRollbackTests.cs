using Len.Abp.Wpf.Mvvm;
using Len.Abp.Wpf.Navigation;
using Len.Abp.Wpf.Navigation.Regions;
using Len.Abp.Wpf.Navigation.Regions.Behaviors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;


namespace Len.Abp.Wpf.Tests;

/// <summary>
/// 覆盖：导航回调异常后区域状态与返回值保持一致。
/// </summary>
/// <remarks>
/// 语义选择"失败即回滚"：<c>OnNavigatedToAsync</c> 抛异常时，区域恢复到导航前的活动视图，
/// 调用方看到失败，界面也回到原状。
/// </remarks>
public class NavigationRollbackTests
{
    /// <summary>
    /// OnNavigatedToAsync 抛异常后，活动视图仍是原视图。
    /// </summary>
    [Fact]
    public async Task NavigateAsync_NavigatedToThrows_RestoresPreviousActiveView()
    {
        var current = new FakeView("current", new CountingViewModel());
        var failing = new FakeView("target", new ThrowingViewModel());
        var (service, region) = CreateService(current, failing);

        var first = await service.NavigateAsync("MainRegion", "current", IParameters.Create());
        Assert.True(first.Result);

        var second = await service.NavigateAsync("MainRegion", "target", IParameters.Create());

        Assert.False(second.Result);
        Assert.IsType<InvalidOperationException>(second.Exception);
        Assert.Single(region.ActiveViews);
        Assert.True(region.ActiveViews.Contains(current));
        Assert.False(region.ActiveViews.Contains(failing));
    }

    /// <summary>
    /// 回滚不重新通知旧视图 OnNavigatedToAsync——它从未真正离开。
    /// </summary>
    [Fact]
    public async Task NavigateAsync_NavigatedToThrows_DoesNotRenotifyPreviousView()
    {
        var currentViewModel = new CountingViewModel();
        var current = new FakeView("current", currentViewModel);
        var failing = new FakeView("target", new ThrowingViewModel());
        var (service, _) = CreateService(current, failing);

        await service.NavigateAsync("MainRegion", "current", IParameters.Create());
        await service.NavigateAsync("MainRegion", "target", IParameters.Create());

        // 首次导航 1 次；回滚不应再触发。
        Assert.Equal(1, currentViewModel.NavigatedToCount);
        Assert.Equal(1, currentViewModel.NavigatedFromCount);
    }

    /// <summary>
    /// 返回结果携带原始异常，而不是回滚过程中的异常。
    /// </summary>
    [Fact]
    public async Task NavigateAsync_NavigatedToThrows_ReturnsOriginalException()
    {
        var current = new FakeView("current", new CountingViewModel());
        var failing = new FakeView("target", new ThrowingViewModel());
        var (service, _) = CreateService(current, failing);

        await service.NavigateAsync("MainRegion", "current", IParameters.Create());
        var result = await service.NavigateAsync("MainRegion", "target", IParameters.Create());

        var exception = Assert.IsType<InvalidOperationException>(result.Exception);
        Assert.Equal("导航失败", exception.Message);
    }

    /// <summary>
    /// 首次导航（没有前序活动视图）失败后区域中没有任何活动视图。
    /// </summary>
    [Fact]
    public async Task NavigateAsync_FirstNavigationFails_LeavesNoActiveView()
    {
        var failing = new FakeView("target", new ThrowingViewModel());
        var (service, region) = CreateService(failing);

        var result = await service.NavigateAsync("MainRegion", "target", IParameters.Create());

        Assert.False(result.Result);
        Assert.Empty(region.ActiveViews);
    }

    /// <summary>
    /// OnNavigatedFromAsync 抛异常时区域状态不变，无需回滚。
    /// </summary>
    [Fact]
    public async Task NavigateAsync_NavigatedFromThrows_KeepsRegionState()
    {
        var current = new FakeView("current", new ThrowingFromViewModel());
        var target = new FakeView("target", new CountingViewModel());
        var (service, region) = CreateService(current, target);

        // 先让 current 成为活动视图，但它的 OnNavigatedFrom 会抛异常。
        // 首次导航没有前序活动视图，因此不会触发 OnNavigatedFrom。
        var first = await service.NavigateAsync("MainRegion", "current", IParameters.Create());
        Assert.True(first.Result);

        var second = await service.NavigateAsync("MainRegion", "target", IParameters.Create());

        Assert.False(second.Result);
        Assert.IsType<InvalidOperationException>(second.Exception);
        Assert.Single(region.ActiveViews);
        Assert.True(region.ActiveViews.Contains(current));
    }

    /// <summary>
    /// 回滚失败时不覆盖原始异常。
    /// </summary>
    /// <remarks>
    /// 用会在激活指定视图时抛异常的派生区域模拟"回滚本身失败"：
    /// 只让回滚时的 <c>Activate(previous)</c> 失败，首次激活仍然成功，
    /// 调用方仍应拿到导航回调抛出的原始异常。
    /// </remarks>
    [Fact]
    public async Task NavigateAsync_RollbackFails_StillReturnsOriginalException()
    {
        var current = new FakeView("current", new CountingViewModel());
        var failing = new FakeView("target", new ThrowingViewModel());
        var region = new FailingActivateRegion("MainRegion", "MainView");
        var service = CreateService(region, current, failing);

        await service.NavigateAsync("MainRegion", "current", IParameters.Create());

        // 第二次导航：激活 target 成功，OnNavigatedTo 抛异常，回滚时 Activate(current) 抛异常。
        region.FailActivationFor = "current";
        var result = await service.NavigateAsync("MainRegion", "target", IParameters.Create());

        Assert.False(result.Result);
        var exception = Assert.IsType<InvalidOperationException>(result.Exception);
        Assert.Equal("导航失败", exception.Message);
    }

    /// <summary>
    /// 挂上拆除行为后，回滚仍能把旧视图留在区域中。
    /// </summary>
    /// <remarks>
    /// 这是回归测试：拆除行为会在视图失活时立即移除并释放它，
    /// 导致回滚无视图可恢复、区域变空白。导航现在延迟拆除——
    /// 激活前登记、成功才提交、失败则取消。
    /// </remarks>
    [Fact]
    public async Task NavigateAsync_NavigatedToThrows_WithTeardownBehavior_KeepsPreviousView()
    {
        var current = new FakeView("current");
        var failing = new FakeView("target", new ThrowingViewModel());
        var (service, region) = CreateServiceWithTeardown(current, failing);

        await service.NavigateAsync("MainRegion", "current", IParameters.Create());
        Assert.Equal("current", Assert.Single(region.ActiveViews).ViewName);

        var result = await service.NavigateAsync("MainRegion", "target", IParameters.Create());

        Assert.False(result.Result);

        // 旧视图必须仍在区域中且处于激活状态——修复前两者都是空的。
        Assert.Equal("current", Assert.Single(region.ActiveViews).ViewName);
        Assert.Equal("current", Assert.Single(region.Views).ViewName);
    }

    /// <summary>
    /// 成功导航后旧视图仍会被拆除，延迟拆除不引入泄漏。
    /// </summary>
    [Fact]
    public async Task NavigateAsync_Success_WithTeardownBehavior_RemovesPreviousView()
    {
        var (service, region) = CreateServiceWithTeardown(new FakeView("a"), new FakeView("b"));

        await service.NavigateAsync("MainRegion", "a", IParameters.Create());
        var result = await service.NavigateAsync("MainRegion", "b", IParameters.Create());

        Assert.True(result.Result);
        Assert.Equal("b", Assert.Single(region.ActiveViews).ViewName);
        Assert.Equal("b", Assert.Single(region.Views).ViewName);
    }

    /// <summary>
    /// 失败后再次成功导航，之前保留的旧视图会被正常拆除。
    /// </summary>
    /// <remarks>
    /// 取消延迟只丢弃登记，不改变区域内容。下一次成功导航必须能正常拆除
    /// 这些视图，否则失败一次就会永久泄漏一个视图。
    /// </remarks>
    [Fact]
    public async Task NavigateAsync_FailureThenSuccess_DoesNotLeakRetainedView()
    {
        var (service, region) = CreateServiceWithTeardown(
            new FakeView("a"),
            new FakeView("b", new ThrowingViewModel()),
            new FakeView("c")
        );

        await service.NavigateAsync("MainRegion", "a", IParameters.Create());

        Assert.False((await service.NavigateAsync("MainRegion", "b", IParameters.Create())).Result);
        Assert.Equal("a", Assert.Single(region.Views).ViewName);

        Assert.True((await service.NavigateAsync("MainRegion", "c", IParameters.Create())).Result);
        Assert.Equal("c", Assert.Single(region.Views).ViewName);
    }

    /// <summary>
    /// 导航到当前已激活的同一视图且回调失败时，该视图必须留在区域中。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 回归：回滚路径原先先 <c>CancelDeferral</c> 再 <c>Rollback</c>，
    /// 而 <c>Rollback</c> 会 <c>Deactivate(view)</c>。延迟已被取消，失活立即触发拆除，
    /// 被拆除的正是要恢复的那个实例——恢复循环再也找不到它，界面变空白。
    /// </para>
    /// <para>
    /// 既有回滚测试全部用两个**不同**视图（<c>current</c> → <c>target</c>），
    /// <c>previousActive</c> 与 <c>view</c> 永远不同，因此这条路径从未被测过。
    /// </para>
    /// </remarks>
    [Fact]
    public async Task NavigateAsync_SameViewNavigatedToThrows_KeepsViewInRegion()
    {
        var viewModel = new SwitchableViewModel();
        var view = new CountingDisposableView("target", viewModel);
        var (service, region) = CreateServiceWithTeardown(view);

        Assert.True((await service.NavigateAsync("MainRegion", "target", IParameters.Create())).Result);
        Assert.Single(region.ActiveViews);

        // 让 OnNavigatedTo 开始抛异常，再导航到同一个视图。
        viewModel.ThrowOnNavigatedTo = true;
        var result = await service.NavigateAsync("MainRegion", "target", IParameters.Create());

        Assert.False(result.Result);
        Assert.Contains(view, region.Views);
        Assert.Contains(view, region.ActiveViews);
        Assert.Equal(0, view.DisposeCount);
    }

    /// <summary>
    /// 导航到当前已激活的同一视图且回调失败时，视图模型不被释放。
    /// </summary>
    /// <remarks>
    /// 与上一条配对：视图与视图模型是两条独立的释放路径，
    /// 只断言视图会漏掉视图模型被释放的情况。
    /// </remarks>
    [Fact]
    public async Task NavigateAsync_SameViewNavigatedToThrows_DoesNotDisposeViewModel()
    {
        var viewModel = new SwitchableViewModel();
        var view = new CountingDisposableView("target", viewModel);
        var (service, _) = CreateServiceWithTeardown(view);

        await service.NavigateAsync("MainRegion", "target", IParameters.Create());

        viewModel.ThrowOnNavigatedTo = true;
        await service.NavigateAsync("MainRegion", "target", IParameters.Create());

        Assert.Equal(0, viewModel.DisposeCount);
    }

    /// <summary>
    /// 回滚之后延迟状态必须结束，下一次导航能正常把旧视图移出区域。
    /// </summary>
    /// <remarks>
    /// 修复在 <c>Rollback</c> 里加了"目标视图原本就是活动视图则不失活"的守卫，
    /// 必须确认它没有让延迟状态残留：若延迟未结束，后续导航的失活视图会一直
    /// 堆积在待拆除列表里，永不移出区域。
    /// </remarks>
    [Fact]
    public async Task NavigateAsync_SameViewFailsThenNavigatesElsewhere_RemovesOldView()
    {
        var viewModel = new SwitchableViewModel();
        var target = new CountingDisposableView("target", viewModel);
        var (service, region) = CreateServiceWithTeardown(target, new FakeView("other"));

        await service.NavigateAsync("MainRegion", "target", IParameters.Create());

        viewModel.ThrowOnNavigatedTo = true;
        Assert.False((await service.NavigateAsync("MainRegion", "target", IParameters.Create())).Result);
        Assert.Contains(target, region.Views);

        // 下一次导航成功：旧视图必须被移出区域，说明延迟状态已结束。
        Assert.True((await service.NavigateAsync("MainRegion", "other", IParameters.Create())).Result);
        Assert.DoesNotContain(target, region.Views);
        Assert.Equal("other", Assert.Single(region.Views).ViewName);
    }

    private static (NavigationService Service, Region Region) CreateService(params FakeView[] views)
    {
        var region = new SingleActiveRegion("MainRegion", "MainView");
        return (CreateService(region, views), region);
    }

    private static NavigationService CreateService(Region region, params FakeView[] views)
    {
        var provider = new RegionProvider();
        provider.Add(region);

        var services = new ServiceCollection();
        services.AddLogging();
        foreach (var view in views)
        {
            services.AddKeyedSingleton<IView>(view.ViewName, view);
        }

        var serviceProvider = services.BuildServiceProvider();

        return new NavigationService(new RegionFinder(provider), provider, serviceProvider, new FakeUiDispatcher(), new NavigationViewResolver(serviceProvider, new ViewScopeRegistry()), Options.Create(new NavigationOptions()));
    }

    /// <summary>
    /// 创建挂上拆除行为的服务，用于验证"回滚时旧视图仍在区域中"。
    /// </summary>
    /// <remarks>
    /// 不挂 <see cref="RegionMemberLifetimeBehavior"/> 时失活的视图不会被移出区域，
    /// 回滚自然能恢复——测试会假通过。真实应用里该行为总是由
    /// <c>RegionOptions.BehaviorKeys</c> 自动附加，因此必须在这里复现。
    /// </remarks>
    private static (NavigationService Service, Region Region) CreateServiceWithTeardown(params IView[] views)
    {
        var region = new SingleActiveRegion("MainRegion", "MainView");
        var provider = new RegionProvider();
        provider.Add(region);

        var registry = new ViewScopeRegistry();
        region.Behaviors.Add(
            nameof(RegionMemberLifetimeBehavior),
            new RegionMemberLifetimeBehavior(provider, registry)
        );

        var services = new ServiceCollection();
        services.AddLogging();
        foreach (var view in views)
        {
            services.AddKeyedSingleton<IView>(view.ViewName, view);
        }

        var serviceProvider = services.BuildServiceProvider();

        var service = new NavigationService(
            new RegionFinder(provider),
            provider,
            serviceProvider,
            new FakeUiDispatcher(),
            new NavigationViewResolver(serviceProvider, registry),
            Options.Create(new NavigationOptions())
        );

        return (service, region);
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
    /// OnNavigatedToAsync 抛异常的视图模型。
    /// </summary>
    private sealed class ThrowingViewModel : INavigationAware
    {
        public Task<bool> IsNavigationTargetAsync(INavigationContext navigationContext) =>
            Task.FromResult(true);

        public Task OnNavigatedFromAsync(INavigationContext navigationContext) =>
            Task.CompletedTask;

        public Task OnNavigatedToAsync(INavigationContext navigationContext) =>
            throw new InvalidOperationException("导航失败");
    }

    /// <summary>
    /// OnNavigatedFromAsync 抛异常的视图模型。
    /// </summary>
    private sealed class ThrowingFromViewModel : INavigationAware
    {
        public Task<bool> IsNavigationTargetAsync(INavigationContext navigationContext) =>
            Task.FromResult(true);

        public Task OnNavigatedFromAsync(INavigationContext navigationContext) =>
            throw new InvalidOperationException("离开失败");

        public Task OnNavigatedToAsync(INavigationContext navigationContext) =>
            Task.CompletedTask;
    }

    /// <summary>
    /// 可切换为激活失败的区域，用于验证回滚异常不覆盖原始异常。
    /// </summary>
    private sealed class FailingActivateRegion(string name, string owner)
        : SingleActiveRegion(name, owner)
    {
        /// <summary>
        /// 获取或设置激活时抛异常的视图名称；为 null 时正常激活。
        /// </summary>
        public string? FailActivationFor { get; set; }

        public override void Activate(IView view)
        {
            if (FailActivationFor is not null && view.ViewName == FailActivationFor)
            {
                throw new InvalidOperationException("回滚失败");
            }

            base.Activate(view);
        }
    }

    /// <summary>
    /// 可切换 <c>OnNavigatedToAsync</c> 是否抛异常的视图模型。
    /// </summary>
    /// <remarks>
    /// 该回归用例需要"首次导航成功、再次导航失败"的同一实例：
    /// 只有同一视图重复导航，<c>previousActive</c> 与目标视图才会是同一个对象。
    /// </remarks>
    private sealed class SwitchableViewModel : INavigationAware, IDisposable
    {
        /// <summary>
        /// 获取或设置 <c>OnNavigatedToAsync</c> 是否抛异常。
        /// </summary>
        public bool ThrowOnNavigatedTo { get; set; }

        /// <summary>
        /// 获取 <see cref="Dispose"/> 被调用的次数。
        /// </summary>
        public int DisposeCount { get; private set; }

        public Task<bool> IsNavigationTargetAsync(INavigationContext navigationContext) =>
            Task.FromResult(true);

        public Task OnNavigatedFromAsync(INavigationContext navigationContext) =>
            Task.CompletedTask;

        public Task OnNavigatedToAsync(INavigationContext navigationContext) =>
            ThrowOnNavigatedTo ? throw new InvalidOperationException("导航失败") : Task.CompletedTask;

        public void Dispose() => DisposeCount++;
    }

    /// <summary>
    /// 记录 <see cref="Dispose"/> 调用次数的视图。
    /// </summary>
    /// <remarks>
    /// 只记录"是否释放过"的替身发现不了重复释放，也发现不了"本该保留却被释放"。
    /// 计数才能区分这两种情况。
    /// </remarks>
    private sealed class CountingDisposableView(string viewName, object viewModel) : IView, IDisposable
    {
        public object ViewModel { get; } = viewModel;

        public string ViewName { get; } = viewName;

        /// <summary>
        /// 获取 <see cref="Dispose"/> 被调用的次数。
        /// </summary>
        public int DisposeCount { get; private set; }

        public void Dispose() => DisposeCount++;
    }
}
