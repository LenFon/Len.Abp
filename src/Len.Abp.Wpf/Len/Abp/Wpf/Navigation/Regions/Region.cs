using Len.Abp.Wpf.Mvvm;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace Len.Abp.Wpf.Navigation.Regions;

public class Region : IRegion
{
    private readonly ViewsCollection _activeViews;
    private readonly ObservableCollection<RegionMetadata> _metadata = [];
    private readonly ViewsCollection _views;

    public Region(string name, string owner)
    {
        Behaviors = new RegionBehaviorCollection(this);
        _views = new(_metadata, x => true);
        _activeViews = new(_metadata, x => x.IsActive);
        Name = name;
        Owner = owner;
    }

    public IViewsCollection ActiveViews => _activeViews;

    public IRegionBehaviorCollection Behaviors { get; }

    public string Name { get; }

    public string Owner { get; }

    public IViewsCollection Views => _views;

    public virtual void Activate(IView view)
    {
        var metadata = GetMetadataOrThrow(view);

        if (!metadata.IsActive)
        {
            metadata.IsActive = true;
        }
    }

    public void Add(IView view)
    {
        if (_metadata.Any(x => x.Item == view))
        {
            throw new ArgumentException("View already exists in region.");
        }

        var itemMetadata = new RegionMetadata(view);

        _metadata.Add(itemMetadata);
    }

    public virtual void Deactivate(IView view)
    {
        var metadata = GetMetadataOrThrow(view);

        if (metadata.IsActive)
        {
            metadata.IsActive = false;
        }
    }

    public IView? GetView(string viewName) => _metadata.FirstOrDefault(x => x.Item.ViewName == viewName)?.Item;

    public void Remove(IView view)
    {
        var metadata = GetMetadataOrThrow(view);

        _metadata.Remove(metadata);
    }

    public void RemoveAll()
    {
        foreach (var view in Views)
        {
            Remove(view);
        }
    }

    [return: NotNull]
    private RegionMetadata GetMetadataOrThrow(IView view)
    {
        var metadata =
            _metadata.FirstOrDefault(x => x.Item == view)
            ?? throw new ArgumentException($"The region does not contain the specified view .");
        return metadata;
    }
}