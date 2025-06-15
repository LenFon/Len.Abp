using Len.Abp.Wpf.Mvvm;
using System.ComponentModel;

namespace Len.Abp.Wpf.Navigation.Regions;

public interface IRegion : INotifyPropertyChanged
{
    IViewsCollection ActiveViews { get; }

    IRegionBehaviorCollection Behaviors { get; }

    string Name { get; internal set; }

    string ViewName { get; }

    IViewsCollection Views { get; }

    void Activate(IView view);

    void Add(IView view);

    void Add(IView view, string viewName);

    void Deactivate(IView view);

    IView? GetView(string viewName);

    void Remove(IView view);

    void RemoveAll();
}