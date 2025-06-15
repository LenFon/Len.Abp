using System.Collections.Specialized;

namespace Len.Abp.Wpf.Navigation.Regions;

public interface IViewsCollection : IEnumerable<object>, INotifyCollectionChanged
{
    bool Contains(object value);
}