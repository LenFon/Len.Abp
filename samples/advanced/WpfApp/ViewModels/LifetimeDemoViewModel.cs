using CommunityToolkit.Mvvm.ComponentModel;
using Len.Abp.Wpf;
using Len.Abp.Wpf.Navigation;
using Len.Abp.Wpf.Navigation.Regions;
using System.Collections.ObjectModel;

namespace WpfApp.ViewModels;

/// <summary>
/// 生命周期演示视图模型的公共部分，记录实例编号与导航回调次数。
/// </summary>
/// <remarks>
/// <para>
/// 各派生视图模型分别被不同生命周期的视图使用，实例编号在进程内自增，
/// 因此界面上的编号是否变化就能直接反映视图的注册生命周期。
/// </para>
/// <para>
/// 基类刻意**不声明** ABP 生命周期标记接口：派生类声明为 <c>partial</c> 后，
/// 生成器会按 <c>[View]</c> 上的 <c>Lifetime</c> / <c>ViewModelLifetime</c> 追加对应接口。
/// 若基类已经实现 <c>ITransientDependency</c>，生成器会认为使用者已自行注册而跳过，
/// 单例与作用域的演示就失效了。
/// </para>
/// </remarks>
public abstract partial class LifetimeDemoViewModelBase
    : ObservableObject,
        INavigationAware,
        IRegionMemberLifetime
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
public partial class LifetimeSingletonViewModel : LifetimeDemoViewModelBase;

/// <summary>
/// 瞬态视图的视图模型。
/// </summary>
public partial class LifetimeTransientViewModel : LifetimeDemoViewModelBase;

/// <summary>
/// 作用域视图的视图模型。
/// </summary>
public partial class LifetimeScopedViewModel : LifetimeDemoViewModelBase;

/// <summary>
/// 视图瞬态、视图模型单例的演示视图模型。
/// </summary>
/// <remarks>
/// 生命周期接口由生成器按 <c>ViewModelLifetime = Singleton</c> 追加为
/// <c>ISingletonDependency</c>，因此这里不手写标记接口。
/// </remarks>
public partial class LifetimeSharedStateViewModel : LifetimeDemoViewModelBase;
