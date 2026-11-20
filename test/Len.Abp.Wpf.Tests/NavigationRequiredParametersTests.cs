using Len.Abp.Wpf.Mvvm;
using Len.Abp.Wpf.Navigation;
using Len.Abp.Wpf.Navigation.Regions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;


namespace Len.Abp.Wpf.Tests;

/// <summary>
/// 覆盖导航必需参数的校验：存在性、类型、失败时的状态一致性。
/// </summary>
public class NavigationRequiredParametersTests
{
    /// <summary>
    /// 提供了必需参数时导航成功。
    /// </summary>
    [Fact]
    public async Task NavigateAsync_RequiredParameterProvided_Succeeds()
    {
        var view = CreateView("demo", new Dictionary<string, Type?> { ["Id"] = typeof(int) });
        var (service, region) = CreateService(view);

        var result = await service.NavigateAsync("MainRegion", "demo", IParameters.Create("?Id=42"));

        Assert.True(result.Result);
        Assert.Null(result.Exception);
        Assert.True(region.ActiveViews.Contains(view));
    }

    /// <summary>
    /// 缺少必需参数时导航失败，异常消息包含参数名。
    /// </summary>
    [Fact]
    public async Task NavigateAsync_MissingRequiredParameter_Fails()
    {
        var view = CreateView("demo", new Dictionary<string, Type?> { ["Id"] = typeof(int) });
        var (service, _) = CreateService(view);

        var result = await service.NavigateAsync("MainRegion", "demo", IParameters.Create());

        Assert.False(result.Result);
        Assert.NotNull(result.Exception);
        Assert.Contains("Id", result.Exception!.Message);
        Assert.Contains("缺少参数", result.Exception.Message);
    }

    /// <summary>
    /// 参数类型不匹配时导航失败。
    /// </summary>
    [Fact]
    public async Task NavigateAsync_TypeMismatch_Fails()
    {
        var view = CreateView("demo", new Dictionary<string, Type?> { ["Id"] = typeof(int) });
        var (service, _) = CreateService(view);

        var result = await service.NavigateAsync("MainRegion", "demo", IParameters.Create("?Id=abc"));

        Assert.False(result.Result);
        Assert.NotNull(result.Exception);
        Assert.Contains("Id", result.Exception!.Message);
        Assert.Contains("类型不是", result.Exception.Message);
    }

    /// <summary>
    /// 只声明名称（类型为 null）时只校验存在性，任意值都通过。
    /// </summary>
    [Fact]
    public async Task NavigateAsync_NameOnlyParameter_AcceptsAnyValue()
    {
        var view = CreateView("demo", new Dictionary<string, Type?> { ["Source"] = null });
        var (service, _) = CreateService(view);

        var result = await service.NavigateAsync("MainRegion", "demo", IParameters.Create("?Source=anything"));

        Assert.True(result.Result);
    }

    /// <summary>
    /// 多传参数不影响导航。
    /// </summary>
    [Fact]
    public async Task NavigateAsync_ExtraParameters_AreIgnored()
    {
        var view = CreateView("demo", new Dictionary<string, Type?> { ["Id"] = typeof(int) });
        var (service, _) = CreateService(view);

        var result = await service.NavigateAsync("MainRegion", "demo", IParameters.Create("?Id=1&Extra=2"));

        Assert.True(result.Result);
    }

    /// <summary>
    /// 校验失败时当前活动视图不会收到 OnNavigatedFrom。
    /// </summary>
    /// <remarks>
    /// 校验发生在激活之前，失败时界面状态保持不变，与回滚语义一致。
    /// </remarks>
    [Fact]
    public async Task NavigateAsync_ValidationFails_DoesNotNotifyCurrentView()
    {
        var currentViewModel = new CountingViewModel();
        var currentView = CreateView("current", new Dictionary<string, Type?>(), currentViewModel);
        var targetView = CreateView("target", new Dictionary<string, Type?> { ["Id"] = typeof(int) });
        var (service, region) = CreateService(currentView, targetView);

        // 先导航到 current，使其成为活动视图。
        var first = await service.NavigateAsync("MainRegion", "current", IParameters.Create());
        Assert.True(first.Result);

        // 再导航到 target，缺少 Id。
        var second = await service.NavigateAsync("MainRegion", "target", IParameters.Create());

        Assert.False(second.Result);
        Assert.Equal(0, currentViewModel.NavigatedFromCount);
        Assert.True(region.ActiveViews.Contains(currentView));
    }

    /// <summary>
    /// 未声明必需参数时行为与之前完全一致。
    /// </summary>
    [Fact]
    public async Task NavigateAsync_NoRequiredParameters_Succeeds()
    {
        var view = CreateView("demo", new Dictionary<string, Type?>());
        var (service, _) = CreateService(view);

        var result = await service.NavigateAsync("MainRegion", "demo", IParameters.Create());

        Assert.True(result.Result);
    }

    /// <summary>
    /// 参数名大小写不敏感。
    /// </summary>
    [Fact]
    public async Task NavigateAsync_ParameterNameIsCaseInsensitive()
    {
        var view = CreateView("demo", new Dictionary<string, Type?>(StringComparer.OrdinalIgnoreCase) { ["Id"] = typeof(int) });
        var (service, _) = CreateService(view);

        var result = await service.NavigateAsync("MainRegion", "demo", IParameters.Create("?id=42"));

        Assert.True(result.Result);
    }

    /// <summary>
    /// 校验通过后，视图模型用不同大小写也能读到同一个参数。
    /// </summary>
    /// <remarks>
    /// 回归：校验器按不敏感匹配，而 <see cref="IParameters"/> 的读取曾是
    /// <see cref="StringComparison.Ordinal"/>。于是 <c>?Id=42</c> 配必需参数
    /// <c>["id:int"]</c> 校验通过，视图模型随后 <c>GetRequired&lt;int&gt;("id")</c>
    /// 却抛"参数不存在"。两处口径必须一致，"校验通过"才意味着"读得到"。
    /// </remarks>
    [Fact]
    public async Task NavigateAsync_ValidatedParameter_IsReadableWithAnyCase()
    {
        var view = CreateView("demo", new Dictionary<string, Type?> { ["id"] = typeof(int) });
        var (service, _) = CreateService(view);

        var result = await service.NavigateAsync("MainRegion", "demo", IParameters.Create("?Id=42"));

        Assert.True(result.Result);
        Assert.Equal(42, result.Context.Parameters.GetRequired<int>("id"));
        Assert.Equal(42, result.Context.Parameters.GetRequired<int>("ID"));
    }

    /// <summary>
    /// 可空值类型接受 null 与可解析的字符串。
    /// </summary>
    [Fact]
    public async Task NavigateAsync_NullableValueType_AcceptsNullAndParsable()
    {
        var view = CreateView("demo", new Dictionary<string, Type?> { ["Id"] = typeof(int?) });
        var (service, _) = CreateService(view);

        var result = await service.NavigateAsync("MainRegion", "demo", IParameters.Create("?Id=42"));

        Assert.True(result.Result);
    }

    /// <summary>
    /// 枚举参数接受成员名。
    /// </summary>
    [Fact]
    public async Task NavigateAsync_EnumParameter_AcceptsMemberName()
    {
        var view = CreateView("demo", new Dictionary<string, Type?> { ["Mode"] = typeof(SampleMode) });
        var (service, _) = CreateService(view);

        var result = await service.NavigateAsync("MainRegion", "demo", IParameters.Create("?Mode=Second"));

        Assert.True(result.Result);
    }

    private static FakeView CreateView(
        string viewName,
        IReadOnlyDictionary<string, Type?> required,
        object? viewModel = null
    ) => new(viewName, viewModel) { RequiredNavigationParameters = required };

    /// <summary>
    /// 契约在视图模型上时同样生效。
    /// </summary>
    /// <remarks>
    /// 生成器把契约成员生成到视图模型上（<see cref="INavigationContract"/>），
    /// 校验器必须优先读那里。视图侧保持默认空字典。
    /// </remarks>
    [Fact]
    public async Task NavigateAsync_ContractOnViewModel_IsValidated()
    {
        var viewModel = new ContractViewModel(new Dictionary<string, Type?> { ["Id"] = typeof(int) });
        var view = new FakeView("demo", viewModel);
        var (service, _) = CreateService(view);

        var missing = await service.NavigateAsync("MainRegion", "demo", IParameters.Create());
        Assert.False(missing.Result);
        Assert.Contains("缺少参数 Id", missing.Exception!.Message);

        var provided = await service.NavigateAsync("MainRegion", "demo", IParameters.Create("?Id=42"));
        Assert.True(provided.Result);
    }

    /// <summary>
    /// 视图模型不实现契约时回落到视图的声明。
    /// </summary>
    /// <remarks>
    /// 保留给手写 <see cref="IView"/> 实现：它们可能仍把契约放在视图上。
    /// </remarks>
    [Fact]
    public async Task NavigateAsync_ContractOnViewOnly_FallsBackToView()
    {
        var view = CreateView("demo", new Dictionary<string, Type?> { ["Id"] = typeof(int) }, new object());
        var (service, _) = CreateService(view);

        var result = await service.NavigateAsync("MainRegion", "demo", IParameters.Create());

        Assert.False(result.Result);
        Assert.Contains("缺少参数 Id", result.Exception!.Message);
    }

    /// <summary>
    /// 两处都声明时以视图模型为准。
    /// </summary>
    /// <remarks>
    /// 视图模型是参数的消费者，契约与消费者放在一起；视图侧的声明视为遗留。
    /// </remarks>
    [Fact]
    public async Task NavigateAsync_ContractOnBoth_PrefersViewModel()
    {
        var viewModel = new ContractViewModel(new Dictionary<string, Type?> { ["FromViewModel"] = null });
        var view = new FakeView("demo", viewModel)
        {
            RequiredNavigationParameters = new Dictionary<string, Type?> { ["FromView"] = null },
        };
        var (service, _) = CreateService(view);

        var result = await service.NavigateAsync("MainRegion", "demo", IParameters.Create());

        Assert.False(result.Result);
        Assert.Contains("缺少参数 FromViewModel", result.Exception!.Message);

        // 视图侧的声明被忽略：错误里不应出现 FromView 这个参数名。
        // 注意不能用 DoesNotContain("FromView")——它是 FromViewModel 的子串。
        Assert.DoesNotContain("缺少参数 FromView。", result.Exception.Message);
    }

    /// <summary>
    /// 实现 <see cref="INavigationContract"/> 的视图模型。
    /// </summary>
    private sealed class ContractViewModel(IReadOnlyDictionary<string, Type?> required) : INavigationContract
    {
        public IReadOnlyDictionary<string, Type?> RequiredNavigationParameters { get; } = required;
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
    /// 记录导航回调次数的视图模型。
    /// </summary>
    private sealed class CountingViewModel : INavigationAware
    {
        public int NavigatedFromCount { get; private set; }

        public Task<bool> IsNavigationTargetAsync(INavigationContext navigationContext) => Task.FromResult(true);

        public Task OnNavigatedFromAsync(INavigationContext navigationContext)
        {
            NavigatedFromCount++;
            return Task.CompletedTask;
        }

        public Task OnNavigatedToAsync(INavigationContext navigationContext) => Task.CompletedTask;
    }

    /// <summary>
    /// 测试用枚举。
    /// </summary>
    private enum SampleMode
    {
        First,
        Second,
    }
}
