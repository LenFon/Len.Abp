using System.Windows;

namespace WpfApp.Controls;

/// <summary>
/// <see cref="TagInputAssist.TagAdded"/> 事件的处理程序委托。
/// </summary>
public delegate void TagAddedEventHandler(object sender, TagEventArgs e);

/// <summary>
/// <see cref="TagInputAssist.TagRejected"/> 事件的处理程序委托。
/// </summary>
public delegate void TagRejectedEventHandler(object sender, TagEventArgs e);

/// <summary>
/// 标签事件参数。
/// </summary>
/// <param name="tag">本次操作涉及的标签文本。</param>
public class TagEventArgs(string tag) : RoutedEventArgs
{
    /// <summary>
    /// 使用空标签初始化事件参数。
    /// </summary>
    public TagEventArgs()
        : this(string.Empty) { }

    /// <summary>
    /// 获取本次操作涉及的标签文本。
    /// </summary>
    public string Tag { get; } = tag;
}
