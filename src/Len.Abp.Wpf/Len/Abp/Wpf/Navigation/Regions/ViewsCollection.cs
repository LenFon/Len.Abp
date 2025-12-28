using Len.Abp.Wpf.Mvvm;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Len.Abp.Wpf.Navigation.Regions;

internal sealed class ViewsCollection : IViewsCollection
{
    private readonly Predicate<RegionMetadata> _filter;
    private readonly ObservableCollection<RegionMetadata> _items;
    private readonly Dictionary<RegionMetadata, MonitorInfo> _monitoredItems = [];
    private List<IView> _filteredItems = [];

    public ViewsCollection(ObservableCollection<RegionMetadata> items, Predicate<RegionMetadata> filter)
    {
        _items = items;
        _filter = filter;
        MonitorAllMetadataItems();
        _items.CollectionChanged += OnItemsCollectionChanged;
    }

    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    public int Count => _filteredItems.Count;

    public bool Contains(IView value) => _filteredItems.Contains(value);

    public IEnumerator<IView> GetEnumerator() => _filteredItems.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private void AddMetadataMonitor(RegionMetadata itemMetadata, bool isInList)
    {
        itemMetadata.MetadataChanged += OnMetadataChanged;
        _monitoredItems.Add(itemMetadata, new() { IsInList = isInList });
    }

    private void MonitorAllMetadataItems()
    {
        foreach (var item in _items)
        {
            AddMetadataMonitor(item, _filter(item));
        }
    }

    private void NotifyAdd(IView item)
    {
        var newIndex = _filteredItems.IndexOf(item);

        NotifyAdd(new[] { item }, newIndex);
    }

    private void NotifyAdd(IList items, int newStartingIndex)
    {
        if (items.Count > 0)
        {
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, items, newStartingIndex));
        }
    }

    private void NotifyRemove(IList items, int originalIndex)
    {
        if (items.Count > 0)
        {
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, items, originalIndex));
        }
    }

    private void NotifyReset()
    {
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    private void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        var handler = CollectionChanged;

        handler?.Invoke(this, e);
    }

    private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                UpdateFilteredItemsList();
                foreach (RegionMetadata itemMetadata in e.NewItems!)
                {
                    bool isInFilter = _filter(itemMetadata);

                    AddMetadataMonitor(itemMetadata, isInFilter);
                    if (isInFilter)
                    {
                        NotifyAdd(itemMetadata.Item);
                    }
                }
                break;

            case NotifyCollectionChangedAction.Remove:
                foreach (RegionMetadata itemMetadata in e.OldItems!)
                {
                    RemoveMetadataMonitor(itemMetadata);
                    if (_filter(itemMetadata))
                    {
                        RemoveFromFilteredList(itemMetadata.Item);
                    }
                }
                break;

            default:
                ResetAllMonitors();
                UpdateFilteredItemsList();
                NotifyReset();
                break;
        }
    }

    private void OnMetadataChanged(object? sender, EventArgs e)
    {
        if (sender is not RegionMetadata itemMetadata)
            return;

        if (!_monitoredItems.TryGetValue(itemMetadata, out var monitorInfo))
        {
            return;
        }

        if (_filter(itemMetadata))
        {
            if (!monitorInfo.IsInList)
            {
                monitorInfo.IsInList = true;
                UpdateFilteredItemsList();
                NotifyAdd(itemMetadata.Item);
            }
        }
        else
        {
            monitorInfo.IsInList = false;
            RemoveFromFilteredList(itemMetadata.Item);
        }
    }

    private void RemoveAllMetadataMonitors()
    {
        foreach (var item in _monitoredItems)
        {
            item.Key.MetadataChanged -= OnMetadataChanged;
        }

        _monitoredItems.Clear();
    }

    private void RemoveFromFilteredList(IView item)
    {
        var index = _filteredItems.IndexOf(item);
        UpdateFilteredItemsList();
        NotifyRemove(new[] { item }, index);
    }

    private void RemoveMetadataMonitor(RegionMetadata itemMetadata)
    {
        itemMetadata.MetadataChanged -= OnMetadataChanged;
        _monitoredItems.Remove(itemMetadata);
    }

    private void ResetAllMonitors()
    {
        RemoveAllMetadataMonitors();
        MonitorAllMetadataItems();
    }

    private void UpdateFilteredItemsList() =>
        _filteredItems = [.. _items.Where(w => _filter(w)).Select(s => s.Item).OrderBy(o => o.ViewName)];

    private class MonitorInfo
    {
        public bool IsInList { get; set; }
    }
}