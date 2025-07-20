using Len.Abp.Wpf.Mvvm;

namespace Len.Abp.Wpf.Navigation.Regions;

public interface IRegion
{
    IViewsCollection ActiveViews { get; }

    IRegionBehaviorCollection Behaviors { get; }

    string Name { get; }

    string Owner { get; }

    IViewsCollection Views { get; }

    void Activate(IView view);

    void Add(IView view);

    void Deactivate(IView view);

    IView? GetView(string viewName);

    void Remove(IView view);

    void RemoveAll();
}