using Len.Abp.Wpf.Mvvm;
using System.Collections.Specialized;

namespace Len.Abp.Wpf.Navigation.Regions;

public interface IViewsCollection : IEnumerable<IView>, INotifyCollectionChanged
{
    int Count { get; }

    bool Contains(IView value);
}