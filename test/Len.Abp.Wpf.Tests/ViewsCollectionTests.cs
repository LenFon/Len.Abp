using Len.Abp.Wpf.Mvvm;
using Len.Abp.Wpf.Navigation.Regions;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Xunit;

namespace Len.Abp.Wpf.Tests;

/// <summary>
/// 覆盖 <see cref="ViewsCollection"/> 的过滤、通知与元数据变更响应。
/// </summary>
public class ViewsCollectionTests
{
    /// <summary>
    /// 构造时按过滤条件填充已存在的元数据。
    /// </summary>
    [Fact]
    public void Constructor_PopulatesExistingItems()
    {
        var metadata = new ObservableCollection<RegionMetadata>
        {
            new(new FakeView("active")) { IsActive = true },
            new(new FakeView("inactive")),
        };

        var collection = new ViewsCollection(metadata, x => x.IsActive);

        Assert.Equal(1, collection.Count);
        Assert.Equal("active", Assert.Single(collection).ViewName);
    }

    /// <summary>
    /// 构造时按插入顺序保留已存在的元数据。
    /// </summary>
    [Fact]
    public void Constructor_PreservesInsertionOrder()
    {
        var metadata = new ObservableCollection<RegionMetadata>
        {
            new(new FakeView("charlie")),
            new(new FakeView("alpha")),
            new(new FakeView("bravo")),
        };

        var collection = new ViewsCollection(metadata, _ => true);

        Assert.Equal(["charlie", "alpha", "bravo"], collection.Select(v => v.ViewName));
    }

    /// <summary>
    /// 构造时已存在的元数据可被 Contains 命中。
    /// </summary>
    [Fact]
    public void Constructor_ExistingItemsAreContained()
    {
        var view = new FakeView("a");
        var metadata = new ObservableCollection<RegionMetadata> { new(view) };

        var collection = new ViewsCollection(metadata, _ => true);

        Assert.True(collection.Contains(view));
    }

    /// <summary>
    /// 添加元数据时按过滤条件决定是否加入集合。
    /// </summary>
    [Fact]
    public void AddMetadata_AppliesFilter()
    {
        var metadata = new ObservableCollection<RegionMetadata>();
        var collection = new ViewsCollection(metadata, x => x.IsActive);

        metadata.Add(new RegionMetadata(new FakeView("inactive")));
        Assert.Equal(0, collection.Count);

        metadata.Add(new RegionMetadata(new FakeView("active")) { IsActive = true });
        Assert.Equal(1, collection.Count);
    }

    /// <summary>
    /// 添加元数据时集合发出 Add 通知。
    /// </summary>
    [Fact]
    public void AddMetadata_RaisesAddNotification()
    {
        var metadata = new ObservableCollection<RegionMetadata>();
        var collection = new ViewsCollection(metadata, _ => true);
        var actions = new List<NotifyCollectionChangedAction>();
        collection.CollectionChanged += (_, e) => actions.Add(e.Action);

        metadata.Add(new RegionMetadata(new FakeView("a")));

        Assert.Equal([NotifyCollectionChangedAction.Add], actions);
    }

    /// <summary>
    /// 移除元数据时集合发出 Remove 通知。
    /// </summary>
    [Fact]
    public void RemoveMetadata_RaisesRemoveNotification()
    {
        var metadata = new ObservableCollection<RegionMetadata>();
        var collection = new ViewsCollection(metadata, _ => true);
        var item = new RegionMetadata(new FakeView("a"));
        metadata.Add(item);

        var actions = new List<NotifyCollectionChangedAction>();
        collection.CollectionChanged += (_, e) => actions.Add(e.Action);

        metadata.Remove(item);

        Assert.Equal([NotifyCollectionChangedAction.Remove], actions);
        Assert.Equal(0, collection.Count);
    }

    /// <summary>
    /// 清空元数据时集合发出 Reset 通知。
    /// </summary>
    [Fact]
    public void ClearMetadata_RaisesResetNotification()
    {
        var metadata = new ObservableCollection<RegionMetadata>();
        var collection = new ViewsCollection(metadata, _ => true);
        metadata.Add(new RegionMetadata(new FakeView("a")));

        var actions = new List<NotifyCollectionChangedAction>();
        collection.CollectionChanged += (_, e) => actions.Add(e.Action);

        metadata.Clear();

        Assert.Equal([NotifyCollectionChangedAction.Reset], actions);
        Assert.Equal(0, collection.Count);
    }

    /// <summary>
    /// 元数据变为匹配过滤条件时加入集合并发出 Add 通知。
    /// </summary>
    [Fact]
    public void MetadataBecomesMatching_AddsToCollection()
    {
        var metadata = new ObservableCollection<RegionMetadata>();
        var collection = new ViewsCollection(metadata, x => x.IsActive);
        var item = new RegionMetadata(new FakeView("a"));
        metadata.Add(item);

        var actions = new List<NotifyCollectionChangedAction>();
        collection.CollectionChanged += (_, e) => actions.Add(e.Action);

        item.IsActive = true;

        Assert.Equal(1, collection.Count);
        Assert.Equal([NotifyCollectionChangedAction.Add], actions);
    }

    /// <summary>
    /// 元数据变为不匹配过滤条件时从集合移除并发出 Remove 通知。
    /// </summary>
    [Fact]
    public void MetadataBecomesNonMatching_RemovesFromCollection()
    {
        var metadata = new ObservableCollection<RegionMetadata>();
        var collection = new ViewsCollection(metadata, x => x.IsActive);
        var item = new RegionMetadata(new FakeView("a")) { IsActive = true };
        metadata.Add(item);

        var actions = new List<NotifyCollectionChangedAction>();
        collection.CollectionChanged += (_, e) => actions.Add(e.Action);

        item.IsActive = false;

        Assert.Equal(0, collection.Count);
        Assert.Equal([NotifyCollectionChangedAction.Remove], actions);
    }

    /// <summary>
    /// 元数据重复触发匹配变更时不会重复加入。
    /// </summary>
    [Fact]
    public void MetadataMatchingTwice_DoesNotDuplicate()
    {
        var metadata = new ObservableCollection<RegionMetadata>();
        var collection = new ViewsCollection(metadata, x => x.IsActive);
        var item = new RegionMetadata(new FakeView("a")) { IsActive = true };
        metadata.Add(item);

        item.IsActive = true;

        Assert.Equal(1, collection.Count);
    }

    /// <summary>
    /// 集合按插入顺序排列，而不是按视图名称。
    /// </summary>
    [Fact]
    public void Collection_PreservesInsertionOrder()
    {
        var metadata = new ObservableCollection<RegionMetadata>();
        var collection = new ViewsCollection(metadata, _ => true);

        metadata.Add(new RegionMetadata(new FakeView("charlie")));
        metadata.Add(new RegionMetadata(new FakeView("alpha")));
        metadata.Add(new RegionMetadata(new FakeView("bravo")));

        Assert.Equal(["charlie", "alpha", "bravo"], collection.Select(v => v.ViewName));
    }

    /// <summary>
    /// Contains 对已包含的视图返回 true。
    /// </summary>
    [Fact]
    public void Contains_ExistingView_ReturnsTrue()
    {
        var metadata = new ObservableCollection<RegionMetadata>();
        var collection = new ViewsCollection(metadata, _ => true);
        var view = new FakeView("a");
        metadata.Add(new RegionMetadata(view));

        Assert.True(collection.Contains(view));
        Assert.False(collection.Contains(new FakeView("b")));
    }

    /// <summary>
    /// 非泛型枚举与泛型枚举返回相同元素。
    /// </summary>
    [Fact]
    public void NonGenericEnumerate_ReturnsSameItems()
    {
        var metadata = new ObservableCollection<RegionMetadata>();
        var collection = new ViewsCollection(metadata, _ => true);
        metadata.Add(new RegionMetadata(new FakeView("a")));

        var count = 0;
        foreach (var _ in (System.Collections.IEnumerable)collection)
        {
            count++;
        }

        Assert.Equal(1, count);
    }

    /// <summary>
    /// 移除不匹配过滤条件的元数据时不发出通知。
    /// </summary>
    [Fact]
    public void RemoveNonMatchingMetadata_DoesNotNotify()
    {
        var metadata = new ObservableCollection<RegionMetadata>();
        var collection = new ViewsCollection(metadata, x => x.IsActive);
        var item = new RegionMetadata(new FakeView("a"));
        metadata.Add(item);

        var actions = new List<NotifyCollectionChangedAction>();
        collection.CollectionChanged += (_, e) => actions.Add(e.Action);

        metadata.Remove(item);

        Assert.Empty(actions);
    }

    /// <summary>
    /// 空元数据集合构造出空视图集合，计数为零且可枚举。
    /// </summary>
    [Fact]
    public void Constructor_EmptyMetadata_ProducesEmptyCollection()
    {
        var collection = new ViewsCollection([], _ => true);

        Assert.Equal(0, collection.Count);
        Assert.Empty(collection);
        Assert.False(collection.Contains(new FakeView("a")));
    }

    /// <summary>
    /// 过滤条件恒为 false 时集合始终为空，即使元数据有内容。
    /// </summary>
    [Fact]
    public void FilterAlwaysFalse_KeepsCollectionEmpty()
    {
        var metadata = new ObservableCollection<RegionMetadata>
        {
            new(new FakeView("a")),
            new(new FakeView("b")),
        };

        var collection = new ViewsCollection(metadata, _ => false);

        Assert.Equal(0, collection.Count);
        Assert.Empty(collection);
    }

    /// <summary>
    /// 大量元数据按插入顺序全部进入集合。
    /// </summary>
    [Fact]
    public void ManyMetadataItems_PreserveOrder()
    {
        var metadata = new ObservableCollection<RegionMetadata>(
            Enumerable.Range(0, 1_000).Select(i => new RegionMetadata(new FakeView($"v{i}")))
        );

        var collection = new ViewsCollection(metadata, _ => true);

        Assert.Equal(1_000, collection.Count);
        Assert.Equal("v0", collection.First().ViewName);
        Assert.Equal("v999", collection.Last().ViewName);
    }

    /// <summary>
    /// 清空元数据集合时发出 Reset 通知并清空视图集合。
    /// </summary>
    [Fact]
    public void ClearMetadata_ResetsCollection()
    {
        var metadata = new ObservableCollection<RegionMetadata>
        {
            new(new FakeView("a")),
            new(new FakeView("b")),
        };
        var collection = new ViewsCollection(metadata, _ => true);

        var actions = new List<NotifyCollectionChangedAction>();
        collection.CollectionChanged += (_, e) => actions.Add(e.Action);

        metadata.Clear();

        Assert.Equal([NotifyCollectionChangedAction.Reset], actions);
        Assert.Equal(0, collection.Count);
    }

    /// <summary>
    /// 同一视图重复加入元数据时集合按元数据条数计数，不做去重。
    /// </summary>
    /// <remarks>
    /// 去重由 <see cref="Region.Add"/> 负责；集合本身只反映元数据内容。
    /// </remarks>
    [Fact]
    public void DuplicateViewInstances_AreCountedPerMetadataEntry()
    {
        var view = new FakeView("a");
        var metadata = new ObservableCollection<RegionMetadata>
        {
            new(view),
            new(view),
        };

        var collection = new ViewsCollection(metadata, _ => true);

        Assert.Equal(2, collection.Count);
    }
}
