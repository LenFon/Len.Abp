using Len.Abp.Wpf.Mvvm;
using System.Windows;
using System.Windows.Controls;
using WpfApp.Controls;
using WpfApp.ViewModels;

namespace WpfApp.Views;

/// <summary>
/// 控件画廊视图，把自定义控件产生的路由事件转发到视图模型的日志。
/// </summary>
[View<ControlsGalleryViewModel>(ViewName = "gallery")]
public partial class ControlsGalleryView : UserControl
{
    /// <summary>
    /// 初始化视图。
    /// </summary>
    public ControlsGalleryView()
    {
        InitializeComponent();

        // 附加路由事件只提供 Add/Remove/Raise 静态方法，事件源需要自行接线。
        InteractionAssist.EnableDoubleClick(DoubleClickArea);
    }

    private void OnPreviewRatingChanged(object sender, RatingChangedEventArgs e) =>
        ViewModel.Log($"PreviewRatingChanged（隧道） 评分 = {e.Rating}");

    private void OnRatingChanged(object sender, RatingChangedEventArgs e) =>
        ViewModel.Log($"RatingChanged（冒泡） 评分 = {e.Rating}");

    private void OnRatingCommitted(object sender, RatingChangedEventArgs e) =>
        ViewModel.Log($"RatingCommitted（直接） 评分 = {e.Rating}");

    private void OnTagAdded(object sender, TagEventArgs e) => ViewModel.Log($"TagAdded 标签 = {e.Tag}");

    private void OnTagRejected(object sender, TagEventArgs e) =>
        ViewModel.Log($"TagRejected 标签 = {e.Tag}（超长或重复）");

    private void OnDoubleClick(object sender, DoubleClickEventArgs e)
    {
        ViewModel.ReportDoubleClick(e.Position);
        ViewModel.Log($"DoubleClick 位置 = ({e.Position.X:F0}, {e.Position.Y:F0})");
    }
}
