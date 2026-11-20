using Len.Abp.Wpf.Mvvm;
using Len.Abp.Wpf.Navigation;
using Len.Abp.Wpf.Navigation.Regions;
using Len.Abp.Wpf.Navigation.Regions.Behaviors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Windows.Controls;
using Volo.Abp.DependencyInjection;
using Xunit;

namespace Len.Abp.Wpf.Tests;

/// <summary>
/// 覆盖 <see cref="ContentControlRegionAdapter"/> 与 <see cref="NavigationService"/> 的组合行为。
/// </summary>
/// <remarks>
/// 适配器在视图加入区域时自动激活它（方便直接使用 <see cref="IRegion"/> 的使用者），
/// 而导航服务假设 <c>Add</c> 不改变激活状态。两者各自的行为都合理，
/// 组合起来会让新视图先收到 <c>OnNavigatedFrom</c>。
/// 单独测适配器或单独测导航服务都发现不了，必须组合验证。
/// </remarks>
public class NavigationContentControlIntegrationTests
{
    /// <summary>
    /// 首次导航成功时，新视图只收到 <c>OnNavigatedTo</c>。
    /// </summary>
    /// <remarks>
    /// 回归：适配器在 <c>region.Add</c> 时自动激活新视图，
    /// 导航服务在解析之后才记录活动视图，于是把新视图自己算成"即将离开"，
    /// 先通知了一遍 <c>OnNavigatedFrom</c>。一个从未显示过的视图不该收到离开通知。
    /// </remarks>
    [Fact]
    public void FirstNavigation_NotifiesOnlyNavigatedTo()
    {
        Sta.Run(() =>
        {
            var log = new List<string>();
            var view = new FakeView("demo", new RecordingViewModel(log));
            var (service, _) = CreateService(view);

            var result = Navigate(service, "demo");

            Assert.True(result.Result);
            Assert.Equal(["To"], log);
        });
    }

    /// <summary>
    /// 首次导航时 <c>OnNavigatedTo</c> 抛异常，失败视图不得留在激活状态。
    /// </summary>
    /// <remarks>
    /// 自动激活让新视图在解析阶段就进入 <c>ActiveViews</c>，
    /// 若回滚时把它当成"导航前的活动视图"重新激活，界面会显示一个导航失败的视图。
    /// </remarks>
    [Fact]
    public void FirstNavigation_NavigatedToThrows_LeavesNoActiveView()
    {
        Sta.Run(() =>
        {
            var log = new List<string>();
            var view = new FakeView("demo", new ThrowingToViewModel(log));
            var (service, region) = CreateService(view);

            var result = Navigate(service, "demo");

            Assert.False(result.Result);
            Assert.Empty(region.ActiveViews);
        });
    }

    /// <summary>
    /// 首次导航参数校验失败时，视图不得被激活，也不得收到任何回调。
    /// </summary>
    /// <remarks>
    /// 校验发生在激活之前是导航服务的核心承诺。适配器的自动激活绕过了它，
    /// 视图会短暂出现在 <see cref="ContentControl"/> 上。
    /// </remarks>
    [Fact]
    public void FirstNavigation_ValidationFails_DoesNotActivateOrNotify()
    {
        Sta.Run(() =>
        {
            var log = new List<string>();
            var view = new FakeView("demo", new RecordingViewModel(log))
            {
                RequiredNavigationParameters = new Dictionary<string, Type?> { ["Id"] = typeof(int) },
            };
            var (service, region) = CreateService(view);

            var result = Navigate(service, "demo");

            Assert.False(result.Result);
            Assert.Empty(log);
            Assert.Empty(region.ActiveViews);
        });
    }

    /// <summary>
    /// 第二次导航到不同视图时，回调顺序仍是先离开旧视图、再进入新视图。
    /// </summary>
    /// <remarks>
    /// 适配器只在区域没有活动视图时自动激活，第二次导航走的是正常路径。
    /// 这条用例防止修复首次导航时把正常路径改坏。
    /// </remarks>
    [Fact]
    public void SecondNavigation_NotifiesFromThenTo()
    {
        Sta.Run(() =>
        {
            var log = new List<string>();
            var first = new FakeView("first", new RecordingViewModel(log));
            var second = new FakeView("second", new RecordingViewModel(log));
            var (service, _) = CreateService(first, second);

            Assert.True(Navigate(service, "first").Result);
            log.Clear();

            var result = Navigate(service, "second");

            Assert.True(result.Result);
            Assert.Equal(["From", "To"], log);
        });
    }

    /// <summary>
    /// 导航服务不依赖 <see cref="IRegion.Add"/> 是否改变激活状态。
    /// </summary>
    /// <remarks>
    /// 契约测试：<see cref="IRegion.Add"/> 本身不激活视图，
    /// 但适配器可以监听集合变更并自动激活。导航服务必须在 <c>Add</c> 之前
    /// 取活动视图快照，才能同时兼容"会激活"与"不会激活"两类适配器。
    /// 这里用一个不自动激活的适配器（<see cref="TabControlRegionAdapter"/>）验证同一套逻辑。
    /// </remarks>
    [Fact]
    public void Navigation_DoesNotDependOnAdapterAutoActivation()
    {
        Sta.Run(() =>
        {
            var log = new List<string>();
            var view = new FakeView("demo", new RecordingViewModel(log));

            var tabControl = new TabControl();
            var adapter = new TabControlRegionAdapter(
                new AbpLazyServiceProvider(new ServiceCollection().AddOptions().BuildServiceProvider())
            );
            var region = ((IRegionAdapter)adapter).Initialize(tabControl, "MainRegion", new FakeView("MainView"));

            var provider = new RegionProvider();
            provider.Add(region);

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddKeyedSingleton<IView>("demo", view);
            var serviceProvider = services.BuildServiceProvider();

            var registry = new ViewScopeRegistry();
            region.Behaviors.Add(
                nameof(RegionMemberLifetimeBehavior),
                new RegionMemberLifetimeBehavior(provider, registry)
            );

            var service = new NavigationService(
                new RegionFinder(provider),
                provider,
                serviceProvider,
                new FakeUiDispatcher(),
                new NavigationViewResolver(serviceProvider, registry),
                Options.Create(new NavigationOptions())
            );

            var result = Navigate(service, "demo");

            Assert.True(result.Result);
            Assert.Equal(["To"], log);
        });
    }

    private static INavigationResult Navigate(NavigationService service, string viewName) =>
        service.NavigateAsync("MainRegion", viewName, IParameters.Create()).AsTask().GetAwaiter().GetResult();

    private static (NavigationService Service, IRegion Region) CreateService(params FakeView[] views)
    {
        var contentControl = new ContentControl();
        var adapter = new ContentControlRegionAdapter(
            new AbpLazyServiceProvider(new ServiceCollection().AddOptions().BuildServiceProvider())
        );
        var region = ((IRegionAdapter)adapter).Initialize(contentControl, "MainRegion", new FakeView("MainView"));

        var provider = new RegionProvider();
        provider.Add(region);

        var services = new ServiceCollection();
        services.AddLogging();
        foreach (var view in views)
        {
            services.AddKeyedSingleton<IView>(view.ViewName, view);
        }

        var serviceProvider = services.BuildServiceProvider();
        var registry = new ViewScopeRegistry();
        region.Behaviors.Add(
            nameof(RegionMemberLifetimeBehavior),
            new RegionMemberLifetimeBehavior(provider, registry)
        );

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
    /// 记录导航回调顺序的视图模型。
    /// </summary>
    private sealed class RecordingViewModel(List<string> log) : INavigationAware
    {
        public Task<bool> IsNavigationTargetAsync(INavigationContext navigationContext) => Task.FromResult(true);

        public Task OnNavigatedFromAsync(INavigationContext navigationContext)
        {
            log.Add("From");
            return Task.CompletedTask;
        }

        public Task OnNavigatedToAsync(INavigationContext navigationContext)
        {
            log.Add("To");
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// <c>OnNavigatedTo</c> 抛异常的视图模型。
    /// </summary>
    private sealed class ThrowingToViewModel(List<string> log) : INavigationAware
    {
        public Task<bool> IsNavigationTargetAsync(INavigationContext navigationContext) => Task.FromResult(true);

        public Task OnNavigatedFromAsync(INavigationContext navigationContext)
        {
            log.Add("From");
            return Task.CompletedTask;
        }

        public Task OnNavigatedToAsync(INavigationContext navigationContext)
        {
            log.Add("To");
            throw new InvalidOperationException("导航失败");
        }
    }
}
