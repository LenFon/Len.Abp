using Len.Abp.Wpf.Mvvm;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Len.Abp.Wpf.Navigation.Regions;

public class Region : IRegion
{
    private readonly ObservableCollection<ItemMetadata> _itemMetadataCollection = [];
    private ViewsCollection? _activeViews;
    private string _name = default!;
    private ViewsCollection? _views;

    public Region(string viewName)
    {
        Behaviors = new RegionBehaviorCollection(this);
        ViewName = viewName;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public IViewsCollection ActiveViews
    {
        get
        {
            _views ??= new(_itemMetadataCollection, x => true);
            return _activeViews ??= new(_itemMetadataCollection, x => x.IsActive);
        }
    }

    public IRegionBehaviorCollection Behaviors { get; }

    public string Name
    {
        get => _name;
        set
        {
            if (_name is not null && _name != value)
            {
                throw new InvalidOperationException($"Cannot be changed once it has been set. The current region name is '{_name}'.");
            }

            if (value.IsNullOrEmpty())
            {
                throw new ArgumentException("Region name cannot be null or empty.");
            }

            _name = value;
            OnPropertyChanged(nameof(Name));
        }
    }

    public string ViewName { get; }

    public IViewsCollection Views => _views ??= new(_itemMetadataCollection, x => true);

    public virtual void Activate(IView view)
    {
        var itemMetadata = GetItemMetadataOrThrow(view);

        if (!itemMetadata.IsActive)
        {
            itemMetadata.IsActive = true;
        }
    }

    public void Add(IView view) => InternalAdd(view, null);

    public void Add(IView view, string viewName)
    {
        if (viewName.IsNullOrEmpty())
        {
            throw new ArgumentNullException(nameof(viewName), "View name cannot be null.");
        }

        InternalAdd(view, viewName);
    }

    public virtual void Deactivate(IView view)
    {
        var itemMetadata = GetItemMetadataOrThrow(view);

        if (itemMetadata.IsActive)
        {
            itemMetadata.IsActive = false;
        }
    }

    public IView? GetView(string viewName)
    {
        if (viewName.IsNullOrEmpty())
        {
            throw new ArgumentException("View name cannot be null or empty.", nameof(viewName));
        }

        var metadata = _itemMetadataCollection.FirstOrDefault(x => x.Item.ViewName == viewName);

        return metadata?.Item;
    }

    public void Remove(IView view)
    {
        ItemMetadata itemMetadata = GetItemMetadataOrThrow(view);

        _itemMetadataCollection.Remove(itemMetadata);
    }

    public void RemoveAll()
    {
        foreach (var view in Views)
        {
            Remove(view);
        }
    }

    private ItemMetadata GetItemMetadataOrThrow(IView view)
    {
        ArgumentNullException.ThrowIfNull(view, nameof(view));
        var itemMetadata =
            _itemMetadataCollection.FirstOrDefault(x => x.Item == view)
            ?? throw new ArgumentException($"The region does not contain the specified view .");
        return itemMetadata;
    }

    private void InternalAdd(IView view, string? viewName)
    {
        if (_itemMetadataCollection.Any(x => x.Item == view))
        {
            throw new ArgumentException("View already exists in region.");
        }

        var itemMetadata = new ItemMetadata(view);

        _itemMetadataCollection.Add(itemMetadata);
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class ItemMetadata(IView item)
    {
        private bool _isActive;

        public event EventHandler? MetadataChanged;

        public bool IsActive
        {
            get { return _isActive; }
            set { _isActive = value; InvokeMetadataChanged(); }
        }

        public IView Item { get; } = item;

        private void InvokeMetadataChanged()
        {
            var metadataChangedHandler = MetadataChanged;

            metadataChangedHandler?.Invoke(this, EventArgs.Empty);
        }
    }
}