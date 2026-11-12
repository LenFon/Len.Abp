using Len.Abp.Wpf.Mvvm;

namespace Len.Abp.Wpf.Navigation.Regions;

/// <summary>
/// 保存区域内单个视图的元数据及其激活状态。
/// </summary>
internal sealed class RegionMetadata(IView item)
{
    private bool _isActive;

    /// <summary>
    /// 当激活状态发生变化时触发。
    /// </summary>
    public event EventHandler? MetadataChanged;

    /// <summary>
    /// 获取或设置该视图是否处于激活状态。值未变化时不会触发 <see cref="MetadataChanged"/>。
    /// </summary>
    public bool IsActive
    {
        get => _isActive;
        set
        {
            if (_isActive == value)
            {
                return;
            }

            _isActive = value;
            MetadataChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// 获取该元数据对应的视图。
    /// </summary>
    public IView Item { get; } = item;
}
