using Microsoft.Extensions.Primitives;

namespace Len.Abp.Wpf;

/// <summary>
/// 把查询字符串的重复键累积成多值集合，供 <see cref="QueryHelpers.ParseNullableQuery"/> 使用。
/// </summary>
/// <remarks>
/// <para>
/// 前两个值存在 <see cref="StringValues"/> 里，第三个起改用扩展列表，
/// 避免常见的一两个值也分配列表。
/// </para>
/// <para>
/// 键比较用 <see cref="StringComparer.OrdinalIgnoreCase"/>：查询串里的
/// <c>?Name=a&amp;name=b</c> 视为同一个键的多值。注意这与
/// <see cref="IParameters"/> 的读取语义不同——后者按键名精确匹配，
/// 因此 <c>?Name=a</c> 之后用 <c>"name"</c> 读不到。
/// </para>
/// </remarks>
internal struct KeyValueAccumulator
{
    private Dictionary<string, StringValues> _accumulator;
    private Dictionary<string, List<string>> _expandingAccumulator;

    /// <summary>
    /// 获取是否已累积任何值。
    /// </summary>
    public readonly bool HasValues => ValueCount > 0;

    /// <summary>
    /// 获取已累积的值的总数。
    /// </summary>
    public int ValueCount { get; private set; }

    /// <summary>
    /// 追加一个键值对。
    /// </summary>
    /// <param name="key">参数键。</param>
    /// <param name="value">参数值。</param>
    public void Append(string key, string value)
    {
        _accumulator ??= [with(StringComparer.OrdinalIgnoreCase)];

        if (_accumulator.TryGetValue(key, out var values))
        {
            if (values.Count == 0)
            {
                // 该键的标记条目，表示已存在于扩展列表字典中
                _expandingAccumulator[key].Add(value);
            }
            else if (values.Count == 1)
            {
                // 该键的第二个值
                _accumulator[key] = new string[] { values[0]!, value };
            }
            else
            {
                // 该键的第三个值
                // 添加计数为零的条目，并把数据移到扩展列表字典
                _accumulator[key] = default;

                _expandingAccumulator ??= [with(StringComparer.OrdinalIgnoreCase)];

                // 已有 3 个条目，因此初始容量分配为 8，后续依靠 List 自身的扩容机制
                var list = new List<string>(8);
                var array = values.ToArray();

                list.Add(array[0]!);
                list.Add(array[1]!);
                list.Add(value);

                _expandingAccumulator[key] = list;
            }
        }
        else
        {
            // 该键的第一个值
            _accumulator[key] = new StringValues(value);
        }

        ValueCount++;
    }

    /// <summary>
    /// 把扩展列表中的多值条目合并回主字典并返回结果。
    /// </summary>
    /// <returns>键到多值集合的映射。</returns>
    public readonly Dictionary<string, StringValues> GetResults()
    {
        if (_expandingAccumulator != null)
        {
            // 将计数为 3 及以上的多值条目合并回 _accumulator 字典
            foreach (var entry in _expandingAccumulator)
            {
                _accumulator[entry.Key] = new StringValues([.. entry.Value]);
            }
        }

        return _accumulator ?? [with(StringComparer.OrdinalIgnoreCase)];
    }
}
