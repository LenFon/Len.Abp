using Len.Abp.Wpf;
using System.Collections.ObjectModel;
using System.Windows;

namespace WpfApp.Controls;

/// <summary>
/// <see cref="TagInput"/> 使用的附加依赖属性与附加路由事件。
/// </summary>
/// <remarks>
/// 这些成员全部由 <c>AttachedDependencyPropertyGenerator</c> 与
/// <c>AttachedRoutedEventGenerator</c> 生成，本类只提供特性声明与分部钩子实现。
/// 宿主必须是 <see cref="DependencyObject"/> 的派生类型——WPF 注册附加属性时要求
/// <c>ownerType</c> 派生自 <see cref="DependencyObject"/>，静态类无法满足。
/// 本类不实例化，成员全部是静态的。
/// </remarks>
[AttachedDependencyProperty<ObservableCollection<string>>(
    "Tags",
    Category = "行为",
    Description = "标签集合。集合是可变引用类型，因此不使用默认值，由控件按实例创建。",
    Flags = FrameworkPropertyMetadataOptions.BindsTwoWayByDefault)]
[AttachedDependencyProperty<int>(
    "MaxTagLength",
    DefaultValue = 12,
    Category = "行为",
    Description = "单个标签的最大长度，校验允许 1 到 64，强制值再收敛到该区间。")]
[AttachedDependencyProperty<bool>(
    "AllowDuplicates",
    DefaultValue = false,
    Category = "行为",
    Description = "是否允许重复标签。")]
[AttachedDependencyProperty<Thickness>(
    "TagPadding",
    DefaultValueExpression = "new global::System.Windows.Thickness(8, 2, 8, 2)",
    Category = "外观",
    Description = "标签胶囊的内边距，默认值由表达式提供。",
    Flags = FrameworkPropertyMetadataOptions.AffectsMeasure)]
[AttachedDependencyProperty<int>(
    "TagCount",
    DefaultValue = 0,
    IsReadOnly = true,
    Category = "只读",
    Description = "当前标签数量，只读附加属性，仅由控件内部写入。")]
[AttachedRoutedEvent<TagAddedEventHandler>(
    "TagAdded",
    RoutingStrategy.Bubble,
    Category = "行为",
    Description = "成功添加标签后沿冒泡路由向上传递。")]
[AttachedRoutedEvent<TagRejectedEventHandler>(
    "TagRejected",
    RoutingStrategy.Bubble,
    Category = "行为",
    Description = "标签被拒绝（超长或重复）时沿冒泡路由向上传递。")]
public partial class TagInputAssist : DependencyObject
{
    /// <summary>
    /// 校验钩子：拒绝小于 1 或大于 64 的最大长度。
    /// </summary>
    static partial void IsMaxTagLengthValid(int value, ref bool result) => result = value is >= 1 and <= 64;

    /// <summary>
    /// 强制值钩子：把最大长度收敛到 1 到 64。
    /// </summary>
    static partial void CoerceMaxTagLength(DependencyObject element, int value, ref int result) =>
        result = Math.Clamp(value, 1, 64);

    /// <summary>
    /// 标签集合变化时同步只读的标签数量。
    /// </summary>
    /// <remarks>
    /// 只读附加属性的 Set 访问器是 <c>protected</c>，只有本类能写入，
    /// 因此数量同步放在这里而不是控件里。
    /// </remarks>
    static partial void OnTagsChanged(
        DependencyObject element,
        ObservableCollection<string> oldValue,
        ObservableCollection<string> newValue
    ) => SetTagCount(element, newValue?.Count ?? 0);

    /// <summary>
    /// 按当前集合内容刷新只读的标签数量。
    /// </summary>
    /// <remarks>
    /// 集合内部增删不会触发依赖属性变更回调，控件改动集合后需要显式调用本方法。
    /// </remarks>
    /// <param name="element">标签输入控件。</param>
    public static void RefreshTagCount(DependencyObject element) =>
        SetTagCount(element, GetTags(element)?.Count ?? 0);
}
