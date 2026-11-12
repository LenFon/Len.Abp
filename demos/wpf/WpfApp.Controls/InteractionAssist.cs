using Len.Abp.Wpf;
using System.Windows;
using System.Windows.Input;

namespace WpfApp.Controls;

/// <summary>
/// <see cref="InteractionAssist.DoubleClick"/> 事件的处理程序委托。
/// </summary>
public delegate void DoubleClickEventHandler(object sender, DoubleClickEventArgs e);

/// <summary>
/// 双击事件参数。
/// </summary>
public class DoubleClickEventArgs : RoutedEventArgs
{
    /// <summary>
    /// 初始化事件参数。
    /// </summary>
    public DoubleClickEventArgs() { }

    /// <summary>
    /// 使用鼠标位置初始化事件参数。
    /// </summary>
    /// <param name="position">双击时鼠标相对元素的位置。</param>
    public DoubleClickEventArgs(Point position) => Position = position;

    /// <summary>
    /// 获取双击时鼠标相对元素的位置。
    /// </summary>
    public Point Position { get; }
}

/// <summary>
/// 通用交互附加属性与附加路由事件，用于演示生成器在“非控件类”上的用法。
/// </summary>
/// <remarks>
/// 这里刻意把附加属性与附加路由事件放在同一个类中，说明生成器不要求宿主是控件。
/// 但 WPF 要求附加属性的 <c>ownerType</c> 派生自 <see cref="DependencyObject"/>，
/// 因此宿主是普通类而不是静态类；本类不实例化，成员全部是静态的。
/// </remarks>
[AttachedDependencyProperty<bool>(
    "IsBusy",
    DefaultValue = false,
    Category = "状态",
    Description = "标记元素处于忙碌状态，变化时影响渲染。",
    Flags = FrameworkPropertyMetadataOptions.AffectsRender)]
[AttachedDependencyProperty<double>(
    "BusyOpacity",
    DefaultValue = 0.4,
    Category = "状态",
    Description = "忙碌时的透明度，校验允许 0 到 1，强制值再收敛到该区间。")]
[AttachedDependencyProperty<string>(
    "BusyText",
    DefaultValue = "处理中…",
    Category = "状态",
    Description = "忙碌时显示的提示文本。")]
[AttachedRoutedEvent<DoubleClickEventHandler>(
    "DoubleClick",
    RoutingStrategy.Bubble,
    Category = "输入",
    Description = "元素被双击时沿冒泡路由向上传递，携带鼠标位置。")]
public partial class InteractionAssist : DependencyObject
{
    /// <summary>
    /// 校验钩子：拒绝小于 0 或大于 1 的透明度。
    /// </summary>
    static partial void IsBusyOpacityValid(double value, ref bool result) => result = value is >= 0 and <= 1;

    /// <summary>
    /// 强制值钩子：把透明度收敛到 0 到 1。
    /// </summary>
    static partial void CoerceBusyOpacity(DependencyObject element, double value, ref double result) =>
        result = Math.Clamp(value, 0, 1);

    /// <summary>
    /// 忙碌状态变化时切换元素透明度，演示附加属性的变更回调。
    /// </summary>
    static partial void OnIsBusyChanged(DependencyObject element, bool oldValue, bool newValue)
    {
        if (element is UIElement uiElement)
        {
            uiElement.Opacity = newValue ? GetBusyOpacity(element) : 1;
        }
    }

    /// <summary>
    /// 为元素挂上双击检测。
    /// </summary>
    /// <param name="element">目标元素。</param>
    /// <remarks>
    /// 附加路由事件只提供 Add/Remove/Raise 三个静态方法，事件源需要自行接线。
    /// 这里用 <see cref="UIElement.MouseLeftButtonDown"/> 的点击次数判断双击。
    /// </remarks>
    public static void EnableDoubleClick(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);

        element.MouseLeftButtonDown += (sender, e) =>
        {
            if (e.ClickCount != 2 || sender is not UIElement source)
            {
                return;
            }

            RaiseDoubleClickEvent(source, new DoubleClickEventArgs(e.GetPosition(source)));
            e.Handled = true;
        };
    }
}
