using System.Collections;

namespace Len.Abp.Wpf;

/// <summary>
/// 导航参数集合，按键存取异构值，并支持从查询字符串解析。
/// </summary>
public interface IParameters : IEnumerable<KeyValuePair<string, object>>
{
    /// <summary>
    /// 获取参数项的数量。
    /// </summary>
    int Count { get; }

    /// <summary>
    /// 获取所有参数的键。
    /// </summary>
    IEnumerable<string> Keys { get; }

    /// <summary>
    /// 获取指定键的第一个值；键不存在时返回 <see langword="null"/>。
    /// </summary>
    /// <param name="key">参数键。</param>
    /// <returns>该键对应的值。</returns>
    object? this[string key] { get; }

    /// <summary>
    /// 创建参数集合。
    /// </summary>
    /// <param name="query">可选的查询字符串，可带或不带前导的 '?'。</param>
    /// <returns>新的参数集合。</returns>
    static IParameters Create(string? query = null) => new Parameters(query);

    /// <summary>
    /// 添加一个参数项。
    /// </summary>
    /// <param name="key">参数键。</param>
    /// <param name="value">参数值。</param>
    /// <returns>当前集合，便于链式调用。</returns>
    IParameters Add(string key, object value);

    /// <summary>
    /// 判断是否存在指定键。
    /// </summary>
    /// <param name="key">参数键。</param>
    /// <returns>存在该键时返回 <see langword="true"/>。</returns>
    bool ContainsKey(string key);

    /// <summary>
    /// 获取指定键的值并转换为 <typeparamref name="T"/>。
    /// 当键不存在或值无法转换时返回 <see langword="default"/>。
    /// 如需区分这两种情况，请使用 <see cref="TryGetValue{T}"/>。
    /// </summary>
    T? GetValue<T>(string key);

    /// <summary>
    /// 获取指定键的必需值并转换为 <typeparamref name="T"/>。
    /// </summary>
    /// <typeparam name="T">目标类型。</typeparam>
    /// <param name="key">参数键。</param>
    /// <returns>转换后的值。</returns>
    /// <exception cref="InvalidOperationException">
    /// 键不存在，或值无法转换为 <typeparamref name="T"/> 时抛出。
    /// </exception>
    /// <remarks>
    /// 与 <see cref="GetValue{T}"/> 的区别：缺失或类型不匹配时立即失败，
    /// 而不是静默返回 <see langword="default"/>，避免"键名拼错"被当成"值为默认值"。
    /// </remarks>
    T GetRequired<T>(string key)
    {
        if (!TryGetValue<T>(key, out var value))
        {
            var available = string.Join(", ", Keys);
            var reason = ContainsKey(key) ? "值无法转换为目标类型" : "参数不存在";

            throw new InvalidOperationException(
                $"无法获取必需参数 \"{key}\"：{reason}。可用参数：{(string.IsNullOrEmpty(available) ? "（无）" : available)}。"
            );
        }

        return value!;
    }

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

