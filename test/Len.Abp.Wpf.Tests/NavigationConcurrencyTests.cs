using Len.Abp.Wpf.Mvvm;
using Len.Abp.Wpf.Navigation;
using Len.Abp.Wpf.Navigation.Regions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Len.Abp.Wpf.Tests;

/// <summary>
/// 覆盖：并发导航被串行化，回调顺序与调用顺序一致。
/// </summary>
/// <remarks>
/// 串行化由 <c>NavigationGate</c> 的 <see cref="SemaphoreSlim"/> 实现；导航服务是单例，
/// 因此同一容器内的所有导航共用这一个闸门。测试通过可控的视图模型在回调里挂起，
/// 制造"第一次导航尚未完成时发起第二次导航"的真实交错场景。
/// </remarks>
public class NavigationConcurrencyTests
{
    /// <summary>
    /// 并发发起两次导航时，第二次必须等第一次的回调全部完成才开始。
    /// </summary>
    [Fact]
    public async Task NavigateAsync_ConcurrentCalls_RunCallbacksInCallOrder()
    {
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var events = new List<string>();

        var first = new FakeView("first", new GatedViewModel(gate.Task, events, "first"));
        var second = new FakeView("second", new RecordingViewModel(events, "second"));
        var (service, _) = CreateService(first, second);

        var firstNavigation = service.NavigateAsync("MainRegion", "first", IParameters.Create()).AsTask();

        // 等第一次导航进入 OnNavigatedToAsync 并挂起，再发起第二次。
        await WaitUntilAsync(() => events.Contains("first:to:enter"));

        var secondNavigation = service.NavigateAsync("MainRegion", "second", IParameters.Create()).AsTask();

        // 第二次导航此时必须还没开始：它被锁挡在外面。
        await Task.Delay(50);
        Assert.DoesNotContain("second:to:enter", events);

        gate.SetResult();
        await firstNavigation;
        await secondNavigation;

        Assert.Equal(
            ["first:to:enter", "first:to:exit", "first:from:enter", "first:from:exit", "second:to:enter", "second:to:exit"],
            events
        );
    }

    /// <summary>
    /// 并发导航到不同区域同样排队：全局锁不区分区域。
    /// </summary>
    /// <remarks>
    /// 这是刻意的取舍——桌面应用导航频率低，全局锁的吞吐损失可忽略，
    /// 换来的是不会出现跨区域互相等待的死锁面。
    /// </remarks>
    [Fact]
    public async Task NavigateAsync_ConcurrentCallsToDifferentRegions_AreSerialized()
    {
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var events = new List<string>();

        var first = new FakeView("first", new GatedViewModel(gate.Task, events, "first"));
        var second = new FakeView("second", new RecordingViewModel(events, "second"));
        var (service, firstRegion, secondRegion) = CreateServiceWithTwoRegions(first, second);

        var firstNavigation = service.NavigateAsync("FirstRegion", "first", IParameters.Create()).AsTask();
        await WaitUntilAsync(() => events.Contains("first:to:enter"));

        var secondNavigation = service.NavigateAsync("SecondRegion", "second", IParameters.Create()).AsTask();

        await Task.Delay(50);
        Assert.DoesNotContain("second:to:enter", events);

        gate.SetResult();
        await firstNavigation;
        await secondNavigation;

        Assert.True(firstRegion.ActiveViews.Contains(first));
        Assert.True(secondRegion.ActiveViews.Contains(second));
    }

    /// <summary>
    /// 排队期间被取消的导航不会执行，也不占用队列。
    /// </summary>
    [Fact]
    public async Task NavigateAsync_CancelledWhileQueued_DoesNotRun()
    {
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var events = new List<string>();

        var first = new FakeView("first", new GatedViewModel(gate.Task, events, "first"));
        var second = new FakeView("second", new RecordingViewModel(events, "second"));
        var (service, _) = CreateService(first, second);

        var firstNavigation = service.NavigateAsync("MainRegion", "first", IParameters.Create()).AsTask();
        await WaitUntilAsync(() => events.Contains("first:to:enter"));

        using var cts = new CancellationTokenSource();
        var secondNavigation = service
            .NavigateAsync("MainRegion", "second", IParameters.Create(), cts.Token)
            .AsTask();

        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => secondNavigation);

        gate.SetResult();
        await firstNavigation;

        // 被取消的导航没有执行任何回调。
        Assert.DoesNotContain("second:to:enter", events);
        Assert.DoesNotContain("second:from:enter", events);
    }

    /// <summary>
    /// 回调内再次导航不会自锁：内层导航直接执行并完成。
    /// </summary>
    /// <remarks>
    /// 外层导航持有锁并等待 <c>OnNavigatedToAsync</c>，若内层也去抢同一把锁就会永久等待。
    /// 重入检测让内层跳过排队。
    /// </remarks>
    [Fact]
    public async Task NavigateAsync_NestedNavigationFromCallback_DoesNotDeadlock()
    {
        var events = new List<string>();
        NavigationService? service = null;

        var child = new FakeView("child", new RecordingViewModel(events, "child"));
        var parent = new FakeView(
            "parent",
            new DelegatingViewModel(
                events,
                "parent",
                onNavigatedTo: async () =>
                {
                    var result = await service!.NavigateAsync("ChildRegion", "child", IParameters.Create());
                    Assert.True(result.Result);
                }
            )
        );

        var (created, childRegion) = CreateServiceWithChildRegion(parent, child);
        service = created;

        var result = await service.NavigateAsync("MainRegion", "parent", IParameters.Create());

        Assert.True(result.Result);
        Assert.True(childRegion.ActiveViews.Contains(child));
        Assert.Equal(
            ["parent:to:enter", "child:to:enter", "child:to:exit", "parent:to:exit"],
            events
        );
    }

    /// <summary>
    /// 重入标记不泄漏：一次导航结束后，后续导航重新排队而不是跳过锁。
    /// </summary>
    [Fact]
    public async Task NavigateAsync_AfterCompletion_StillSerializes()
    {
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var events = new List<string>();

        var first = new FakeView("first", new GatedViewModel(gate.Task, events, "first"));
        var second = new FakeView("second", new RecordingViewModel(events, "second"));
        var (service, _) = CreateService(first, second);

        // 第一次导航在回调里挂起，期间发起第二次；结束后再发起第三次。
        var firstNavigation = service.NavigateAsync("MainRegion", "first", IParameters.Create()).AsTask();
        await WaitUntilAsync(() => events.Contains("first:to:enter"));
        var secondNavigation = service.NavigateAsync("MainRegion", "second", IParameters.Create()).AsTask();

        gate.SetResult();
        await firstNavigation;
        await secondNavigation;

        events.Clear();

        var thirdNavigation = service.NavigateAsync("MainRegion", "first", IParameters.Create()).AsTask();
        await thirdNavigation;

        // 第三次导航正常完成，说明锁已释放、重入标记已复位。
        Assert.Contains("first:to:enter", events);
        Assert.Contains("first:to:exit", events);
    }

    /// <summary>
    /// 回调里启动的游离任务在外层结束后才导航时，必须重新排队而不是跳过锁。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 回归：重入标记是 <see cref="AsyncLocal{T}"/>，<c>Task.Run</c> 会捕获调用点的
    /// 执行上下文，因此游离任务带着外层导航的标记。若只看"标记是否存在"，
    /// 它在外层结束后仍会误判为重入而跳过锁，与别的导航并发修改区域状态。
    /// </para>
    /// <para>
    /// 修法是给标记加生命周期：外层导航结束时置为失效，残留标记不再生效。
    /// </para>
    /// <para>
    /// <b>用例必须让游离任务在外层结束后才真正导航。</b>若让它在外部仍在执行时进入，
    /// 那是"回调里 await 的合法嵌套"与"误用"无法区分的边界（见
    /// <c>NavigationGate</c> 的说明），两种实现都会通过，锁不住缺陷。
    /// 这里先让另一个导航持锁挂起，再放行游离任务，才能观察到它是否真的排队。
    /// </para>
    /// </remarks>
    [Fact]
    public async Task NavigateAsync_DetachedNavigationStartedAfterOuterCompletes_WaitsForLock()
    {
        var events = new List<string>();
        var outerGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var detachedMayProceed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var holderGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        NavigationService? service = null;

        var first = new FakeView(
            "first",
            new DelegatingViewModel(
                events,
                "first",
                onNavigatedTo: async () =>
                {
                    // 在回调里启动游离任务，但不 await 它。
                    // Task.Run 在此刻捕获执行上下文，游离任务因此带着外层导航的标记。
                    _ = Task.Run(async () =>
                    {
                        await detachedMayProceed.Task;
                        await service!.NavigateAsync("MainRegion", "detached", IParameters.Create());
                    });

                    await outerGate.Task;
                }
            )
        );
        var holder = new FakeView("holder", new GatedViewModel(holderGate.Task, events, "holder"));
        var detached = new FakeView("detached", new RecordingViewModel(events, "detached"));
        var (created, _) = CreateService(first, holder, detached);
        service = created;

        // 1. 外层导航完成，其标记已失效。
        var outer = service.NavigateAsync("MainRegion", "first", IParameters.Create()).AsTask();
        await WaitUntilAsync(() => events.Contains("first:to:enter"));
        outerGate.SetResult();
        await outer;

        // 2. 让另一个导航先拿到锁并挂起。
        var holding = service.NavigateAsync("MainRegion", "holder", IParameters.Create()).AsTask();
        await WaitUntilAsync(() => events.Contains("holder:to:enter"));

        // 3. 放行游离任务：它带着失效的残留标记，必须排队等 holder 结束。
        detachedMayProceed.SetResult();
        await Task.Delay(50);
        Assert.DoesNotContain("detached:to:enter", events);

        holderGate.SetResult();
        await holding;
        await WaitUntilAsync(() => events.Contains("detached:to:enter"));

        Assert.True(
            events.IndexOf("detached:to:enter") > events.IndexOf("holder:to:exit"),
            "游离任务应在 holder 完全结束后才开始。"
        );
    }

    private static (NavigationService Service, Region Region) CreateService(params FakeView[] views)
    {
        var region = new SingleActiveRegion("MainRegion", "MainView");
        var provider = new RegionProvider();
        provider.Add(region);

        return (BuildService(provider, views), region);
    }

    private static (NavigationService Service, Region FirstRegion, Region SecondRegion) CreateServiceWithTwoRegions(
        params FakeView[] views
    )
    {
        var firstRegion = new SingleActiveRegion("FirstRegion", "MainView");
        var secondRegion = new SingleActiveRegion("SecondRegion", "MainView");
        var provider = new RegionProvider();
        provider.Add(firstRegion);
        provider.Add(secondRegion);

        return (BuildService(provider, views), firstRegion, secondRegion);
    }

    private static (NavigationService Service, Region ChildRegion) CreateServiceWithChildRegion(
        FakeView parent,
        FakeView child
    )
    {
        var mainRegion = new SingleActiveRegion("MainRegion", "MainView");
        var childRegion = new SingleActiveRegion("ChildRegion", "parent");
        var provider = new RegionProvider();
        provider.Add(mainRegion);
        provider.Add(childRegion);

        return (BuildService(provider, parent, child), childRegion);
    }

    private static NavigationService BuildService(RegionProvider provider, params FakeView[] views)
    {
        var services = new ServiceCollection();
        services.AddLogging();

        foreach (var view in views)
        {
            services.AddKeyedSingleton<IView>(view.ViewName, view);
        }

        var serviceProvider = services.BuildServiceProvider();

        return new NavigationService(
            new RegionFinder(provider),
            provider,
            serviceProvider,
            new FakeUiDispatcher(),
            new NavigationViewResolver(serviceProvider, new ViewScopeRegistry()),
            Options.Create(new NavigationOptions())
        );
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        // 回调在导航内部同步推进，这里只是让出执行权，避免忙等。
        for (var i = 0; i < 200 && !condition(); i++)
        {
            await Task.Delay(10);
        }

        Assert.True(condition(), "等待条件超时。");
    }

    /// <summary>
    /// 在 <c>OnNavigatedToAsync</c> 里挂起直到外部放行的视图模型。
    /// </summary>
    private sealed class GatedViewModel(Task gate, List<string> events, string name) : INavigationAware
    {
        public Task<bool> IsNavigationTargetAsync(INavigationContext navigationContext) =>
            Task.FromResult(true);

        public Task OnNavigatedFromAsync(INavigationContext navigationContext)
        {
            events.Add($"{name}:from:enter");
            events.Add($"{name}:from:exit");
            return Task.CompletedTask;
        }

        public async Task OnNavigatedToAsync(INavigationContext navigationContext)
        {
            events.Add($"{name}:to:enter");
            await gate;
            events.Add($"{name}:to:exit");
        }
    }

    /// <summary>
    /// 只记录回调顺序的视图模型。
    /// </summary>
    private sealed class RecordingViewModel(List<string> events, string name) : INavigationAware
    {
        public Task<bool> IsNavigationTargetAsync(INavigationContext navigationContext) =>
            Task.FromResult(true);

        public Task OnNavigatedFromAsync(INavigationContext navigationContext)
        {
            events.Add($"{name}:from:enter");
            events.Add($"{name}:from:exit");
            return Task.CompletedTask;
        }

        public Task OnNavigatedToAsync(INavigationContext navigationContext)
        {
            events.Add($"{name}:to:enter");
            events.Add($"{name}:to:exit");
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// 在 <c>OnNavigatedToAsync</c> 中执行自定义动作的视图模型。
    /// </summary>
    private sealed class DelegatingViewModel(
        List<string> events,
        string name,
        Func<Task> onNavigatedTo
    ) : INavigationAware
    {
        public Task<bool> IsNavigationTargetAsync(INavigationContext navigationContext) =>
            Task.FromResult(true);

        public Task OnNavigatedFromAsync(INavigationContext navigationContext)
        {
            events.Add($"{name}:from:enter");
            events.Add($"{name}:from:exit");
            return Task.CompletedTask;
        }

        public async Task OnNavigatedToAsync(INavigationContext navigationContext)
        {
            events.Add($"{name}:to:enter");
            await onNavigatedTo();
            events.Add($"{name}:to:exit");
        }
    }
}
