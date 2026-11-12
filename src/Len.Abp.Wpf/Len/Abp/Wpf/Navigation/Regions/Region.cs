using Len.Abp.Wpf.Mvvm;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace Len.Abp.Wpf.Navigation.Regions;

/// <summary>
/// <see cref="IRegion"/> 的默认实现，维护区域内的视图集合及其激活状态。
/// </summary>
public class Region : IRegion
{
    private readonly ViewsCollection _activeViews;
    private readonly ObservableCollection<RegionMetadata> _metadata = [];
    private readonly ViewsCollection _views;

    /// <summary>
    /// 初始化区域。
    /// </summary>
    /// <param name="name">区域名称。</param>
    /// <param name="owner">区域所属的视图名称。</param>
    public Region(string name, string owner)
    {
        Behaviors = new RegionBehaviorCollection(this);
        _views = new(_metadata, x => true);
        _activeViews = new(_metadata, x => x.IsActive);
        Name = name;
        Owner = owner;
    }

    /// <inheritdoc/>
    public IViewsCollection ActiveViews => _activeViews;

    /// <inheritdoc/>
    public IRegionBehaviorCollection Behaviors { get; }

    /// <inheritdoc/>
    public string Name { get; }

    /// <inheritdoc/>
    public string Owner { get; }

    /// <inheritdoc/>
    public IViewsCollection Views => _views;

    /// <inheritdoc/>
    public virtual void Activate(IView view)
    {
        FindMetadata(view).IsActive = true;
    }

    /// <inheritdoc/>
    public void Add(IView view)
    {
        if (_metadata.Any(x => x.Item == view))
        {
            throw new ArgumentException("该视图已存在于区域中。");
        }

        _metadata.Add(new RegionMetadata(view));
    }

    /// <inheritdoc/>
    public virtual void Deactivate(IView view)
    {
        FindMetadata(view).IsActive = false;
    }

    /// <inheritdoc/>
    public IView? GetView(string viewName) => _metadata.FirstOrDefault(x => x.Item.ViewName == viewName)?.Item;

    /// <inheritdoc/>
    public void Remove(IView view)
    {
        _metadata.Remove(FindMetadata(view));
    }

    /// <inheritdoc/>
    public void RemoveAll()
    {
        // 直接清空元数据集合。逐项 Remove 也能工作（Views 返回的是快照），
        // 但每次移除都会重建过滤列表，整体是 O(n²)。
        _metadata.Clear();
    }

    /// <summary>
    /// 查找指定视图的元数据。
    /// </summary>
    /// <param name="view">目标视图。</param>
    /// <returns>该视图的元数据。</returns>
    /// <exception cref="ArgumentException">该视图不在区域中时抛出。</exception>
    [return: NotNull]
    private protected RegionMetadata FindMetadata(IView view) =>
        _metadata.FirstOrDefault(x => x.Item == view)
        ?? throw new ArgumentException("该区域不包含指定的视图。");
}