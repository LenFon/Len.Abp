using Len.Abp.Wpf.Mvvm;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Len.Abp.Wpf.Navigation.Regions;

/// <summary>
/// <see cref="IViewsCollection"/> 的默认实现，按过滤条件实时维护视图列表。
/// </summary>
internal sealed class ViewsCollection : IViewsCollection
{
    private readonly Predicate<RegionMetadata> _filter;
    private readonly ObservableCollection<RegionMetadata> _items;
    private readonly HashSet<RegionMetadata> _monitoredItems = [];
    private List<IView> _filteredItems = [];

    /// <summary>
    /// 初始化视图集合。
    /// </summary>
    /// <param name="items">区域元数据集合。</param>
    /// <param name="filter">决定元数据是否包含在集合中的过滤条件。</param>
    public ViewsCollection(ObservableCollection<RegionMetadata> items, Predicate<RegionMetadata> filter)
    {
        _items = items;
        _filter = filter;
        MonitorAllMetadataItems();
        UpdateFilteredItemsList();
        _items.CollectionChanged += OnItemsCollectionChanged;
    }

    /// <inheritdoc/>
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    /// <inheritdoc/>
    public int Count => _filteredItems.Count;

    /// <inheritdoc/>
    public bool Contains(IView value) => _filteredItems.Contains(value);

    /// <inheritdoc/>
    public IEnumerator<IView> GetEnumerator() => _filteredItems.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private void MonitorAllMetadataItems()
    {
        foreach (var item in _items)
        {
            AddMetadataMonitor(item);
        }
    }

    private void AddMetadataMonitor(RegionMetadata itemMetadata)
    {
        itemMetadata.MetadataChanged += OnMetadataChanged;
        _monitoredItems.Add(itemMetadata);
    }

    private void RemoveMetadataMonitor(RegionMetadata itemMetadata)
    {
        itemMetadata.MetadataChanged -= OnMetadataChanged;
        _monitoredItems.Remove(itemMetadata);
    }

    private void ResetAllMonitors()
    {
        foreach (var item in _monitoredItems)
        {
            item.MetadataChanged -= OnMetadataChanged;
        }

        _monitoredItems.Clear();
        MonitorAllMetadataItems();
    }

    private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                UpdateFilteredItemsList();
                foreach (RegionMetadata itemMetadata in e.NewItems!)
                {
                    AddMetadataMonitor(itemMetadata);
                    if (_filter(itemMetadata))
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
                        NotifyRemove(itemMetadata.Item);
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
        if (sender is not RegionMetadata itemMetadata || !_monitoredItems.Contains(itemMetadata))
        {
            return;
        }

        if (_filter(itemMetadata))
        {
            if (_filteredItems.Contains(itemMetadata.Item))
            {
                return;
            }

            UpdateFilteredItemsList();
            NotifyAdd(itemMetadata.Item);
        }
        else
        {
            NotifyRemove(itemMetadata.Item);
        }
    }

    /// <summary>
    /// 先更新过滤列表再通知，使订阅者按新索引接收变更。
    /// </summary>
    private void NotifyAdd(IView item)
    {
        UpdateFilteredItemsList();
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, _filteredItems.IndexOf(item)));
    }

    /// <summary>
    /// 先记录旧索引再更新过滤列表，使订阅者按移除前的位置接收变更。
    /// </summary>
    private void NotifyRemove(IView item)
    {
        var index = _filteredItems.IndexOf(item);
        UpdateFilteredItemsList();
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
    }

    private void NotifyReset() =>
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

    private void OnCollectionChanged(NotifyCollectionChangedEventArgs e) => CollectionChanged?.Invoke(this, e);

    private void UpdateFilteredItemsList() =>
        _filteredItems = [.. _items.Where(w => _filter(w)).Select(s => s.Item).OrderBy(o => o.ViewName)];
}
