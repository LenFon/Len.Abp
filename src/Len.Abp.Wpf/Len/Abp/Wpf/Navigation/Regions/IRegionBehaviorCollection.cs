namespace Len.Abp.Wpf.Navigation.Regions;

public interface IRegionBehaviorCollection : IEnumerable<KeyValuePair<string, IRegionBehavior>>
{
    IRegionBehavior this[string key] { get; }

    void Add(string key, IRegionBehavior regionBehavior);

    bool ContainsKey(string key);
}