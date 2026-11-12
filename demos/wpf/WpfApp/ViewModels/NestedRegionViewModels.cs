using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Len.Abp.Wpf;
using Len.Abp.Wpf.Navigation;
using Len.Abp.Wpf.Navigation.Regions;
using Volo.Abp.DependencyInjection;

namespace WpfApp.ViewModels;

/// <summary>
/// 嵌套区域演示的公共视图模型，记录实例编号、层级与进入次数。
/// </summary>
/// <remarks>
/// 所有层级都是瞬态且不保活：父视图离开区域时整棵子树会被递归拆除，
/// 再次进入时每一层都会得到新的实例编号，界面上的编号变化即可验证拆除确实发生。
/// </remarks>
/// <param name="navigationService">导航服务。</param>
/// <param name="depth">当前层级，从 1 开始。</param>
/// <param name="childRegionName">子区域名称；叶子层为 <see langword="null"/>。</param>
/// <param name="nextViewName">下一层视图名称；叶子层为 <see langword="null"/>。</param>
public abstract partial class NestedRegionViewModelBase(
    INavigationService navigationService,
    int depth,
    string? childRegionName,
    string? nextViewName
) : ObservableObject, ITransientDependency, IRegionMemberLifetime, INavigationAware
{
    private static int _counter;

    /// <summary>
    /// 获取视图模型的实例编号，进程内自增。
    /// </summary>
    public int InstanceId { get; } = Interlocked.Increment(ref _counter);
    /// <summary>
    /// 获取当前层级，从 1 开始。
    /// </summary>
    public int Depth { get; } = depth;

    /// <summary>
    /// 获取子区域名称；叶子层为 <see langword="null"/>。
    /// </summary>
    public string? ChildRegionName { get; } = childRegionName;

    /// <summary>
    /// 获取下一层视图名称；叶子层为 <see langword="null"/>。
    /// </summary>
    public string? NextViewName { get; } = nextViewName;

    /// <summary>
    /// 获取是否存在下一层。
    /// </summary>
    public bool HasChild => ChildRegionName is not null && NextViewName is not null;

    /// <summary>
    /// 获取或设置进入本层的次数。
    /// </summary>
    [ObservableProperty]
    public partial int NavigatedToCount { get; set; }

    /// <summary>
    /// 获取或设置实例存活统计文本。
    /// </summary>
    [ObservableProperty]
    public partial string InstanceStats { get; set; } = "点击「统计实例存活」查看";

    private bool _tracked;

    /// <inheritdoc/>
    public bool KeepAlive => false;

    /// <inheritdoc/>
    public Task<bool> IsNavigationTargetAsync(INavigationContext navigationContext) => Task.FromResult(true);

    /// <inheritdoc/>
    public Task OnNavigatedFromAsync(INavigationContext navigationContext) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task OnNavigatedToAsync(INavigationContext navigationContext)
    {
        NavigatedToCount++;

        if (!_tracked)
        {
            _tracked = true;
            NestedInstanceTracker.TrackViewModel(this);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 导航到下一层。
    /// </summary>
    [RelayCommand]
    private async Task EnterNextAsync()
    {
        if (ChildRegionName is null || NextViewName is null)
        {
            return;
        }

        await navigationService.NavigateAsync(ChildRegionName, NextViewName, IParameters.Create());
    }

    /// <summary>
    /// 把子区域切换到替代视图，用于验证只拆除被替换的那部分子树。
    /// </summary>
    /// <remarks>
    /// 例如在第 4 层点击时，NestedLevel4Region 里的第 5 层被替换，
    /// 第 5 到第 8 层应被拆除，而第 1 到第 4 层保持存活。
    /// </remarks>
    [RelayCommand]
    private async Task SwitchChildAsync()
    {
        if (ChildRegionName is null)
        {
            return;
        }

        await navigationService.NavigateAsync(ChildRegionName, NestedAlternateViewModel.ViewName, IParameters.Create());
    }

    /// <summary>
    /// 退出到画廊，销毁整棵嵌套子树。
    /// </summary>
    [RelayCommand]
    private async Task ExitAsync() =>
        await navigationService.NavigateAsync("MainRegion", "gallery", IParameters.Create());

    /// <summary>
    /// 触发回收并统计视图与视图模型的存活数量。
    /// </summary>
    [RelayCommand]
    private void CollectStats()
    {
        var (viewsCreated, viewsAlive) = NestedInstanceTracker.CollectViews();
        var (viewModelsCreated, viewModelsAlive) = NestedInstanceTracker.CollectViewModels();

        InstanceStats =
            $"视图 {viewsAlive}/{viewsCreated} 存活，视图模型 {viewModelsAlive}/{viewModelsCreated} 存活";
    }
}

/// <summary>
/// 第 1 层视图模型，位于主区域中，内部拥有 <c>NestedLevel1Region</c>。
/// </summary>
public class NestedRootViewModel(INavigationService navigationService)
    : NestedRegionViewModelBase(navigationService, 1, "NestedLevel1Region", "nested-level1");

/// <summary>
/// 第 2 层视图模型，内部拥有 <c>NestedLevel2Region</c>。
/// </summary>
public class NestedLevel1ViewModel(INavigationService navigationService)
    : NestedRegionViewModelBase(navigationService, 2, "NestedLevel2Region", "nested-level2");

/// <summary>
/// 第 3 层视图模型，内部拥有 <c>NestedLevel3Region</c>。
/// </summary>
public class NestedLevel2ViewModel(INavigationService navigationService)
    : NestedRegionViewModelBase(navigationService, 3, "NestedLevel3Region", "nested-level3");

/// <summary>
/// 第 4 层视图模型，内部拥有 <c>NestedLevel4Region</c>。
/// </summary>
public class NestedLevel3ViewModel(INavigationService navigationService)
    : NestedRegionViewModelBase(navigationService, 4, "NestedLevel4Region", "nested-level4");

/// <summary>
/// 第 5 层视图模型，内部拥有 <c>NestedLevel5Region</c>。
/// </summary>
public class NestedLevel4ViewModel(INavigationService navigationService)
    : NestedRegionViewModelBase(navigationService, 5, "NestedLevel5Region", "nested-level5");

/// <summary>
/// 第 6 层视图模型，内部拥有 <c>NestedLevel6Region</c>。
/// </summary>
public class NestedLevel5ViewModel(INavigationService navigationService)
    : NestedRegionViewModelBase(navigationService, 6, "NestedLevel6Region", "nested-level6");

/// <summary>
/// 第 7 层视图模型，内部拥有 <c>NestedLevel7Region</c>。
/// </summary>
public class NestedLevel6ViewModel(INavigationService navigationService)
    : NestedRegionViewModelBase(navigationService, 7, "NestedLevel7Region", "nested-level7");

/// <summary>
/// 第 8 层视图模型，叶子层，不再包含子区域。
/// </summary>
public class NestedLeafViewModel(INavigationService navigationService)
    : NestedRegionViewModelBase(navigationService, 8, null, null);

/// <summary>
/// 替代视图的视图模型，用于替换某一层的子区域内容。
/// </summary>
/// <remarks>
/// 它没有子区域，所以替换后该层以下的整棵子树都会被拆除。
/// </remarks>
public class NestedAlternateViewModel(INavigationService navigationService)
    : NestedRegionViewModelBase(navigationService, 0, null, null)
{
    /// <summary>
    /// 获取替代视图的视图名称。
    /// </summary>
    public const string ViewName = "nested-alternate";
}
