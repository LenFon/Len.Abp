namespace Len.Abp.Wpf.Navigation.Regions;

/// <summary>
/// 区域相关配置，保存需要自动附加到每个区域的行为键。
/// </summary>
public sealed class RegionOptions
{
    private readonly List<string> _behaviorKeys = [];

    /// <summary>
    /// 获取需要自动附加到每个区域的行为键集合。
    /// </summary>
    public IEnumerable<string> BehaviorKeys => _behaviorKeys;

    /// <summary>
    /// 添加一个行为键。
    /// </summary>
    /// <param name="key">行为键。</param>
    /// <exception cref="InvalidOperationException">该行为键已存在时抛出。</exception>
    internal void AddBehaviorKey(string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key, nameof(key));

        if (_behaviorKeys.Contains(key))
        {
            throw new InvalidOperationException($"行为键 {key} 已存在。");
        }

        _behaviorKeys.Add(key);
    }
}
