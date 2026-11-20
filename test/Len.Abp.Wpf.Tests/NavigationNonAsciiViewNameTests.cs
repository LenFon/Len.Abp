using Len.Abp.Wpf.Mvvm;
using Len.Abp.Wpf.Navigation;
using Len.Abp.Wpf.Navigation.Regions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Len.Abp.Wpf.Tests;

/// <summary>
/// 覆盖：非 ASCII 与含空格的视图名必须能正常导航。
/// </summary>
/// <remarks>
/// 单元层只验证 <see cref="NavigationUri.GetViewName"/> 的还原；
/// 这里验证端到端——导航服务拿到的视图名与容器里的注册键一致，
/// 否则 <c>GetRequiredKeyedService</c> 会抛异常，使用者看到的是
/// "请确认该视图已标注 [View]" 这种方向错误的提示。
/// </remarks>
public class NavigationNonAsciiViewNameTests
{
    /// <summary>
    /// 中文视图名能解析到容器中注册的视图。
    /// </summary>
    [Theory]
    [InlineData("页面")]
    [InlineData("用户列表")]
    [InlineData("设置页")]
    public async Task NavigateAsync_ChineseViewName_ResolvesView(string viewName)
    {
        var view = new FakeView(viewName);
        var (service, region) = CreateService(view);

        var result = await service.NavigateAsync("MainRegion", viewName, IParameters.Create());

        Assert.True(result.Result, result.Exception?.ToString());
        Assert.True(region.ActiveViews.Contains(view));
    }

    /// <summary>
    /// 含空格的视图名能解析到容器中注册的视图。
    /// </summary>
    [Theory]
    [InlineData("my view")]
    [InlineData("user list")]
    public async Task NavigateAsync_ViewNameWithSpaces_ResolvesView(string viewName)
    {
        var view = new FakeView(viewName);
        var (service, region) = CreateService(view);

        var result = await service.NavigateAsync("MainRegion", viewName, IParameters.Create());

        Assert.True(result.Result, result.Exception?.ToString());
        Assert.True(region.ActiveViews.Contains(view));
    }

    /// <summary>
    /// 中文视图名带查询参数时，视图名与参数都被正确解析。
    /// </summary>
    [Fact]
    public async Task NavigateAsync_ChineseViewNameWithQuery_ResolvesViewAndParameters()
    {
        var view = new FakeView("详情页");
        var (service, region) = CreateService(view);

        var result = await service.NavigateAsync("MainRegion", "详情页?id=7", IParameters.Create());

        Assert.True(result.Result, result.Exception?.ToString());
        Assert.Equal(7, result.Context.Parameters.GetValue<int>("id"));
        Assert.True(region.ActiveViews.Contains(view));
    }

    /// <summary>
    /// 中文视图名与 ASCII 视图名在同一区域中可来回切换。
    /// </summary>
    [Fact]
    public async Task NavigateAsync_AlternatingChineseAndAscii_SwitchesViews()
    {
        var chinese = new FakeView("页面");
        var ascii = new FakeView("demo");
        var (service, region) = CreateService(chinese, ascii);

        Assert.True((await service.NavigateAsync("MainRegion", "页面", IParameters.Create())).Result);
        Assert.True(region.ActiveViews.Contains(chinese));

        Assert.True((await service.NavigateAsync("MainRegion", "demo", IParameters.Create())).Result);
        Assert.True(region.ActiveViews.Contains(ascii));

        Assert.True((await service.NavigateAsync("MainRegion", "页面", IParameters.Create())).Result);
        Assert.True(region.ActiveViews.Contains(chinese));
    }

    /// <summary>
    /// 中文视图名参与导航历史与回退。
    /// </summary>
    /// <remarks>
    /// 历史条目保存的是视图名原文，回退时按名称重新解析。
    /// 若还原逻辑只在首次导航生效、历史里存的是转义形式，回退会失败。
    /// </remarks>
    [Fact]
    public async Task GoBackAsync_ChineseViewName_RestoresPreviousView()
    {
        var first = new FakeView("首页");
        var second = new FakeView("详情页");
        var (service, region) = CreateService(first, second);

        Assert.True((await service.NavigateAsync("MainRegion", "首页", IParameters.Create())).Result);
        Assert.True((await service.NavigateAsync("MainRegion", "详情页", IParameters.Create())).Result);

        var back = await service.GoBackAsync("MainRegion");

        Assert.True(back.Result, back.Exception?.ToString());
        Assert.True(region.ActiveViews.Contains(first));
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

        return (
            new NavigationService(
                new RegionFinder(provider),
                provider,
                serviceProvider,
                new FakeUiDispatcher(),
                new NavigationViewResolver(serviceProvider, new ViewScopeRegistry()),
                Options.Create(new NavigationOptions())
            ),
            region
        );
    }
}
