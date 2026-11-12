using System.ComponentModel;

namespace WpfApp.Controls;

/// <summary>
/// 评分控件中的单颗星。
/// </summary>
/// <remarks>
/// 自定义控件的模板通过 <c>ItemsControl</c> 渲染星标，每颗星是一个数据项。
/// 这里实现 <see cref="INotifyPropertyChanged"/>，让 <see cref="IsFilled"/> 变化时
/// 模板里的 <c>DataTrigger</c> 能立即刷新颜色。
/// </remarks>
public sealed class RatingStar(int value) : INotifyPropertyChanged
{
    private bool _isFilled;

    /// <summary>
    /// 获取该星代表的评分值。
    /// </summary>
    public int Value { get; } = value;

    /// <summary>
    /// 获取或设置该星是否被点亮。
    /// </summary>
    public bool IsFilled
    {
        get => _isFilled;
        internal set
        {
            if (_isFilled == value)
            {
                return;
            }

            _isFilled = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsFilled)));
        }
    }

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;
}
