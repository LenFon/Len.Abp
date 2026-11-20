using CommunityToolkit.Mvvm.ComponentModel;
using Len.Abp.Wpf.Navigation;
using Volo.Abp.DependencyInjection;

namespace WpfApp.ViewModels;

/// <summary>
/// 详情页的视图模型，从导航参数读取必需参数。
/// </summary>
/// <remarks>
/// 用 <c>GetRequired&lt;T&gt;</c> 而不是 <c>GetValue&lt;T&gt;</c>：
/// 前者在缺失或类型不匹配时立即失败，避免"键名拼错"被当成"值为默认值"。
/// 导航服务已经在激活之前校验过一遍，这里再读一次是视图模型自己的输入契约。
/// </remarks>
public partial class DetailViewModel : ObservableObject, ITransientDependency, INavigationAware
{
    /// <summary>
    /// 获取或设置详情编号。
    /// </summary>
    [ObservableProperty]
    public partial int Id { get; set; }

    /// <summary>
    /// 获取或设置来源标识。
    /// </summary>
    [ObservableProperty]
    public partial string Source { get; set; } = "（无）";

    /// <summary>
    /// 获取或设置进入本视图的次数。
    /// </summary>
    [ObservableProperty]
    public partial int NavigatedToCount { get; set; }

    /// <inheritdoc/>
    public Task<bool> IsNavigationTargetAsync(INavigationContext navigationContext) => Task.FromResult(true);

    /// <inheritdoc/>
    public Task OnNavigatedFromAsync(INavigationContext navigationContext) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task OnNavigatedToAsync(INavigationContext navigationContext)
    {
        NavigatedToCount++;
        Id = navigationContext.Parameters.GetRequired<int>("id");
        Source = navigationContext.Parameters.GetRequired<string>("source");

        return Task.CompletedTask;
    }
}
