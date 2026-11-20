using System.Windows;

namespace WpfApp.Controls;

/// <summary>
/// <see cref="RatingControl.RatingChanged"/> 事件的处理程序委托。
/// </summary>
public delegate void RatingChangedEventHandler(object sender, RatingChangedEventArgs e);

/// <summary>
/// 评分变化事件参数。
/// </summary>
/// <param name="rating">变化后的评分值。</param>
public class RatingChangedEventArgs(int rating) : RoutedEventArgs
{
    /// <summary>
    /// 获取变化后的评分值。
    /// </summary>
    public int Rating { get; } = rating;
}
