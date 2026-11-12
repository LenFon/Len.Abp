using CommunityToolkit.Mvvm.ComponentModel;
using Len.Abp.Wpf;
using Len.Abp.Wpf.Navigation;
using Len.Abp.Wpf.Navigation.Regions;
using System.Collections.ObjectModel;
using Volo.Abp.DependencyInjection;

namespace WpfApp.ViewModels;

/// <summary>
/// 生命周期演示视图模型的公共部分，记录实例编号与导航回调次数。
/// </summary>
/// <remarks>
/// 两个派生视图模型分别被单例与瞬态视图使用，实例编号在进程内自增，
/// 因此界面上的编号是否变化就能直接反映视图的注册生命周期。
/// </remarks>
public abstract partial class LifetimeDemoViewModelBase
    : ObservableObject,
        INavigationAware,
        IRegionMemberLifetime,
        ITransientDependency
{
    private static int _counter;

    /// <summary>
    /// 获取当前视图模型的实例编号。
    /// </summary>
    public int InstanceId { get; } = Interlocked.Increment(ref _counter);

    /// <summary>
    /// 获取视图模型的创建时间。
    /// </summary>
    public DateTime CreatedAt { get; } = DateTime.Now;

    /// <summary>
    /// 获取导航事件记录。
    /// </summary>
    public ObservableCollection<string> Events { get; } = [];

    /// <summary>
    /// 获取或设置本次导航收到的参数文本。
    /// </summary>
    [ObservableProperty]
    public partial string NavigationParameters { get; set; } = "（无）";

    /// <summary>
    /// 获取或设置进入本视图的次数。
    /// </summary>
    [ObservableProperty]
    public partial int NavigatedToCount { get; set; }

    /// <inheritdoc/>
    public bool KeepAlive => false;

    /// <inheritdoc/>
    public Task<bool> IsNavigationTargetAsync(INavigationContext navigationContext) => Task.FromResult(true);

    /// <inheritdoc/>
    public Task OnNavigatedFromAsync(INavigationContext navigationContext)
    {
        Events.Insert(0, $"{DateTime.Now:HH:mm:ss}  OnNavigatedFrom");
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task OnNavigatedToAsync(INavigationContext navigationContext)
    {
        NavigatedToCount++;
        Events.Insert(0, $"{DateTime.Now:HH:mm:ss}  OnNavigatedTo（第 {NavigatedToCount} 次）");

        NavigationParameters = navigationContext.Parameters.Count == 0
            ? "（无）"
            : string.Join(", ", navigationContext.Parameters.Select(pair => $"{pair.Key}={pair.Value}"));

        return Task.CompletedTask;
    }
}

/// <summary>
/// 单例视图的视图模型。
/// </summary>
public class LifetimeSingletonViewModel : LifetimeDemoViewModelBase;

/// <summary>
/// 瞬态视图的视图模型。
/// </summary>
public class LifetimeTransientViewModel : LifetimeDemoViewModelBase;

/// <summary>
/// 作用域视图的视图模型。
/// </summary>
public class LifetimeScopedViewModel : LifetimeDemoViewModelBase;
