using System.Collections;

namespace Len.Abp.Wpf;

/// <summary>
/// <see cref="IParameters"/> 的默认实现，支持从查询字符串解析并按键存取。
/// </summary>
/// <remarks>
/// 值以 <see cref="object"/> 存储，因此写入值类型时必然发生一次装箱，这是异构参数包的固有代价。
/// 读取路径则通过泛型转换避免额外的装箱：类型匹配时直接拆箱，字符串解析走按类型缓存的委托。
/// <para>
/// <b>键比较不区分大小写</b>（<see cref="StringComparison.OrdinalIgnoreCase"/>），
/// 与查询串解析阶段一致：<c>?Name=a&amp;name=b</c> 会合并成同一个键的多值，
/// 合并后的键名取首次出现的写法，之后用 <c>"name"</c> 或 <c>"Name"</c> 都能读到。
/// </para>
/// <para>
/// 导航参数校验器同样按不敏感匹配，两处口径一致才能保证"校验通过"意味着"读得到"。
/// 早期版本读取是 <see cref="StringComparison.Ordinal"/>，而校验是不敏感的，
/// 于是 <c>?Id=42</c> 配必需参数 <c>["id:int"]</c> 会校验通过、随后
/// <c>GetRequired&lt;int&gt;("id")</c> 抛"参数不存在"。
/// </para>
/// </remarks>
internal sealed class Parameters : IParameters
{
    private readonly List<KeyValuePair<string, object>> _entries = [];
    private string[]? _keys;

    /// <summary>
    /// 使用查询字符串初始化参数集合。
    /// </summary>
    /// <param name="query">查询字符串，可带或不带前导的 '?'。</param>
    public Parameters(string? query = null)
    {
        var queries = QueryHelpers.ParseNullableQuery(query);

        if (queries is null)
            return;

        foreach (var item in queries)
        {
            foreach (var value in item.Value)
            {
                Add(item.Key, value!);
            }
        }
    }

    /// <inheritdoc/>
    public int Count => _entries.Count;

    /// <inheritdoc/>
    public IEnumerable<string> Keys => _keys ??= [.. _entries.Select(s => s.Key)];

    /// <inheritdoc/>
    public object? this[string key]
    {
        get
        {
            ArgumentNullException.ThrowIfNull(key);

            foreach (var entry in _entries)
            {
                if (IsKey(entry.Key, key))
                {
                    return entry.Value;
                }
            }

            return null;
        }
    }

    /// <inheritdoc/>
    public IParameters Add(string key, object value)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);

        _entries.Add(new(key, value));
        _keys = null;

        return this;
    }

    /// <inheritdoc/>
    public bool ContainsKey(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        foreach (var entry in _entries)
        {
            if (IsKey(entry.Key, key))
            {
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => _entries.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _entries.GetEnumerator();

    /// <inheritdoc/>
    public T? GetValue<T>(string key) => TryGetValue<T>(key, out var value) ? value : default;

    /// <inheritdoc/>
    public IEnumerable<T?> GetValues<T>(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        // 返回数组而非迭代器：调用方通常只取一次结果，数组避免迭代器状态机分配。
        List<T?>? values = null;

        foreach (var entry in _entries)
        {
            if (IsKey(entry.Key, key) && ParameterConversion.TryConvert<T>(entry.Value, out var value))
            {
                (values ??= []).Add(value);
            }
        }

        return values ?? (IEnumerable<T?>)(T?[])[];
    }

    /// <inheritdoc/>
    public bool TryGetValue<T>(string key, out T? value)
    {
        ArgumentNullException.ThrowIfNull(key);

        foreach (var entry in _entries)
        {
            if (IsKey(entry.Key, key))
            {
                return ParameterConversion.TryConvert<T>(entry.Value, out value);
            }
        }

        value = default;
        return false;
    }

    /// <summary>
    /// 判断两个参数键是否匹配。
    /// </summary>
    /// <remarks>
    /// 大小写不敏感：查询串解析阶段本就按不敏感合并重复键，
    /// 读取若改成敏感就会出现"校验通过但读不到"的矛盾。
    /// 导航参数校验器使用同一套比较规则。
    /// </remarks>
    private static bool IsKey(string candidate, string key) =>
        string.Equals(candidate, key, StringComparison.OrdinalIgnoreCase);
}
