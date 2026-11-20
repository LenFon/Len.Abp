using Len.Abp.Wpf.Mvvm;
using Len.Abp.Wpf.Navigation;
using Len.Abp.Wpf.Navigation.Regions;
using Len.Abp.Wpf.Navigation.Regions.Behaviors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Len.Abp.Wpf.Tests;

/// <summary>
/// 覆盖：按区域独立的导航历史与 <c>GoBackAsync</c>。
/// </summary>
/// <remarks>
/// 历史只记录视图名与参数，不记录视图实例；回退走与普通导航相同的解析路径，
/// 因此已被释放的瞬态视图会得到新实例。
/// </remarks>
public class NavigationHistoryTests
{
    /// <summary>
    /// A → B → C 后连续回退，依次回到 B、A。
    /// </summary>
    [Fact]
    public async Task GoBackAsync_AfterThreeNavigations_ReturnsInReverseOrder()
    {
        var (service, region) = CreateService(new FakeView("a"), new FakeView("b"), new FakeView("c"));

        await service.NavigateAsync("MainRegion", "a", IParameters.Create());
        await service.NavigateAsync("MainRegion", "b", IParameters.Create());
        await service.NavigateAsync("MainRegion", "c", IParameters.Create());

        var backToB = await service.GoBackAsync("MainRegion");
        Assert.True(backToB.Result);
        Assert.Equal("b", Assert.Single(region.ActiveViews).ViewName);

        var backToA = await service.GoBackAsync("MainRegion");
        Assert.True(backToA.Result);
        Assert.Equal("a", Assert.Single(region.ActiveViews).ViewName);
    }

    /// <summary>
    /// 栈里只剩一条时无法回退，返回失败结果。
    /// </summary>
    [Fact]
    public async Task GoBackAsync_OnlyOneEntry_ReturnsFailedResult()
    {
        var (service, _) = CreateService(new FakeView("a"));

        await service.NavigateAsync("MainRegion", "a", IParameters.Create());

        var result = await service.GoBackAsync("MainRegion");

        Assert.False(result.Result);
        Assert.Null(result.Exception);
    }

    /// <summary>
    /// 从未导航过时回退返回失败结果。
    /// </summary>
    [Fact]
    public async Task GoBackAsync_NoHistory_ReturnsFailedResult()
    {
        var (service, _) = CreateService(new FakeView("a"));

        var result = await service.GoBackAsync("MainRegion");

        Assert.False(result.Result);
    }

    /// <summary>
    /// 区域不存在时回退返回失败结果，不抛异常。
    /// </summary>
    [Fact]
    public async Task GoBackAsync_UnknownRegion_ReturnsFailedResult()
    {
        var (service, _) = CreateService(new FakeView("a"));

        var result = await service.GoBackAsync("MissingRegion");

        Assert.False(result.Result);
        Assert.Null(result.Exception);
    }

    /// <summary>
    /// 回退到已被释放的瞬态视图时重新解析新实例，参数取历史记录中的值。
    /// </summary>
    /// <remarks>
    /// 必须挂上 <see cref="RegionMemberLifetimeBehavior"/>：没有它时失活的视图不会被移出区域，
    /// 回退会复用旧实例，测不出"重新解析"这条路径。
    /// </remarks>
    [Fact]
    public async Task GoBackAsync_ReleasedTransientView_ResolvesNewInstanceWithOriginalParameters()
    {
        var created = new List<FakeView>();
        var (service, region) = CreateServiceWithFactory(created, "a", "b");

        await service.NavigateAsync("MainRegion", "a", IParameters.Create("?id=42"));
        await service.NavigateAsync("MainRegion", "b", IParameters.Create());

        var firstA = created.Single(view => view.ViewName == "a");

        var result = await service.GoBackAsync("MainRegion");

        Assert.True(result.Result);

        var secondA = Assert.Single(region.ActiveViews);
        Assert.Equal("a", secondA.ViewName);
        Assert.NotSame(firstA, secondA);
        Assert.Equal(42, result.Context.Parameters.GetValue<int>("id"));
    }

    /// <summary>
    /// 回退后目标视图的 <c>OnNavigatedToAsync</c> 被调用，参数与首次导航一致。
    /// </summary>
    [Fact]
    public async Task GoBackAsync_NotifiesTargetWithOriginalParameters()
    {
        var viewModel = new RecordingViewModel();
        var (service, _) = CreateService(
            new FakeView("a", viewModel),
            new FakeView("b")
        );

        await service.NavigateAsync("MainRegion", "a", IParameters.Create("?id=7"));
        await service.NavigateAsync("MainRegion", "b", IParameters.Create());

        var result = await service.GoBackAsync("MainRegion");

        Assert.True(result.Result);
        Assert.Equal(2, viewModel.NavigatedToCount);
        Assert.Equal(7, viewModel.LastParameters!.GetValue<int>("id"));
    }

    /// <summary>
    /// 回退不产生新的历史条目，因此可以连续回退而不会在两个视图之间震荡。
    /// </summary>
    [Fact]
    public async Task GoBackAsync_DoesNotPushHistory()
    {
        var (service, region) = CreateService(new FakeView("a"), new FakeView("b"), new FakeView("c"));

        await service.NavigateAsync("MainRegion", "a", IParameters.Create());
        await service.NavigateAsync("MainRegion", "b", IParameters.Create());
        await service.NavigateAsync("MainRegion", "c", IParameters.Create());

        await service.GoBackAsync("MainRegion");
        Assert.Equal("b", Assert.Single(region.ActiveViews).ViewName);

        // 若回退也压栈，这里会回到 c 而不是 a。
        await service.GoBackAsync("MainRegion");
        Assert.Equal("a", Assert.Single(region.ActiveViews).ViewName);

        var third = await service.GoBackAsync("MainRegion");
        Assert.False(third.Result);
    }

    /// <summary>
    /// 不同区域的历史互相独立。
    /// </summary>
    [Fact]
    public async Task GoBackAsync_RegionsHaveIndependentHistory()
    {
        var (service, first, second) = CreateServiceWithTwoRegions(
            new FakeView("a"),
            new FakeView("b"),
            new FakeView("x"),
            new FakeView("y")
        );

        await service.NavigateAsync("FirstRegion", "a", IParameters.Create());
        await service.NavigateAsync("FirstRegion", "b", IParameters.Create());
        await service.NavigateAsync("SecondRegion", "x", IParameters.Create());
        await service.NavigateAsync("SecondRegion", "y", IParameters.Create());

        var backFirst = await service.GoBackAsync("FirstRegion");
        Assert.True(backFirst.Result);
        Assert.Equal("a", Assert.Single(first.ActiveViews).ViewName);

        // 第二区域的历史不受第一区域回退影响。
        Assert.Equal("y", Assert.Single(second.ActiveViews).ViewName);

        var backSecond = await service.GoBackAsync("SecondRegion");
        Assert.True(backSecond.Result);
        Assert.Equal("x", Assert.Single(second.ActiveViews).ViewName);
    }

    /// <summary>
    /// 连续导航到同一视图不产生重复历史条目。
    /// </summary>
    [Fact]
    public async Task NavigateAsync_SameViewTwice_DoesNotDuplicateHistory()
    {
        var (service, _) = CreateService(new FakeView("a"), new FakeView("b"));

        await service.NavigateAsync("MainRegion", "a", IParameters.Create());
        await service.NavigateAsync("MainRegion", "a", IParameters.Create());
        await service.NavigateAsync("MainRegion", "b", IParameters.Create());

        // 历史是 a → b；回退一次到 a 后就没有更早的条目。
        var backToA = await service.GoBackAsync("MainRegion");
        Assert.True(backToA.Result);

        var further = await service.GoBackAsync("MainRegion");
        Assert.False(further.Result);
    }

    /// <summary>
    /// 失败的导航不记入历史。
    /// </summary>
    [Fact]
    public async Task NavigateAsync_FailedNavigation_IsNotRecorded()
    {
        var (service, region) = CreateService(new FakeView("a"));

        await service.NavigateAsync("MainRegion", "a", IParameters.Create());

        var failed = await service.NavigateAsync("MainRegion", "missing", IParameters.Create());
        Assert.False(failed.Result);

        // 历史里只有 a，没有更早的条目。
        var back = await service.GoBackAsync("MainRegion");
        Assert.False(back.Result);
        Assert.Equal("a", Assert.Single(region.ActiveViews).ViewName);
    }

    /// <summary>
    /// 历史深度受限，超出后丢弃最旧的条目。
    /// </summary>
    [Fact]
    public async Task NavigateAsync_ExceedsDepth_DropsOldestEntries()
    {
        var (service, _) = CreateService(
            depth: 2,
            new FakeView("a"),
            new FakeView("b"),
            new FakeView("c")
        );

        await service.NavigateAsync("MainRegion", "a", IParameters.Create());
        await service.NavigateAsync("MainRegion", "b", IParameters.Create());
        await service.NavigateAsync("MainRegion", "c", IParameters.Create());

        // 深度 2 只保留 b、c：回退一次到 b，再回退就没有了。
        Assert.True((await service.GoBackAsync("MainRegion")).Result);
        Assert.False((await service.GoBackAsync("MainRegion")).Result);
    }

    /// <summary>
    /// 回退同样封送到 UI 线程。
    /// </summary>
    [Fact]
    public async Task GoBackAsync_FromNonUiThread_Marshals()
    {
        var dispatcher = new FakeUiDispatcher { IsUiThread = false };
        var (service, _) = CreateService(dispatcher, new FakeView("a"), new FakeView("b"));

        await service.NavigateAsync("MainRegion", "a", IParameters.Create());
        await service.NavigateAsync("MainRegion", "b", IParameters.Create());

        var before = dispatcher.InvokeCount;
        var result = await service.GoBackAsync("MainRegion");

        Assert.True(result.Result);
        Assert.Equal(before + 1, dispatcher.InvokeCount);
    }

    /// <summary>
    /// 回退与普通导航共用串行化锁，不会交错执行。
    /// </summary>
    /// <remarks>
    /// 用只在"回退进入 b"时挂起的视图模型：首次导航 b 时放行，回退时挂起，
    /// 从而制造"回退尚未完成时发起新导航"的交错场景。
    /// </remarks>
    [Fact]
    public async Task GoBackAsync_IsSerializedWithNavigation()
    {
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var events = new List<string>();
        var gated = new GateOnSecondEntryViewModel(gate.Task, events);

        var (service, _) = CreateService(
            new FakeView("a"),
            new FakeView("b", gated),
            new FakeView("c")
        );

        await service.NavigateAsync("MainRegion", "a", IParameters.Create());
        await service.NavigateAsync("MainRegion", "b", IParameters.Create());
        await service.NavigateAsync("MainRegion", "c", IParameters.Create());
        events.Clear();

        // 历史是 a → b → c，回退一次到 b。
        var back = service.GoBackAsync("MainRegion").AsTask();
        await WaitUntilAsync(() => events.Contains("b:to:enter"));

        var navigation = service.NavigateAsync("MainRegion", "a", IParameters.Create()).AsTask();
        await Task.Delay(50);
        Assert.DoesNotContain("a:to:enter", events);

        gate.SetResult();
        await back;
        await navigation;
    }

    /// <summary>
    /// 自定义实现未提供历史时，默认实现抛 <see cref="NotSupportedException"/>。
    /// </summary>
    [Fact]
    public async Task GoBackAsync_DefaultInterfaceImplementation_ThrowsNotSupported()
    {
        INavigationService service = new MinimalNavigationService();

        await Assert.ThrowsAsync<NotSupportedException>(async () => await service.GoBackAsync("MainRegion"));
    }

    /// <summary>
    /// 深度为 0 时回退到默认深度，避免配置失误让回退完全失效。
    /// </summary>
    [Fact]
    public async Task HistoryDepth_Zero_FallsBackToDefault()
    {
        var (service, _) = CreateService(
            depth: 0,
            new FakeView("a"),
            new FakeView("b"),
            new FakeView("c")
        );

        await service.NavigateAsync("MainRegion", "a", IParameters.Create());
        await service.NavigateAsync("MainRegion", "b", IParameters.Create());
        await service.NavigateAsync("MainRegion", "c", IParameters.Create());

        Assert.True((await service.GoBackAsync("MainRegion")).Result);
        Assert.True((await service.GoBackAsync("MainRegion")).Result);
    }

    /// <summary>
    /// 深度为负数时同样回退到默认深度。
    /// </summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public async Task HistoryDepth_Negative_FallsBackToDefault(int depth)
    {
        var (service, _) = CreateService(depth, new FakeView("a"), new FakeView("b"), new FakeView("c"));

        await service.NavigateAsync("MainRegion", "a", IParameters.Create());
        await service.NavigateAsync("MainRegion", "b", IParameters.Create());
        await service.NavigateAsync("MainRegion", "c", IParameters.Create());

        Assert.True((await service.GoBackAsync("MainRegion")).Result);
        Assert.True((await service.GoBackAsync("MainRegion")).Result);
    }

    /// <summary>
    /// 深度为 1 时永远无法回退：栈里最多只有一条。
    /// </summary>
    [Fact]
    public async Task HistoryDepth_One_NeverAllowsGoBack()
    {
        var (service, _) = CreateService(depth: 1, new FakeView("a"), new FakeView("b"));

        await service.NavigateAsync("MainRegion", "a", IParameters.Create());
        await service.NavigateAsync("MainRegion", "b", IParameters.Create());

        Assert.False((await service.GoBackAsync("MainRegion")).Result);
    }

    /// <summary>
    /// 深度恰好等于导航次数时，最早的一条仍可回退。
    /// </summary>
    [Fact]
    public async Task HistoryDepth_ExactlyMatchesNavigations_KeepsOldest()
    {
        var (service, region) = CreateService(depth: 3, new FakeView("a"), new FakeView("b"), new FakeView("c"));

        await service.NavigateAsync("MainRegion", "a", IParameters.Create());
        await service.NavigateAsync("MainRegion", "b", IParameters.Create());
        await service.NavigateAsync("MainRegion", "c", IParameters.Create());

        Assert.True((await service.GoBackAsync("MainRegion")).Result);
        Assert.Equal("b", Assert.Single(region.ActiveViews).ViewName);
        Assert.True((await service.GoBackAsync("MainRegion")).Result);
        Assert.Equal("a", Assert.Single(region.ActiveViews).ViewName);
        Assert.False((await service.GoBackAsync("MainRegion")).Result);
    }

    /// <summary>
    /// 超出深度一位时最旧条目被丢弃，回退次数正好等于深度减一。
    /// </summary>
    [Fact]
    public async Task HistoryDepth_ExceededByOne_DropsOnlyOldest()
    {
        var (service, _) = CreateService(
            depth: 2,
            new FakeView("a"),
            new FakeView("b"),
            new FakeView("c")
        );

        await service.NavigateAsync("MainRegion", "a", IParameters.Create());
        await service.NavigateAsync("MainRegion", "b", IParameters.Create());
        await service.NavigateAsync("MainRegion", "c", IParameters.Create());

        // 深度 2 保留 b、c：只能回退一次。
        Assert.True((await service.GoBackAsync("MainRegion")).Result);
        Assert.False((await service.GoBackAsync("MainRegion")).Result);
    }

    /// <summary>
    /// 区域名称为空时回退抛出参数异常，而不是静默失败。
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task GoBackAsync_EmptyRegionName_Throws(string? regionName)
    {
        var (service, _) = CreateService(new FakeView("a"));

        await Assert.ThrowsAnyAsync<ArgumentException>(
            async () => await service.GoBackAsync(regionName!)
        );
    }

    /// <summary>
    /// 回退失败时不弹出历史，后续回退仍能回到原目标。
    /// </summary>
    /// <remarks>
    /// 历史栈的弹出必须与回退成功绑定：先 peek 拿到目标，导航成功后才 pop。
    /// 若在导航前就弹出，一次失败的回退会永久丢掉一条历史，
    /// 用户再点后退会跳过本该回到的视图。
    /// </remarks>
    [Fact]
    public async Task GoBackAsync_FailedNavigation_KeepsHistoryIntact()
    {
        var throwingViewModel = new ThrowingOnceViewModel();
        var (service, region) = CreateService(
            new FakeView("a"),
            new FakeView("b", throwingViewModel),
            new FakeView("c")
        );

        await service.NavigateAsync("MainRegion", "a", IParameters.Create());
        await service.NavigateAsync("MainRegion", "b", IParameters.Create());
        await service.NavigateAsync("MainRegion", "c", IParameters.Create());

        // 第一次回退到 b 失败：b 的 OnNavigatedToAsync 抛异常。
        var failed = await service.GoBackAsync("MainRegion");
        Assert.False(failed.Result);
        Assert.Equal("c", Assert.Single(region.ActiveViews).ViewName);

        // 历史未被破坏，第二次回退仍应尝试回到 b 并成功。
        var second = await service.GoBackAsync("MainRegion");
        Assert.True(second.Result);
        Assert.Equal("b", Assert.Single(region.ActiveViews).ViewName);

        // 再回退一次回到 a，说明 b 只被消费了一次。
        var third = await service.GoBackAsync("MainRegion");
        Assert.True(third.Result);
        Assert.Equal("a", Assert.Single(region.ActiveViews).ViewName);
    }

    /// <summary>
    /// 回退成功后才弹出历史，连续回退不会重复命中同一目标。
    /// </summary>
    [Fact]
    public async Task GoBackAsync_SuccessfulNavigation_PopsExactlyOneEntry()
    {
        var (service, region) = CreateService(new FakeView("a"), new FakeView("b"), new FakeView("c"));

        await service.NavigateAsync("MainRegion", "a", IParameters.Create());
        await service.NavigateAsync("MainRegion", "b", IParameters.Create());
        await service.NavigateAsync("MainRegion", "c", IParameters.Create());

        Assert.True((await service.GoBackAsync("MainRegion")).Result);
        Assert.Equal("b", Assert.Single(region.ActiveViews).ViewName);

        Assert.True((await service.GoBackAsync("MainRegion")).Result);
        Assert.Equal("a", Assert.Single(region.ActiveViews).ViewName);

        // 历史已耗尽，不会重复回到 b。
        Assert.False((await service.GoBackAsync("MainRegion")).Result);
        Assert.Equal("a", Assert.Single(region.ActiveViews).ViewName);
    }

    /// <summary>
    /// 只在第二次进入时抛异常的视图模型，用于验证回退失败后的历史状态。
    /// </summary>
    private sealed class ThrowingOnceViewModel : INavigationAware
    {
        private int _calls;

        public Task<bool> IsNavigationTargetAsync(INavigationContext navigationContext) =>
            Task.FromResult(true);

        public Task OnNavigatedFromAsync(INavigationContext navigationContext) => Task.CompletedTask;

        public Task OnNavigatedToAsync(INavigationContext navigationContext)
        {
            _calls++;

            // 第一次是正常导航到 b，第二次是回退到 b——此时抛异常。
            return _calls == 2
                ? throw new InvalidOperationException("回退到 b 失败。")
                : Task.CompletedTask;
        }
    }

    private static (NavigationService Service, Region Region) CreateService(params FakeView[] views) =>
        CreateService(depth: NavigationOptions.DefaultHistoryDepth, views);

    private static (NavigationService Service, Region Region) CreateService(
        int depth,
        params FakeView[] views
    )
    {
        var region = new SingleActiveRegion("MainRegion", "MainView");
        var provider = new RegionProvider();
        provider.Add(region);

        return (BuildService(provider, depth, views), region);
    }

    private static (NavigationService Service, Region Region) CreateService(
        FakeUiDispatcher dispatcher,
        params FakeView[] views
    )
    {
        var region = new SingleActiveRegion("MainRegion", "MainView");
        var provider = new RegionProvider();
        provider.Add(region);

        return (BuildService(provider, NavigationOptions.DefaultHistoryDepth, dispatcher, views), region);
    }

    private static (NavigationService Service, Region First, Region Second) CreateServiceWithTwoRegions(
        params FakeView[] views
    )
    {
        var first = new SingleActiveRegion("FirstRegion", "MainView");
        var second = new SingleActiveRegion("SecondRegion", "MainView");
        var provider = new RegionProvider();
        provider.Add(first);
        provider.Add(second);

        return (BuildService(provider, NavigationOptions.DefaultHistoryDepth, views), first, second);
    }

    private static (NavigationService Service, Region Region) CreateServiceWithFactory(
        List<FakeView> created,
        params string[] viewNames
    )
    {
        var region = new SingleActiveRegion("MainRegion", "MainView");
        var provider = new RegionProvider();
        provider.Add(region);

        var registry = new ViewScopeRegistry();

        // 挂上保活行为：不保活的瞬态视图在失活时被移出区域并释放，
        // 回退才能走到"重新解析新实例"这条路径。
        region.Behaviors.Add(
            nameof(RegionMemberLifetimeBehavior),
            new RegionMemberLifetimeBehavior(provider, registry)
        );

        var services = new ServiceCollection();
        services.AddLogging();

        foreach (var viewName in viewNames)
        {
            services.AddKeyedTransient<IView>(viewName, (_, _) =>
            {
                var view = new FakeView(viewName);
                created.Add(view);
                return view;
            });
        }

        var serviceProvider = services.BuildServiceProvider();

        return (
            BuildService(provider, serviceProvider, new FakeUiDispatcher(), NavigationOptions.DefaultHistoryDepth, registry),
            region
        );
    }

    private static NavigationService BuildService(
        RegionProvider provider,
        int depth,
        params FakeView[] views
    ) => BuildService(provider, depth, new FakeUiDispatcher(), views);

    private static NavigationService BuildService(
        RegionProvider provider,
        int depth,
        FakeUiDispatcher dispatcher,
        params FakeView[] views
    )
    {
        var services = new ServiceCollection();
        services.AddLogging();

        foreach (var view in views)
        {
            services.AddKeyedSingleton<IView>(view.ViewName, view);
        }

        return BuildService(provider, services.BuildServiceProvider(), dispatcher, depth);
    }

    private static NavigationService BuildService(
        RegionProvider provider,
        IServiceProvider serviceProvider,
        FakeUiDispatcher dispatcher,
        int depth,
        IViewScopeRegistry? registry = null
    ) =>
        new(
            new RegionFinder(provider),
            provider,
            serviceProvider,
            dispatcher,
            new NavigationViewResolver(serviceProvider, registry ?? new ViewScopeRegistry()),
            Options.Create(new NavigationOptions { HistoryDepth = depth })
        );

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        for (var i = 0; i < 200 && !condition(); i++)
        {
            await Task.Delay(10);
        }

        Assert.True(condition(), "等待条件超时。");
    }

    /// <summary>
    /// 记录导航回调与参数的视图模型。
    /// </summary>
    private sealed class RecordingViewModel : INavigationAware
    {
        public int NavigatedToCount { get; private set; }

        public IParameters? LastParameters { get; private set; }

        public Task<bool> IsNavigationTargetAsync(INavigationContext navigationContext) =>
            Task.FromResult(true);

        public Task OnNavigatedFromAsync(INavigationContext navigationContext) => Task.CompletedTask;

        public Task OnNavigatedToAsync(INavigationContext navigationContext)
        {
            NavigatedToCount++;
            LastParameters = navigationContext.Parameters;

            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// 在 <c>OnNavigatedToAsync</c> 里挂起直到外部放行的视图模型。
    /// </summary>
    private sealed class GatedViewModel(Task gate, List<string> events, string name) : INavigationAware
    {
        public Task<bool> IsNavigationTargetAsync(INavigationContext navigationContext) =>
            Task.FromResult(true);

        public Task OnNavigatedFromAsync(INavigationContext navigationContext) => Task.CompletedTask;

        public async Task OnNavigatedToAsync(INavigationContext navigationContext)
        {
            events.Add($"{name}:to:enter");
            await gate;
            events.Add($"{name}:to:exit");
        }
    }

    /// <summary>
    /// 只在第二次进入时挂起的视图模型，用于制造"回退被挡住"的场景。
    /// </summary>
    private sealed class GateOnSecondEntryViewModel(Task gate, List<string> events) : INavigationAware
    {
        private int _entries;

        public Task<bool> IsNavigationTargetAsync(INavigationContext navigationContext) =>
            Task.FromResult(true);

        public Task OnNavigatedFromAsync(INavigationContext navigationContext) => Task.CompletedTask;

        public async Task OnNavigatedToAsync(INavigationContext navigationContext)
        {
            events.Add("b:to:enter");

            // 首次导航放行，第二次（回退）挂起。
            if (++_entries > 1)
            {
                await gate;
            }

            events.Add("b:to:exit");
        }
    }

    /// <summary>
    /// 只实现两个导航重载的最小实现，用于验证 <c>GoBackAsync</c> 的默认实现。
    /// </summary>
    private sealed class MinimalNavigationService : INavigationService
    {
        public ValueTask<INavigationResult> NavigateAsync(
            string regionName,
            string viewName,
            IParameters navigationParameters,
            CancellationToken cancellationToken = default
        ) => throw new NotSupportedException();

        public ValueTask<INavigationResult> NavigateAsync(
            string regionName,
            string viewName,
            CancellationToken cancellationToken = default
        ) => throw new NotSupportedException();
    }
}