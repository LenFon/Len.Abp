using Len.Abp.Wpf.Mvvm;
using Len.Abp.Wpf.Navigation;
using System.Windows.Controls;
using WpfApp.ViewModels;

namespace WpfApp.Views;

/// <summary>
/// 嵌套区域演示视图的公共外观，八层视图共用同一份 XAML。
/// </summary>
/// <remarks>
/// 派生视图只负责声明 <c>[View&lt;T&gt;]</c> 与自己的子区域名称，
/// 界面内容由本类提供，避免为八层复制八份相同的 XAML。
/// 子区域名称通过附加属性在构造函数里设置，与 XAML 中静态声明的效果一致。
/// </remarks>
public partial class NestedRegionView : UserControl
{
    /// <summary>
    /// 初始化视图。
    /// </summary>
    public NestedRegionView()
    {
        InitializeComponent();

        // 记录实例，供「统计实例存活」按钮验证离开区域后是否被回收。
        NestedInstanceTracker.TrackView(this);
    }

    /// <summary>
    /// 设置标题与子区域名称。
    /// </summary>
    /// <param name="title">视图标题。</param>
    /// <param name="childRegionName">子区域名称；叶子层传 <see langword="null"/>。</param>
    protected void Configure(string title, string? childRegionName)
    {
        TitleText.Text = title;

        if (childRegionName is not null)
        {
            // 设置附加属性会触发区域构建，区域在控件加载完成后创建。
            Region.SetName(ChildRegionHost, childRegionName);
        }
    }
}

/// <summary>
/// 第 1 层视图，位于主区域中，内部声明 <c>NestedLevel1Region</c>。
/// </summary>
[View<NestedRootViewModel>(ViewName = "nested-root")]
public partial class NestedRootView : NestedRegionView
{
    /// <summary>
    /// 初始化视图。
    /// </summary>
    public NestedRootView() =>
        Configure("嵌套区域 · 第 1 层", "NestedLevel1Region");
}

/// <summary>
/// 第 2 层视图，内部声明 <c>NestedLevel2Region</c>。
/// </summary>
[View<NestedLevel1ViewModel>(ViewName = "nested-level1")]
public partial class NestedLevel1View : NestedRegionView
{
    /// <summary>
    /// 初始化视图。
    /// </summary>
    public NestedLevel1View() =>
        Configure("嵌套区域 · 第 2 层", "NestedLevel2Region");
}

/// <summary>
/// 第 3 层视图，内部声明 <c>NestedLevel3Region</c>。
/// </summary>
[View<NestedLevel2ViewModel>(ViewName = "nested-level2")]
public partial class NestedLevel2View : NestedRegionView
{
    /// <summary>
    /// 初始化视图。
    /// </summary>
    public NestedLevel2View() =>
        Configure("嵌套区域 · 第 3 层", "NestedLevel3Region");
}

/// <summary>
/// 第 4 层视图，内部声明 <c>NestedLevel4Region</c>。
/// </summary>
[View<NestedLevel3ViewModel>(ViewName = "nested-level3")]
public partial class NestedLevel3View : NestedRegionView
{
    /// <summary>
    /// 初始化视图。
    /// </summary>
    public NestedLevel3View() =>
        Configure("嵌套区域 · 第 4 层", "NestedLevel4Region");
}

/// <summary>
/// 第 5 层视图，内部声明 <c>NestedLevel5Region</c>。
/// </summary>
[View<NestedLevel4ViewModel>(ViewName = "nested-level4")]
public partial class NestedLevel4View : NestedRegionView
{
    /// <summary>
    /// 初始化视图。
    /// </summary>
    public NestedLevel4View() =>
        Configure("嵌套区域 · 第 5 层", "NestedLevel5Region");
}

/// <summary>
/// 第 6 层视图，内部声明 <c>NestedLevel6Region</c>。
/// </summary>
[View<NestedLevel5ViewModel>(ViewName = "nested-level5")]
public partial class NestedLevel5View : NestedRegionView
{
    /// <summary>
    /// 初始化视图。
    /// </summary>
    public NestedLevel5View() =>
        Configure("嵌套区域 · 第 6 层", "NestedLevel6Region");
}

/// <summary>
/// 第 7 层视图，内部声明 <c>NestedLevel7Region</c>。
/// </summary>
[View<NestedLevel6ViewModel>(ViewName = "nested-level6")]
public partial class NestedLevel6View : NestedRegionView
{
    /// <summary>
    /// 初始化视图。
    /// </summary>
    public NestedLevel6View() =>
        Configure("嵌套区域 · 第 7 层", "NestedLevel7Region");
}

/// <summary>
/// 第 8 层视图，叶子层，不再包含子区域。
/// </summary>
[View<NestedLeafViewModel>(ViewName = "nested-level7")]
public partial class NestedLeafView : NestedRegionView
{
    /// <summary>
    /// 初始化视图。
    /// </summary>
    public NestedLeafView() =>
        Configure("嵌套区域 · 第 8 层", null);
}
