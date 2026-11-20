using CommunityToolkit.Mvvm.ComponentModel;

namespace WpfApp.ViewModels;

/// <summary>
/// 区域页面视图模型的公共部分，只记录实例编号。
/// </summary>
/// <remarks>
/// 实例编号在进程内自增，界面上的编号是否变化即可反映视图的注册生命周期。
/// 基类不声明生命周期标记接口：派生类声明为 <c>partial</c> 后由生成器追加，
/// 否则生成器会认为使用者已自行注册而跳过。
/// </remarks>
public abstract partial class RegionPageViewModelBase : ObservableObject
{
    private static int _counter;

    /// <summary>
    /// 获取当前视图模型的实例编号。
    /// </summary>
    public int InstanceId { get; } = Interlocked.Increment(ref _counter);
}

/// <summary>
/// 选项卡区域页面的视图模型。
/// </summary>
public partial class RegionTabPageViewModel : RegionPageViewModelBase;

/// <summary>
/// 列表区域页面的视图模型。
/// </summary>
public partial class RegionListPageViewModel : RegionPageViewModelBase;
