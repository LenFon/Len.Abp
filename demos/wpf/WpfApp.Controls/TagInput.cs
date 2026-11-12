using Len.Abp.Wpf;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace WpfApp.Controls;

/// <summary>
/// 标签输入控件，用于演示附加依赖属性与附加路由事件生成器。
/// </summary>
/// <remarks>
/// 这是真正的自定义控件：派生自 <see cref="Control"/>，外观由
/// <c>Themes/Generic.xaml</c> 中的 <see cref="ControlTemplate"/> 提供。
/// 模板通过 <c>PART_InputBox</c> 约定把输入框交给控件，控件用
/// <see cref="OnApplyTemplate"/> 取回引用，这是自定义控件的标准做法。
/// <para>
/// 附加依赖属性部分覆盖：引用类型与值类型、默认值与默认值表达式、分类与描述、
/// 元数据标志、只读附加属性，以及校验、强制值、变更三个静态分部钩子。
/// 附加路由事件部分覆盖：自定义事件处理程序委托与冒泡路由。
/// </para>
/// </remarks>
[DependencyProperty<string>(
    "Text",
    DefaultValue = "",
    Category = "行为",
    Description = "输入框中的文本，更新源触发器为 PropertyChanged。",
    Flags = FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
    UpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.PropertyChanged)]
[DependencyProperty<string>(
    "Placeholder",
    DefaultValue = "输入标签后按回车",
    Category = "外观",
    Description = "输入框为空时显示的占位文本。")]
public partial class TagInput : Control
{
    private TextBox? _inputBox;

    static TagInput()
    {
        // 删除按钮由模板生成，无法在代码里逐个订阅 Click，改用类处理器统一接收。
        EventManager.RegisterClassHandler(typeof(TagInput), ButtonBase.ClickEvent, new RoutedEventHandler(OnTagButtonClick));
    }

    /// <summary>
    /// 初始化标签输入控件。
    /// </summary>
    public TagInput()
    {
        // 集合类型的默认值不能放进 PropertyMetadata：元数据里的默认值由所有实例共享，
        // 会在不同控件之间串数据。这里按实例创建，顺便演示附加属性的 Set 访问器。
        TagInputAssist.SetTags(this, []);
        TagInputAssist.SetTagPadding(this, new Thickness(8, 2, 8, 2));
    }

    /// <summary>
    /// 获取标签集合，供模板绑定。
    /// </summary>
    public ObservableCollection<string> Tags => TagInputAssist.GetTags(this);

    /// <summary>
    /// 模板应用后取回输入框引用并接管键盘输入。
    /// </summary>
    public override void OnApplyTemplate()
    {
        _inputBox?.KeyDown -= OnInputKeyDown;

        base.OnApplyTemplate();

        _inputBox = GetTemplateChild("PART_InputBox") as TextBox;

        _inputBox?.KeyDown += OnInputKeyDown;
    }

    /// <summary>
    /// 文本变化时刷新占位文本的可见性。
    /// </summary>
    partial void OnTextChanged(string oldValue, string newValue) => UpdatePlaceholder();

    /// <summary>
    /// 占位文本变化时刷新显示。
    /// </summary>
    partial void OnPlaceholderChanged(string oldValue, string newValue) => UpdatePlaceholder();

    /// <summary>
    /// 移除指定标签，供模板中的删除按钮调用。
    /// </summary>
    /// <param name="tag">要移除的标签。</param>
    public void RemoveTag(string tag)
    {
        var tags = TagInputAssist.GetTags(this);

        if (tags.Remove(tag))
        {
            TagInputAssist.RefreshTagCount(this);
        }
    }

    private static void OnTagButtonClick(object sender, RoutedEventArgs e)
    {
        if (sender is not TagInput input)
        {
            return;
        }

        // 类处理器收到的是冒泡到控件的事件，e.Source 可能已被模板内部改写，
        // 因此从 OriginalSource 沿可视树向上找带标签的删除按钮。
        if (FindTagButton(e.OriginalSource as DependencyObject) is { Tag: string tag })
        {
            input.RemoveTag(tag);
        }
    }

    private static Button? FindTagButton(DependencyObject? node)
    {
        while (node is not null and not TagInput)
        {
            if (node is Button { Tag: string })
            {
                return (Button)node;
            }

            node = System.Windows.Media.VisualTreeHelper.GetParent(node);
        }

        return null;
    }

    private void UpdatePlaceholder()
    {
        if (GetTemplateChild("PART_Placeholder") is TextBlock placeholder)
        {
            placeholder.Text = Placeholder;
            placeholder.Visibility = string.IsNullOrEmpty(Text) ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void OnInputKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
        {
            return;
        }

        e.Handled = true;

        var tag = Text.Trim();
        if (tag.Length == 0)
        {
            return;
        }

        var tags = TagInputAssist.GetTags(this);

        if (tag.Length > TagInputAssist.GetMaxTagLength(this))
        {
            TagInputAssist.RaiseTagRejectedEvent(this, new TagEventArgs(tag));
            return;
        }

        if (!TagInputAssist.GetAllowDuplicates(this) && tags.Contains(tag))
        {
            TagInputAssist.RaiseTagRejectedEvent(this, new TagEventArgs(tag));
            return;
        }

        tags.Add(tag);
        TagInputAssist.RefreshTagCount(this);
        TagInputAssist.RaiseTagAddedEvent(this, new TagEventArgs(tag));

        Text = string.Empty;
    }
}
