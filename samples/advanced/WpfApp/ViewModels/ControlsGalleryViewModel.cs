using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Len.Abp.Wpf.Navigation.Regions;
using System.Collections.ObjectModel;
using System.Windows;
using Volo.Abp.DependencyInjection;

namespace WpfApp.ViewModels;

/// <summary>
/// 控件画廊的视图模型，汇总三个自定义控件产生的状态与事件日志。
/// </summary>
/// <remarks>
/// 显式声明保活：画廊是主界面，事件日志与控件状态需要在来回导航后保留。
/// 不声明契约时默认不保活，每次导航都会重建并清空日志。
/// </remarks>
public partial class ControlsGalleryViewModel : ObservableObject, ITransientDependency, IRegionMemberLifetime
{
    /// <inheritdoc/>
    public bool KeepAlive => true;

    /// <summary>
    /// 获取事件日志。界面上的路由事件由视图转发到这里。
    /// </summary>
    public ObservableCollection<string> Logs { get; } = [];

    /// <summary>
    /// 获取或设置评分控件的评分，双向绑定到生成的依赖属性。
    /// </summary>
    [ObservableProperty]
    public partial int Rating { get; set; }

    /// <summary>
    /// 获取或设置评分控件的标题。
    /// </summary>
    [ObservableProperty]
    public partial string RatingCaption { get; set; } = "服务评分";

    /// <summary>
    /// 获取或设置标签输入框的占位文本。
    /// </summary>
    [ObservableProperty]
    public partial string TagPlaceholder { get; set; } = "输入标签后按回车";

    /// <summary>
    /// 获取或设置是否允许重复标签。
    /// </summary>
    [ObservableProperty]
    public partial bool AllowDuplicateTags { get; set; }

    /// <summary>
    /// 获取或设置单个标签的最大长度。
    /// </summary>
    [ObservableProperty]
    public partial int MaxTagLength { get; set; } = 12;

    /// <summary>
    /// 获取或设置演示元素是否处于忙碌状态。
    /// </summary>
    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    /// <summary>
    /// 获取或设置忙碌时的透明度。
    /// </summary>
    [ObservableProperty]
    public partial double BusyOpacity { get; set; } = 0.4;

    /// <summary>
    /// 获取或设置忙碌提示文本。
    /// </summary>
    [ObservableProperty]
    public partial string BusyText { get; set; } = "处理中…";

    /// <summary>
    /// 获取双击演示区域的提示文本。
    /// </summary>
    [ObservableProperty]
    public partial string DoubleClickHint { get; set; } = "在下方区域双击试试";

    /// <summary>
    /// 记录一条事件日志。
    /// </summary>
    /// <param name="message">日志内容。</param>
    public void Log(string message)
    {
        Logs.Insert(0, $"{DateTime.Now:HH:mm:ss}  {message}");

        // 日志只用于演示，保留最近 50 条即可，避免长时间运行后无限增长。
        while (Logs.Count > 50)
        {
            Logs.RemoveAt(Logs.Count - 1);
        }
    }

    /// <summary>
    /// 清空日志。
    /// </summary>
    [RelayCommand]
    private void ClearLogs() => Logs.Clear();

    /// <summary>
    /// 把评分重置为 0。
    /// </summary>
    [RelayCommand]
    private void ResetRating() => Rating = 0;

    /// <summary>
    /// 切换忙碌状态。
    /// </summary>
    [RelayCommand]
    private void ToggleBusy() => IsBusy = !IsBusy;

    /// <summary>
    /// 记录双击位置。
    /// </summary>
    /// <param name="position">鼠标相对元素的位置。</param>
    public void ReportDoubleClick(Point position) =>
        DoubleClickHint = $"已双击：({position.X:F0}, {position.Y:F0})";
}
