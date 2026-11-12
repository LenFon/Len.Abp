using Microsoft.Extensions.Primitives;

namespace Len.Abp.Wpf;

/// <summary>
/// 此 API 仅用于支撑基础设施，不应在业务代码中直接使用。
/// 该 API 可能在未来版本中变更或移除。
/// </summary>
internal struct KeyValueAccumulator
{
    private Dictionary<string, StringValues> _accumulator;
    private Dictionary<string, List<string>> _expandingAccumulator;

    /// <summary>
    /// 此 API 仅用于支撑基础设施，不应在业务代码中直接使用。
    /// 该 API 可能在未来版本中变更或移除。
    /// </summary>
    public readonly bool HasValues => ValueCount > 0;

    /// <summary>
    /// 此 API 仅用于支撑基础设施，不应在业务代码中直接使用。
    /// 该 API 可能在未来版本中变更或移除。
    /// </summary>
    public int ValueCount { get; private set; }

    /// <summary>
    /// 此 API 仅用于支撑基础设施，不应在业务代码中直接使用。
    /// 该 API 可能在未来版本中变更或移除。
    /// </summary>
    public void Append(string key, string value)
    {
        _accumulator ??= new Dictionary<string, StringValues>(StringComparer.OrdinalIgnoreCase);

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

                _expandingAccumulator ??= new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

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
    /// 此 API 仅用于支撑基础设施，不应在业务代码中直接使用。
    /// 该 API 可能在未来版本中变更或移除。
    /// </summary>
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

        return _accumulator ?? new Dictionary<string, StringValues>(0, StringComparer.OrdinalIgnoreCase);
    }
}
