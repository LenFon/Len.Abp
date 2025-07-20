using Len.Abp.Wpf.Mvvm;

namespace Len.Abp.Wpf.Navigation.Regions;

internal class RegionMetadata(IView item)
{
    private bool _isActive;

    public event EventHandler? MetadataChanged;

    public bool IsActive
    {
        get { return _isActive; }
        set { _isActive = value; MetadataChanged?.Invoke(this, EventArgs.Empty); }
    }

    public IView Item { get; } = item;
}