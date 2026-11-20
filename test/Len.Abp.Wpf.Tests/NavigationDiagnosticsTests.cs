using Len.Abp.Wpf.Mvvm;
using Len.Abp.Wpf.Navigation;
using Len.Abp.Wpf.Navigation.Regions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Len.Abp.Wpf.Tests;

/// <summary>
/// 覆盖：导航失败时能从日志与异常消息定位到具体视图名/区域名。
/// </summary>
/// <remarks>
/// 三种失败各自可诊断：区域不存在（Warning + 可用区域列表）、
/// 视图解析失败（Error + 视图名）、导航回调异常（Error + 视图名与阶段）。
/// </remarks>
public class NavigationDiagnosticsTests
{
    /// <summary>
    /// 区域不存在时记录 Warning，并列出已注册的区域名。
    /// </summary>
    [Fact]
    public async Task NavigateAsync_UnknownRegion_LogsWarningWithAvailableRegions()
    {
        var (service, logger) = CreateService(new FakeView("demo"));

        var result = await service.NavigateAsync("MissingRegion", "demo", IParameters.Create());

        Assert.False(result.Result);
        Assert.Null(result.Exception);

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Contains("MissingRegion", entry.Message);
        Assert.Contains("MainRegion", entry.Message);
    }

    /// <summary>
    /// 没有任何已注册区域时，日志明确写出"（无）"，而不是留空。
    /// </summary>
    [Fact]
    public async Task NavigateAsync_UnknownRegionWithoutAnyRegion_LogsEmptyMarker()
    {
        var (service, logger) = CreateService(includeRegion: false);

        await service.NavigateAsync("MissingRegion", "demo", IParameters.Create());

        var entry = Assert.Single(logger.Entries);
        Assert.Contains("（无）", entry.Message);
    }

    /// <summary>
    /// 视图未注册时记录 Error，异常消息包含视图名与区域名。
    /// </summary>
    [Fact]
    public async Task NavigateAsync_UnregisteredView_LogsErrorAndMentionsViewName()
    {
        var (service, logger) = CreateService(new FakeView("demo"));

        var result = await service.NavigateAsync("MainRegion", "missing", IParameters.Create());

        Assert.False(result.Result);

        var exception = Assert.IsType<InvalidOperationException>(result.Exception);
        Assert.Contains("missing", exception.Message);
        Assert.Contains("MainRegion", exception.Message);
        Assert.Contains("[View]", exception.Message);

        // 原始容器异常必须保留为内部异常，否则丢失根因。
        Assert.NotNull(exception.InnerException);

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Same(exception.InnerException, entry.Exception);
        Assert.Contains("missing", entry.Message);
        Assert.Contains("MainRegion", entry.Message);
    }

    /// <summary>
    /// OnNavigatedToAsync 抛异常时记录 Error，日志包含视图名与阶段。
    /// </summary>
    [Fact]
    public async Task NavigateAsync_NavigatedToThrows_LogsErrorWithViewNameAndStage()
    {
        var (service, logger) = CreateService(new FakeView("target", new ThrowingToViewModel()));

        var result = await service.NavigateAsync("MainRegion", "target", IParameters.Create());

        Assert.False(result.Result);

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Contains("target", entry.Message);
        Assert.Contains("OnNavigatedToAsync", entry.Message);
        Assert.Same(result.Exception, entry.Exception);
    }

    /// <summary>
    /// OnNavigatedFromAsync 抛异常时记录 Error，日志包含离开视图名与阶段。
    /// </summary>
    [Fact]
    public async Task NavigateAsync_NavigatedFromThrows_LogsErrorWithViewNameAndStage()
    {
        var (service, logger) = CreateService(
            new FakeView("current", new ThrowingFromViewModel()),
            new FakeView("target")
        );

        await service.NavigateAsync("MainRegion", "current", IParameters.Create());

        var result = await service.NavigateAsync("MainRegion", "target", IParameters.Create());

        Assert.False(result.Result);

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Contains("current", entry.Message);
        Assert.Contains("OnNavigatedFromAsync", entry.Message);
    }

    /// <summary>
    /// 成功导航不产生任何日志，避免正常路径刷日志。
    /// </summary>
    [Fact]
    public async Task NavigateAsync_Success_LogsNothing()
    {
        var (service, logger) = CreateService(new FakeView("demo"));

        var result = await service.NavigateAsync("MainRegion", "demo", IParameters.Create());

        Assert.True(result.Result);
        Assert.Empty(logger.Entries);
    }

    /// <summary>
    /// 容器未注册日志服务时导航仍能完成，不因缺少 <see cref="ILogger"/> 而失败。
    /// </summary>
    [Fact]
    public async Task NavigateAsync_WithoutLogging_StillWorks()
    {
        var region = new SingleActiveRegion("MainRegion", "MainView");
        var provider = new RegionProvider();
        provider.Add(region);

        var services = new ServiceCollection();
        services.AddKeyedSingleton<IView>("demo", new FakeView("demo"));
        var serviceProvider = services.BuildServiceProvider();

        var service = new NavigationService(
            new RegionFinder(provider),
            provider,
            serviceProvider,
            new FakeUiDispatcher(),
            new NavigationViewResolver(serviceProvider, new ViewScopeRegistry()),
            Options.Create(new NavigationOptions())
        );

        var success = await service.NavigateAsync("MainRegion", "demo", IParameters.Create());
        var missingRegion = await service.NavigateAsync("MissingRegion", "demo", IParameters.Create());
        var missingView = await service.NavigateAsync("MainRegion", "missing", IParameters.Create());

        Assert.True(success.Result);
        Assert.False(missingRegion.Result);
        Assert.False(missingView.Result);
        Assert.IsType<InvalidOperationException>(missingView.Exception);
    }

    /// <summary>
    /// 没有更早历史时回退只记录 Debug，不产生 Warning/Error 噪音。
    /// </summary>
    /// <remarks>
    /// 日志工厂默认过滤 Debug，这里显式放开最小级别，否则测不到这条分支。
    /// </remarks>
    [Fact]
    public async Task GoBackAsync_EmptyHistory_LogsDebugOnly()
    {
        var (service, logger) = CreateService(LogLevel.Debug, new FakeView("demo"));

        await service.NavigateAsync("MainRegion", "demo", IParameters.Create());
        var result = await service.GoBackAsync("MainRegion");

        Assert.False(result.Result);

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Debug, entry.Level);
        Assert.Contains("MainRegion", entry.Message);
    }

    private static (NavigationService Service, RecordingLoggerProvider Logger) CreateService(
        params FakeView[] views
    ) => CreateService(includeRegion: true, views);

    private static (NavigationService Service, RecordingLoggerProvider Logger) CreateService(
        bool includeRegion,
        params FakeView[] views
    ) => CreateService(LogLevel.Information, includeRegion, views);

    private static (NavigationService Service, RecordingLoggerProvider Logger) CreateService(
        LogLevel minimumLevel,
        params FakeView[] views
    ) => CreateService(minimumLevel, includeRegion: true, views);

    private static (NavigationService Service, RecordingLoggerProvider Logger) CreateService(
        LogLevel minimumLevel,
        bool includeRegion,
        params FakeView[] views
    )
    {
        var provider = new RegionProvider();

        if (includeRegion)
        {
            var region = new SingleActiveRegion("MainRegion", "MainView");
            provider.Add(region);
        }

        var logger = new RecordingLoggerProvider();
        var services = new ServiceCollection();
        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(minimumLevel);
            builder.AddProvider(logger);
        });

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
            new NavigationViewResolver(serviceProvider, new ViewScopeRegistry()),
            Options.Create(new NavigationOptions())
        );

        return (service, logger);
    }

    /// <summary>
    /// OnNavigatedToAsync 抛异常的视图模型。
    /// </summary>
    private sealed class ThrowingToViewModel : INavigationAware
    {
        public Task<bool> IsNavigationTargetAsync(INavigationContext navigationContext) =>
            Task.FromResult(true);

        public Task OnNavigatedFromAsync(INavigationContext navigationContext) =>
            Task.CompletedTask;

        public Task OnNavigatedToAsync(INavigationContext navigationContext) =>
            throw new InvalidOperationException("进入失败");
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
}
