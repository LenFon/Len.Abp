using CommunityToolkit.Mvvm.ComponentModel;
using Len.Abp.Wpf.Navigation;
using Volo.Abp.DependencyInjection;

namespace MinimalApp.ViewModels;

/// <summary>
/// Hello 视图的视图模型。
/// </summary>
public partial class HelloViewModel : ObservableObject, INavigationAware, ITransientDependency
{
    /// <summary>
    /// 获取或设置显示的消息。
    /// </summary>
    [ObservableProperty]
    public partial string Message { get; set; } = "Hello, Len.Abp!";

    /// <inheritdoc/>
    public Task<bool> IsNavigationTargetAsync(INavigationContext navigationContext) => Task.FromResult(true);

    /// <inheritdoc/>
    public Task OnNavigatedFromAsync(INavigationContext navigationContext) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task OnNavigatedToAsync(INavigationContext navigationContext) => Task.CompletedTask;
}

/// <summary>
/// Goodbye 视图的视图模型。
/// </summary>
public partial class GoodbyeViewModel : ObservableObject, INavigationAware, ITransientDependency
{
    /// <summary>
    /// 获取或设置显示的消息。
    /// </summary>
    [ObservableProperty]
    public partial string Message { get; set; } = "Goodbye!";

    /// <inheritdoc/>
    public Task<bool> IsNavigationTargetAsync(INavigationContext navigationContext) => Task.FromResult(true);

    /// <inheritdoc/>
    public Task OnNavigatedFromAsync(INavigationContext navigationContext) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task OnNavigatedToAsync(INavigationContext navigationContext) => Task.CompletedTask;
}

/// <summary>
/// 问候视图的视图模型，从导航参数读取必需参数。
/// </summary>
/// <remarks>
/// 用 <c>GetRequired&lt;T&gt;</c> 而不是 <c>GetValue&lt;T&gt;</c>：
/// 前者在缺失时立即失败，避免"键名拼错"被当成"值为默认值"。
/// </remarks>
public partial class GreetingViewModel : ObservableObject, INavigationAware, ITransientDependency
{
    /// <summary>
    /// 获取或设置显示的消息。
    /// </summary>
    [ObservableProperty]
    public partial string Message { get; set; } = "Hello!";

    /// <inheritdoc/>
    public Task<bool> IsNavigationTargetAsync(INavigationContext navigationContext) => Task.FromResult(true);

    /// <inheritdoc/>
    public Task OnNavigatedFromAsync(INavigationContext navigationContext) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task OnNavigatedToAsync(INavigationContext navigationContext)
    {
        Message = $"Hello, {navigationContext.Parameters.GetRequired<string>("name")}!";
        return Task.CompletedTask;
    }
}
