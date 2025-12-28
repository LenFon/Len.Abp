namespace Len.Abp.Wpf.Navigation.Regions;

public sealed class RegionOptions
{
    private readonly List<string> _behaviorKeys = [];
    public IEnumerable<string> BehaviorKeys => _behaviorKeys;

    internal void AddBehaviorKey(string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key, nameof(key));

        if (_behaviorKeys.Contains(key))
        {
            throw new InvalidOperationException($"Behavior key '{key}' already exists.");
        }

        _behaviorKeys.Add(key);
    }
}