using Len.Abp.Wpf.Mvvm;
using System.Collections.Specialized;

namespace Len.Abp.Wpf.Navigation.Regions;

/// <summary>
/// 视图集合，按过滤条件实时维护视图列表并在变化时发出通知。
/// </summary>
public interface IViewsCollection : IEnumerable<IView>, INotifyCollectionChanged
{
    /// <summary>
    /// 获取集合中的视图数量。
    /// </summary>
    int Count { get; }

    /// <summary>
    /// 判断集合是否包含指定视图。
    /// </summary>
    /// <param name="value">要查找的视图。</param>
    /// <returns>包含该视图时返回 <see langword="true"/>。</returns>
    bool Contains(IView value);
}
