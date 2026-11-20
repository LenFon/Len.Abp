using Len.Abp.Wpf.Mvvm;
using Len.Abp.Wpf.Navigation;
using System.Windows.Controls;
using WpfApp.ViewModels;

namespace WpfApp.Views;

/// <summary>
/// 区域适配器演示页的公共外观，被 <c>TabControl</c>、<c>ItemsControl</c> 与自定义
/// <c>ListBox</c> 三个区域承载。
/// </summary>
/// <remarks>
/// 派生视图只声明 <c>[View&lt;T&gt;]</c> 与文案；选项卡标题由
/// <c>RegionHost.TabHeader</c> 附加属性提供，未设置时回退到视图名。
/// </remarks>
public partial class RegionPageView : UserControl
{
    public RegionPageView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 设置页面文案与选项卡标题。
    /// </summary>
    /// <param name="title">标题。</param>
    /// <param name="body">正文说明。</param>
    /// <param name="tabHeader">选项卡标题；为 <see langword="null"/> 时不设置，回退到视图名。</param>
    protected void Configure(string title, string body, string? tabHeader = null)
    {
        TitleText.Text = title;
        BodyText.Text = body;

        if (tabHeader is not null)
        {
            // 附加属性设在视图自身，TabControl 适配器创建选项卡时读取它作为 Header。
            RegionHost.SetTabHeader(this, tabHeader);
        }
    }
}

/// <summary>
/// 选项卡区域的第一个页面。
/// </summary>
[View<RegionTabPageViewModel>(ViewName = "tab-page-1")]
public partial class TabPage1View : RegionPageView
{
    public TabPage1View() =>
        Configure(
            "选项卡一",
            "TabControl 区域是单活动视图：切换选项卡会激活对应视图，旧视图按保活规则处理。",
            "选项卡一"
        );
}

/// <summary>
/// 选项卡区域的第二个页面。
/// </summary>
[View<RegionTabPageViewModel>(ViewName = "tab-page-2")]
public partial class TabPage2View : RegionPageView
{
    public TabPage2View() =>
        Configure(
            "选项卡二",
            "标题来自视图上的 RegionHost.TabHeader；未设置时显示视图名。",
            "选项卡二"
        );
}

/// <summary>
/// 选项卡区域的第三个页面，刻意不设置 <c>TabHeader</c>，用于对比回退行为。
/// </summary>
[View<RegionTabPageViewModel>(ViewName = "tab-page-3")]
public partial class TabPage3View : RegionPageView
{
    public TabPage3View() =>
        Configure("选项卡三", "本页没有设置 TabHeader，因此选项卡标题回退为视图名 tab-page-3。");
}

/// <summary>
/// 列表区域的第一个页面。
/// </summary>
[View<RegionListPageViewModel>(ViewName = "list-page-1")]
public partial class ListPage1View : RegionPageView
{
    public ListPage1View() =>
        Configure("列表项一", "ItemsControl 区域是多活动视图：所有视图同时显示，与激活状态无关。");
}

/// <summary>
/// 列表区域的第二个页面。
/// </summary>
[View<RegionListPageViewModel>(ViewName = "list-page-2")]
public partial class ListPage2View : RegionPageView
{
    public ListPage2View() =>
        Configure("列表项二", "多活动区域不会被导航自动失活，导航过的视图会一直留在区域中。");
}
