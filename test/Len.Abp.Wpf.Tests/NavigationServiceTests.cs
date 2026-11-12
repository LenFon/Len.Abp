using Len.Abp.Wpf.Mvvm;
using Len.Abp.Wpf.Navigation;
using Len.Abp.Wpf.Navigation.Regions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

// 附加属性 Len.Abp.Wpf.Navigation.Region 与区域实现 Len.Abp.Wpf.Navigation.Regions.Region 同名。
using RegionImpl = Len.Abp.Wpf.Navigation.Regions.Region;

namespace Len.Abp.Wpf.Tests;

/// <summary>
/// 覆盖 <see cref="NavigationService"/> 的视图解析、激活与生命周期通知。
/// </summary>
public class NavigationServiceTests
{
    /// <summary>
    /// 导航到已注册的视图时解析视图、加入区域并激活。
    /// </summary>
    [Fact]
    public async Task NavigateAsync_RegisteredView_AddsAndActivatesView()
    {
        var view = new FakeView("demo");
        var (service, region) = CreateService(view);

        var result = await service.NavigateAsync("MainRegion", "demo", IParameters.Create());

        Assert.True(result.Result);
        Assert.Null(result.Exception);
        Assert.True(region.Views.Contains(view));
        Assert.True(region.ActiveViews.Contains(view));
    }

    /// <summary>
    /// 区域不存在时返回失败结果而不抛异常。
    /// </summary>
    [Fact]
    public async Task NavigateAsync_UnknownRegion_ReturnsFailedResult()
    {
        var (service, _) = CreateService(new FakeView("demo"));

        var result = await service.NavigateAsync("MissingRegion", "demo", IParameters.Create());

        Assert.False(result.Result);
        Assert.Null(result.Exception);
        Assert.Equal("demo", NavigationUri.GetViewName(result.Context.Uri));
    }

    /// <summary>
    /// 视图名称对应的键控服务未注册时返回带异常的结果。
    /// </summary>
    [Fact]
    public async Task NavigateAsync_UnregisteredView_ReturnsResultWithException()
    {
        var (service, _) = CreateService(new FakeView("demo"));

        var result = await service.NavigateAsync("MainRegion", "missing", IParameters.Create());

        Assert.False(result.Result);
        Assert.NotNull(result.Exception);
    }

    /// <summary>
    /// 视图名称为空时抛出异常。
    /// </summary>
    [Fact]
    public async Task NavigateAsync_EmptyViewName_Throws()
    {
        var (service, _) = CreateService(new FakeView("demo"));

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await service.NavigateAsync("MainRegion", string.Empty, IParameters.Create())
        );
    }

    /// <summary>
    /// 导航参数会传入导航上下文。
    /// </summary>
    [Fact]
    public async Task NavigateAsync_PassesParametersToContext()
    {
        var view = new FakeView("demo");
        var (service, _) = CreateService(view);

        var result = await service.NavigateAsync("MainRegion", "demo", IParameters.Create("?id=42"));

        Assert.Equal(42, result.Context.Parameters.GetValue<int>("id"));
    }

    /// <summary>
    /// 已存在于区域中的视图若声明为导航目标则复用，不再从容器解析。
    /// </summary>
    [Fact]
    public async Task NavigateAsync_ExistingNavigationTarget_ReusesView()
    {
        var viewModel = new NavigationAwareViewModel { IsTarget = true };
        var existing = new FakeView("demo", viewModel);
        var (service, region) = CreateService(new FakeView("other"));
        region.Add(existing);

        var result = await service.NavigateAsync("MainRegion", "demo", IParameters.Create());

        Assert.True(result.Result);
        Assert.Single(region.Views);
        Assert.True(region.ActiveViews.Contains(existing));
        Assert.Equal(1, viewModel.NavigatedToCount);
    }

    /// <summary>
    /// 视图模型未实现 <see cref="INavigationAware"/> 时，同名视图仍应复用，
    /// 否则每次导航都会解析新实例并累积在区域中。
    /// </summary>
    [Fact]
    public async Task NavigateAsync_PlainViewModel_ReusesExistingView()
    {
        var existing = new FakeView("demo");
        var (service, region) = CreateService();
        region.Add(existing);

        var first = await service.NavigateAsync("MainRegion", "demo", IParameters.Create());
        var second = await service.NavigateAsync("MainRegion", "demo", IParameters.Create());

        Assert.True(first.Result);
        Assert.True(second.Result);
        Assert.Single(region.Views);
        Assert.True(region.ActiveViews.Contains(existing));
    }

    /// <summary>
    /// 目标视图解析失败时，当前活动视图不应收到 OnNavigatedFrom，
    /// 否则视图模型认为自己已离开，而界面仍显示它。
    /// </summary>
    [Fact]
    public async Task NavigateAsync_UnresolvableTarget_DoesNotNotifyActiveViewNavigatedFrom()
    {
        var viewModel = new NavigationAwareViewModel();
        var active = new FakeView("demo", viewModel);
        var (service, region) = CreateService();
        region.Add(active);
        region.Activate(active);

        var result = await service.NavigateAsync("MainRegion", "missing", IParameters.Create());

        Assert.False(result.Result);
        Assert.NotNull(result.Exception);
        Assert.Equal(0, viewModel.NavigatedFromCount);
        Assert.True(region.ActiveViews.Contains(active));
    }

    /// <summary>
    /// 已存在的视图若声明不是导航目标则从容器解析新视图。
    /// </summary>
    [Fact]
    public async Task NavigateAsync_ExistingNonTarget_ResolvesNewView()
    {
        var existing = new FakeView("demo", new NavigationAwareViewModel { IsTarget = false });
        var fresh = new FakeView("demo");
        var (service, region) = CreateService(fresh);
        region.Add(existing);

        var result = await service.NavigateAsync("MainRegion", "demo", IParameters.Create());

        Assert.True(result.Result);
        Assert.Equal(2, region.Views.Count);
        Assert.True(region.ActiveViews.Contains(fresh));
    }

    /// <summary>
    /// 导航离开时通知当前活动视图的 OnNavigatedFromAsync。
    /// </summary>
    [Fact]
    public async Task NavigateAsync_NotifiesActiveViewNavigatedFrom()
    {
        var firstViewModel = new NavigationAwareViewModel();
        var first = new FakeView("first", firstViewModel);
        var second = new FakeView("second");
        var (service, region) = CreateService(first, second);
        region.Add(first);
        region.Activate(first);

        await service.NavigateAsync("MainRegion", "second", IParameters.Create());

        Assert.Equal(1, firstViewModel.NavigatedFromCount);
    }

    /// <summary>
    /// 导航到目标视图时通知其 OnNavigatedToAsync。
    /// </summary>
    [Fact]
    public async Task NavigateAsync_NotifiesTargetViewNavigatedTo()
    {
        var viewModel = new NavigationAwareViewModel();
        var view = new FakeView("demo", viewModel);
        var (service, _) = CreateService(view);

        await service.NavigateAsync("MainRegion", "demo", IParameters.Create());

        Assert.Equal(1, viewModel.NavigatedToCount);
    }

    /// <summary>
    /// 每次导航都触发回调：离开的视图收到 OnNavigatedFrom，进入的视图收到 OnNavigatedTo。
    /// </summary>
    /// <remarks>
    /// 视图被保活复用时，回调仍按次触发，视图模型可以据此刷新数据，
    /// 不能因为实例复用就跳过通知。
    /// </remarks>
    [Fact]
    public async Task NavigateAsync_RepeatedNavigation_NotifiesEveryTime()
    {
        var firstViewModel = new NavigationAwareViewModel();
        var secondViewModel = new NavigationAwareViewModel();
        var first = new FakeView("first", firstViewModel);
        var second = new FakeView("second", secondViewModel);
        var (service, _) = CreateService(first, second);

        await service.NavigateAsync("MainRegion", "first", IParameters.Create());
        await service.NavigateAsync("MainRegion", "second", IParameters.Create());
        await service.NavigateAsync("MainRegion", "first", IParameters.Create());

        Assert.Equal(2, firstViewModel.NavigatedToCount);
        Assert.Equal(1, firstViewModel.NavigatedFromCount);
        Assert.Equal(1, secondViewModel.NavigatedToCount);
        Assert.Equal(1, secondViewModel.NavigatedFromCount);
    }

    /// <summary>
    /// 导航到当前已激活的同一视图时，仍会先触发 OnNavigatedFrom 再触发 OnNavigatedTo。
    /// </summary>
    [Fact]
    public async Task NavigateAsync_SameViewTwice_NotifiesFromThenTo()
    {
        var viewModel = new NavigationAwareViewModel();
        var view = new FakeView("demo", viewModel);
        var (service, _) = CreateService(view);

        await service.NavigateAsync("MainRegion", "demo", IParameters.Create());
        await service.NavigateAsync("MainRegion", "demo", IParameters.Create());

        Assert.Equal(2, viewModel.NavigatedToCount);
        Assert.Equal(1, viewModel.NavigatedFromCount);
    }

    /// <summary>
    /// 视图模型的 OnNavigatedToAsync 抛异常时返回带异常的结果。
    /// </summary>
    [Fact]
    public async Task NavigateAsync_ViewModelThrows_ReturnsResultWithException()
    {
        var viewModel = new NavigationAwareViewModel { ThrowOnNavigatedTo = true };
        var view = new FakeView("demo", viewModel);
        var (service, _) = CreateService(view);

        var result = await service.NavigateAsync("MainRegion", "demo", IParameters.Create());

        Assert.False(result.Result);
        Assert.IsType<InvalidOperationException>(result.Exception);
    }

    /// <summary>
    /// 视图模型未实现 INavigationAware 时导航照常完成。
    /// </summary>
    [Fact]
    public async Task NavigateAsync_PlainViewModel_Succeeds()
    {
        var view = new FakeView("demo", new object());
        var (service, _) = CreateService(view);

        var result = await service.NavigateAsync("MainRegion", "demo", IParameters.Create());

        Assert.True(result.Result);
    }

    /// <summary>
    /// 使用 URI 重载时同样能完成导航。
    /// </summary>
    [Fact]
    public async Task NavigateAsync_UriOverload_Navigates()
    {
        var view = new FakeView("demo");
        var (service, region) = CreateService(view);

        var result = await service.NavigateAsync("MainRegion", new Uri("demo?id=7", UriKind.Relative), IParameters.Create());

        Assert.True(result.Result);
        Assert.Equal(7, result.Context.Parameters.GetValue<int>("id"));
        Assert.True(region.ActiveViews.Contains(view));
    }

    private static (NavigationService Service, RegionImpl Region) CreateService(params FakeView[] views)
    {
        // 与 ContentControlRegionAdapter 保持一致：内容控件区域同一时刻只有一个活动视图。
        var region = new SingleActiveRegion("MainRegion", "MainView");
        var provider = new RegionProvider();
        provider.Add(region);

        var services = new ServiceCollection();
        foreach (var view in views)
        {
            services.AddKeyedSingleton<IView>(view.ViewName, view);
        }

        var serviceProvider = services.BuildServiceProvider();
        var service = new NavigationService(new RegionFinder(provider), serviceProvider);

        return (service, region);
    }

    private sealed class NavigationAwareViewModel : INavigationAware
    {
        public bool IsTarget { get; init; } = true;

        public bool ThrowOnNavigatedTo { get; init; }

        public int NavigatedToCount { get; private set; }

        public int NavigatedFromCount { get; private set; }

        public Task<bool> IsNavigationTargetAsync(INavigationContext navigationContext) => Task.FromResult(IsTarget);

        public Task OnNavigatedFromAsync(INavigationContext navigationContext)
        {
            NavigatedFromCount++;
            return Task.CompletedTask;
        }

        public Task OnNavigatedToAsync(INavigationContext navigationContext)
        {
            NavigatedToCount++;

            return ThrowOnNavigatedTo
                ? throw new InvalidOperationException("导航失败")
                : Task.CompletedTask;
        }
    }
}
