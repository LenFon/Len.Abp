using System.Collections;
using System.Globalization;
using System.Reflection;

namespace Len.Abp.Wpf;

public interface IParameters : IEnumerable<KeyValuePair<string, object>>
{
    int Count { get; }

    IEnumerable<string> Keys { get; }

    object? this[string key] { get; }

    static IParameters Create(string? query = null) => new Parameters(query);

    IParameters Add(string key, object value);

    bool ContainsKey(string key);

    /// <summary>
    /// 获取指定键的值并转换为 <typeparamref name="T"/>。
    /// 当键不存在或值无法转换时返回 <see langword="default"/>。
    /// 如需区分这两种情况，请使用 <see cref="TryGetValue{T}"/>。
    /// </summary>
    T? GetValue<T>(string key);

    /// <summary>
    /// 获取指定键的所有值并转换为 <typeparamref name="T"/>。
    /// 无法转换的值会被跳过。
    /// </summary>
    IEnumerable<T?> GetValues<T>(string key);

    /// <summary>
    /// 尝试获取指定键的值并转换为 <typeparamref name="T"/>。
    /// </summary>
    /// <returns>
    /// 键存在且值可以转换时返回 <see langword="true"/>，否则返回 <see langword="false"/>。
    /// </returns>
    bool TryGetValue<T>(string key, out T? value);
}

/// <summary>
/// <see cref="IParameters"/> 的默认实现，支持从查询字符串解析并按键存取。
/// </summary>
/// <remarks>
/// 值以 <see cref="object"/> 存储，因此写入值类型时必然发生一次装箱，这是异构参数包的固有代价。
/// 读取路径则通过泛型转换避免额外的装箱：类型匹配时直接拆箱，字符串解析走按类型缓存的委托。
/// </remarks>
file class Parameters : IParameters
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
            if (IsKey(entry.Key, key) && TryConvert<T>(entry.Value, out var value))
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
                return TryConvert<T>(entry.Value, out value);
            }
        }

        value = default;
        return false;
    }

    private static bool IsKey(string candidate, string key) => string.Equals(candidate, key, StringComparison.Ordinal);

    /// <summary>
    /// 把存储的原始值转换为 <typeparamref name="T"/>，尽可能避免装箱。
    /// </summary>
    private static bool TryConvert<T>(object? raw, out T? value)
    {
        if (raw is null)
        {
            value = default;
            return !typeof(T).IsValueType || Nullable.GetUnderlyingType(typeof(T)) is not null;
        }

        // 快路径：类型已匹配。存储本身是 object，这里只发生一次拆箱。
        if (raw is T typed)
        {
            value = typed;
            return true;
        }

        // 字符串解析：使用按类型缓存的委托，不产生装箱。
        if (raw is string text && StringParser<T>.Instance is { } parse)
        {
            return parse(text, out value);
        }

        return TryConvertFallback(raw, out value);
    }

    /// <summary>
    /// 回退路径：处理跨数值类型、数值转枚举等少见场景，允许发生装箱。
    /// </summary>
    private static bool TryConvertFallback<T>(object raw, out T? value)
    {
        var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

        if (targetType.IsEnum && raw is IConvertible)
        {
            try
            {
                value = (T?)Enum.ToObject(targetType, raw);
                return true;
            }
            catch (ArgumentException)
            {
                value = default;
                return false;
            }
        }

        if (!typeof(IConvertible).IsAssignableFrom(targetType))
        {
            value = default;
            return false;
        }

        try
        {
            value = (T?)Convert.ChangeType(raw, targetType, CultureInfo.InvariantCulture);
            return true;
        }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException)
        {
            value = default;
            return false;
        }
    }

    /// <summary>
    /// 把字符串解析为 <typeparamref name="T"/> 的委托。
    /// </summary>
    private delegate bool TryParseDelegate<T>(string text, out T value);

    /// <summary>
    /// 按类型缓存字符串解析委托。委托在首次使用时通过反射构造一次，
    /// 之后每次调用都是直接调用，不产生装箱。
    /// </summary>
    private static class StringParser<T>
    {
        public static readonly TryParseDelegate<T>? Instance = Create();

        private static TryParseDelegate<T>? Create()
        {
            var underlyingType = Nullable.GetUnderlyingType(typeof(T));

            // 可空值类型：复用底层类型的解析逻辑，直接产出 T（即 TValue?），无需中间装箱。
            if (underlyingType is not null)
            {
                if (underlyingType.IsEnum)
                {
                    return CreateDelegate(nameof(ParseNullableEnum), underlyingType);
                }

                return ImplementsParsable(underlyingType)
                    ? CreateDelegate(nameof(ParseNullable), underlyingType)
                    : null;
            }

            if (typeof(T).IsEnum)
            {
                return CreateDelegate(nameof(ParseEnum), typeof(T));
            }

            return ImplementsParsable(typeof(T)) ? CreateDelegate(nameof(Parse), typeof(T)) : null;
        }

        // IParsable<TSelf> 带 TSelf 自引用约束，无法用 MakeGenericType 探测不满足约束的类型，
        // 因此改为检查类型是否实现了该接口。
        private static bool ImplementsParsable(Type type) =>
            type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IParsable<>));

        private static TryParseDelegate<T> CreateDelegate(string methodName, Type type) =>
            (TryParseDelegate<T>)typeof(StringParser<T>)
                .GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(type)
                .CreateDelegate<TryParseDelegate<T>>();

        private static bool Parse<TValue>(string text, out TValue value) where TValue : IParsable<TValue> =>
            TValue.TryParse(text, CultureInfo.InvariantCulture, out value!);

        private static bool ParseNullable<TValue>(string text, out TValue? value) where TValue : struct, IParsable<TValue>
        {
            if (TValue.TryParse(text, CultureInfo.InvariantCulture, out var parsed))
            {
                value = parsed;
                return true;
            }

            value = null;
            return false;
        }

        private static bool ParseEnum<TEnum>(string text, out TEnum value) where TEnum : struct, Enum =>
            Enum.TryParse(text, ignoreCase: true, out value);

        private static bool ParseNullableEnum<TEnum>(string text, out TEnum? value) where TEnum : struct, Enum
        {
            if (Enum.TryParse(text, ignoreCase: true, out TEnum parsed))
            {
                value = parsed;
                return true;
            }

            value = null;
            return false;
        }
    }
}
