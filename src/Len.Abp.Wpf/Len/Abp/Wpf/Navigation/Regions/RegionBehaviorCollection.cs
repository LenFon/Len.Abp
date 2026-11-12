using System.Collections;

namespace Len.Abp.Wpf.Navigation.Regions;

/// <summary>
/// <see cref="IRegionBehaviorCollection"/> 的默认实现，添加行为时自动附加到所属区域。
/// </summary>
internal class RegionBehaviorCollection(IRegion region) : IRegionBehaviorCollection
{
    private readonly Dictionary<string, IRegionBehavior> _behaviors = [];

    /// <inheritdoc/>
    public IRegionBehavior this[string key] => _behaviors[key];

    /// <inheritdoc/>
    public void Add(string key, IRegionBehavior regionBehavior)
    {
        ArgumentNullException.ThrowIfNull(key, nameof(key));
        ArgumentNullException.ThrowIfNull(regionBehavior, nameof(regionBehavior));

        if (!_behaviors.TryAdd(key, regionBehavior))
        {
            throw new ArgumentException("无法添加使用相同键的重复行为。", nameof(key));
        }

        regionBehavior.Region = region;
        regionBehavior.Attach();
    }

    /// <inheritdoc/>
    public bool ContainsKey(string key) => _behaviors.ContainsKey(key);

    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<string, IRegionBehavior>> GetEnumerator() => _behaviors.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _behaviors.GetEnumerator();
}
