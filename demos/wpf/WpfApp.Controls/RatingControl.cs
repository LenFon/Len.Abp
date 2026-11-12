using Len.Abp.Wpf;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace WpfApp.Controls;

/// <summary>
/// 评分控件，用于演示依赖属性生成器与路由事件生成器的全部能力。
/// </summary>
/// <remarks>
/// 这是真正的自定义控件：派生自 <see cref="Control"/>，外观由
/// <c>Themes/Generic.xaml</c> 中的 <see cref="ControlTemplate"/> 提供，
/// 使用方可以完全替换模板而不受控件内部结构约束。
/// <para>
/// 依赖属性部分覆盖：泛型与非泛型特性、默认值与默认值表达式、分类与描述、
/// 元数据标志、更新源触发器、只读属性，以及强制值、校验、变更三个分部钩子。
/// 路由事件部分覆盖：冒泡、隧道、直接三种路由策略与自定义事件处理程序委托。
/// </para>
/// </remarks>
[DependencyProperty<int>(
    "Rating",
    DefaultValue = 0,
    Category = "行为",
    Description = "当前评分，校验允许 0 到 100，强制值再收敛到 0 到 5。",
    Flags = FrameworkPropertyMetadataOptions.BindsTwoWayByDefault)]
[DependencyProperty<string>("Caption", DefaultValue = "评分", Category = "外观", Description = "评分控件左侧的标题。")]
[DependencyProperty<Thickness>(
    "StarMargin",
    DefaultValueExpression = "new global::System.Windows.Thickness(2)",
    Category = "外观",
    Description = "每颗星的外边距，默认值由表达式提供。",
    Flags = FrameworkPropertyMetadataOptions.AffectsMeasure)]
[DependencyProperty<string>(
    "DisplayText",
    DefaultValue = "未评分",
    IsReadOnly = true,
    Category = "只读",
    Description = "由 Rating 计算出的只读文本，外部无法直接赋值。")]
[RoutedEvent<RatingChangedEventHandler>(
    "RatingChanged",
    RoutingStrategy.Bubble,
    Category = "行为",
    Description = "评分变化后沿冒泡路由向上传递。")]
// 非泛型重载通过第三个参数显式指定委托类型，效果与泛型重载一致。
[RoutedEvent(
    "PreviewRatingChanged",
    RoutingStrategy.Tunnel,
    typeof(RatingChangedEventHandler),
    Category = "行为",
    Description = "评分变化时沿隧道路由从根向下传递。")]
[RoutedEvent(
    "RatingCommitted",
    RoutingStrategy.Direct,
    typeof(RatingChangedEventHandler),
    Category = "行为",
    Description = "只在控件自身触发，不参与路由。")]
public partial class RatingControl : Control
{
    private const int MaxStars = 5;

    static RatingControl()
    {
        // 星标按钮由模板生成，无法在代码里逐个订阅 Click，改用类处理器统一接收。
        EventManager.RegisterClassHandler(typeof(RatingControl), ButtonBase.ClickEvent, new RoutedEventHandler(OnStarClick));
    }

    /// <summary>
    /// 初始化评分控件。
    /// </summary>
    public RatingControl()
    {
        Stars = [.. Enumerable.Range(1, MaxStars).Select(value => new RatingStar(value))];
        UpdateStars();
    }

    /// <summary>
    /// 获取模板用来渲染星标的数据项集合。
    /// </summary>
    public IReadOnlyList<RatingStar> Stars { get; }

    /// <summary>
    /// 校验钩子：拒绝负数与超过 100 的评分。
    /// </summary>
    /// <remarks>
    /// 校验在强制值之前执行，返回 false 时 WPF 会抛出 <see cref="ArgumentException"/>，
    /// 因此这里只拦截明显非法的值，把“超过上限”交给强制值处理。
    /// </remarks>
    static partial void IsRatingValid(int value, ref bool result) => result = value is >= 0 and <= 100;

    /// <summary>
    /// 强制值钩子：把评分收敛到 0 到 5。
    /// </summary>
    /// <remarks>
    /// 生成的回调会先给 <c>result</c> 赋默认值再调用本方法，所以这里必须显式写入结果。
    /// </remarks>
    static partial void CoerceRating(int value, ref int result) => result = Math.Clamp(value, 0, MaxStars);

    /// <summary>
    /// 变更钩子：同步只读文本、刷新星标，并依次触发三种路由事件。
    /// </summary>
    partial void OnRatingChanged(int oldValue, int newValue)
    {
        DisplayText = newValue == 0 ? "未评分" : $"{newValue} / {MaxStars}";
        UpdateStars();

        // 同一来源依次触发隧道、冒泡、直接事件，便于在界面上观察路由顺序。
        RaisePreviewRatingChangedEvent(new RatingChangedEventArgs(newValue));
        RaiseRatingChangedEvent(new RatingChangedEventArgs(newValue));
        RaiseRatingCommittedEvent(new RatingChangedEventArgs(newValue));
    }

    private static void OnStarClick(object sender, RoutedEventArgs e)
    {
        if (sender is not RatingControl control)
        {
            return;
        }

        // 类处理器收到的是冒泡到控件的事件，e.Source 可能已被模板内部改写，
        // 因此从 OriginalSource 沿可视树向上找承载 RatingStar 的按钮。
        if (FindStarButton(e.OriginalSource as DependencyObject) is { DataContext: RatingStar star })
        {
            control.Rating = star.Value;
        }
    }

    private static Button? FindStarButton(DependencyObject? node)
    {
        while (node is not null and not RatingControl)
        {
            if (node is Button { DataContext: RatingStar })
            {
                return (Button)node;
            }

            node = System.Windows.Media.VisualTreeHelper.GetParent(node);
        }

        return null;
    }

    private void UpdateStars()
    {
        foreach (var star in Stars)
        {
            star.IsFilled = star.Value <= Rating;
        }
    }
}
