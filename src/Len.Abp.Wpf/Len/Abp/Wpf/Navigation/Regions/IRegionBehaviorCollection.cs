namespace Len.Abp.Wpf.Navigation.Regions;

/// <summary>
/// 区域行为集合，按字符串键保存行为，添加时自动附加到所属区域。
/// </summary>
public interface IRegionBehaviorCollection : IEnumerable<KeyValuePair<string, IRegionBehavior>>
{
    /// <summary>
    /// 获取指定键的行为。
    /// </summary>
    /// <param name="key">行为键。</param>
    /// <returns>该键对应的行为。</returns>
    IRegionBehavior this[string key] { get; }

    /// <summary>
    /// 添加行为并立即附加到区域。
    /// </summary>
    /// <param name="key">行为键。</param>
    /// <param name="regionBehavior">要添加的行为。</param>
    /// <exception cref="ArgumentException">已存在相同键的行为时抛出。</exception>
    void Add(string key, IRegionBehavior regionBehavior);

    /// <summary>
    /// 判断是否存在指定键的行为。
    /// </summary>
    /// <param name="key">行为键。</param>
    /// <returns>存在该键时返回 <see langword="true"/>。</returns>
    bool ContainsKey(string key);
}
