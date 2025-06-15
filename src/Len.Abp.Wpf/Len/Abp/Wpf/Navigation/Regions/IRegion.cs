using System.ComponentModel;

namespace Len.Abp.Wpf.Navigation.Regions;

public interface IRegion : INotifyPropertyChanged
{
    IViewsCollection ActiveViews { get; }

    string Name { get; set; }

    Comparison<object?> SortComparison { get; set; }

    IViewsCollection Views { get; }

    void Activate(object view);

    void Add(object view);

    void Add(object view, string viewName);

    void Deactivate(object view);

    object? GetView(string viewName);

    void Remove(object view);

    void RemoveAll();
}