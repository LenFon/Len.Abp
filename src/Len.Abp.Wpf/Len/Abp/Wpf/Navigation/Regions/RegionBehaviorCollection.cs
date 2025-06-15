using System.Collections;

namespace Len.Abp.Wpf.Navigation.Regions;

internal class RegionBehaviorCollection(IRegion region) : IRegionBehaviorCollection
{
    private readonly Dictionary<string, IRegionBehavior> _behaviors = [];
    private readonly IRegion _region = region;

    public IRegionBehavior this[string key] => _behaviors[key];

    public void Add(string key, IRegionBehavior regionBehavior)
    {
        ArgumentNullException.ThrowIfNull(key, nameof(key));
        ArgumentNullException.ThrowIfNull(regionBehavior, nameof(regionBehavior));

        if (_behaviors.ContainsKey(key))
        {
            throw new ArgumentException("Could not add duplicate behavior with same key.", nameof(key));
        }

        _behaviors.Add(key, regionBehavior);
        regionBehavior.Region = _region;
        regionBehavior.Attach();
    }

    public bool ContainsKey(string key) => _behaviors.ContainsKey(key);

    public IEnumerator<KeyValuePair<string, IRegionBehavior>> GetEnumerator() => _behaviors.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _behaviors.GetEnumerator();
}