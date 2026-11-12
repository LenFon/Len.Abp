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
    /// 初始化事件参数。
    /// </summary>
    /// <remarks>
    /// 附加路由事件生成器会生成 <c>args ??= new();</c>，因此必须保留无参构造函数。
    /// </remarks>
    public TagEventArgs()
        : this(string.Empty) { }

    /// <summary>
    /// 获取本次操作涉及的标签文本。
    /// </summary>
    public string Tag { get; } = tag;
}
