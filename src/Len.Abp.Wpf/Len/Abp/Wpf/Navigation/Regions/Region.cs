using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Windows;

namespace Len.Abp.Wpf.Navigation.Regions;

public class Region : IRegion
{
    private ViewsCollection? _activeViews;
    private ObservableCollection<ItemMetadata> _itemMetadataCollection = [];
    private string _name = default!;
    private Comparison<object?> _sort;
    private ViewsCollection? _views;

    public Region()
    {
        _sort = DefaultSortComparison;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public IViewsCollection ActiveViews
    {
        get
        {
            _views ??= new(_itemMetadataCollection, x => true) { SortComparison = _sort };
            return _activeViews ??= new(_itemMetadataCollection, x => x.IsActive) { SortComparison = _sort };
        }
    }

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

    public Comparison<object?> SortComparison
    {
        get => _sort;
        set
        {
            _sort = value;

            if (_activeViews is not null)
            {
                _activeViews.SortComparison = _sort;
            }

            if (_views is not null)
            {
                _views.SortComparison = _sort;
            }
        }
    }

    public IViewsCollection Views => _views ??= new(_itemMetadataCollection, x => true) { SortComparison = _sort };

    public void Activate(object view)
    {
        var itemMetadata = GetItemMetadataOrThrow(view);

        if (!itemMetadata.IsActive)
        {
            itemMetadata.IsActive = true;
        }
    }

    public void Add(object view) => InternalAdd(view, null);

    public void Add(object view, string viewName)
    {
        if (viewName.IsNullOrEmpty())
        {
            throw new ArgumentNullException(nameof(viewName), "View name cannot be null.");
        }

        InternalAdd(view, viewName);
    }

    public void Deactivate(object view)
    {
        var itemMetadata = GetItemMetadataOrThrow(view);

        if (itemMetadata.IsActive)
        {
            itemMetadata.IsActive = false;
        }
    }

    public object? GetView(string viewName)
    {
        if (viewName.IsNullOrEmpty())
        {
            throw new ArgumentException("View name cannot be null or empty.", nameof(viewName));
        }

        var metadata = _itemMetadataCollection.FirstOrDefault(x => x.Name == viewName);

        return metadata?.Item;
    }

    public void Remove(object view)
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

    private static int ViewSortHintAttributeComparison(ViewSortHintAttribute? x, ViewSortHintAttribute? y)
    {
        if (x == null)
        {
            return y == null ? 0 : -1;
        }
        else
        {
            return y == null ? 1 : string.Compare(x.Hint, y.Hint, StringComparison.Ordinal);
        }
    }

    private int DefaultSortComparison(object? x, object? y)
    {
        if (x is null)
        {
            return y is null ? 0 : -1;
        }
        else
        {
            if (y is null)
            {
                return 1;
            }

            var xAttribute = x.GetType().GetCustomAttribute<ViewSortHintAttribute>(true);
            var yAttribute = y.GetType().GetCustomAttribute<ViewSortHintAttribute>(true);

            return ViewSortHintAttributeComparison(xAttribute, yAttribute);
        }
    }

    private ItemMetadata GetItemMetadataOrThrow(object view)
    {
        ArgumentNullException.ThrowIfNull(view, nameof(view));
        var itemMetadata =
            _itemMetadataCollection.FirstOrDefault(x => x.Item == view)
            ?? throw new ArgumentException($"The region does not contain the specified view .");
        return itemMetadata;
    }

    private void InternalAdd(object view, string? viewName)
    {
        if (_itemMetadataCollection.Any(x => x.Item == view))
        {
            throw new ArgumentException("View already exists in region.");
        }

        var itemMetadata = new ItemMetadata(view);

        if (!viewName.IsNullOrEmpty())
        {
            if (_itemMetadataCollection.Any(x => x.Name == viewName))
            {
                throw new ArgumentException($"A view with the name '{viewName}' already exists in the region.");
            }

            itemMetadata.Name = viewName;
        }

        _itemMetadataCollection.Add(itemMetadata);
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class ItemMetadata(object item) : DependencyObject
    {
        public static readonly DependencyProperty IsActiveProperty = DependencyProperty.Register(
            "IsActive",
            typeof(bool),
            typeof(ItemMetadata),
            new PropertyMetadata(false, OnIsActiveChanged)
        );

        public event EventHandler? MetadataChanged;

        public bool IsActive
        {
            get { return (bool)GetValue(IsActiveProperty); }
            set { SetValue(IsActiveProperty, value); }
        }

        public object Item { get; } = item;

        public string? Name { get; set; }

        private static void OnIsActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ItemMetadata { } itemMetadata)
            {
                itemMetadata.InvokeMetadataChanged();
            }
        }

        private void InvokeMetadataChanged()
        {
            var metadataChangedHandler = MetadataChanged;

            metadataChangedHandler?.Invoke(this, EventArgs.Empty);
        }
    }
}