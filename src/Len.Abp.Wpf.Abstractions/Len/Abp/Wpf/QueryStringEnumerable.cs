namespace Len.Abp.Wpf;

/// <summary>
/// 可从 URI 查询字符串中提供名称/值对的可枚举对象。
/// </summary>
/// <remarks>
/// 构造 <see cref="QueryStringEnumerable"/> 的实例。
/// </remarks>
/// <param name="queryString">查询字符串。</param>
internal readonly struct QueryStringEnumerable(ReadOnlyMemory<char> queryString)
{
    private readonly ReadOnlyMemory<char> _queryString = queryString;

    /// <summary>
    /// 构造 <see cref="QueryStringEnumerable"/> 的实例。
    /// </summary>
    /// <param name="queryString">查询字符串。</param>
    public QueryStringEnumerable(string? queryString)
        : this(queryString.AsMemory()) { }

    /// <summary>
    /// 获取一个可遍历查询字符串中名称/值对的对象。
    /// </summary>
    /// <returns>可遍历查询字符串中名称/值对的对象。</returns>
    public Enumerator GetEnumerator() => new(_queryString);

    /// <summary>
    /// 表示枚举过程中从查询字符串提取的单个名称/值对。
    /// </summary>
    public readonly struct EncodedNameValuePair
    {
        /// <summary>
        /// 初始化名称/值对。
        /// </summary>
        /// <param name="encodedName">编码后的名称。</param>
        /// <param name="encodedValue">编码后的值。</param>
        internal EncodedNameValuePair(ReadOnlyMemory<char> encodedName, ReadOnlyMemory<char> encodedValue)
        {
            EncodedName = encodedName;
            EncodedValue = encodedValue;
        }

        /// <summary>
        /// 以原始编码形式获取该名称/值对中的名称。
        /// 如需获取解码后的字符串，请调用 <see cref="DecodeName"/>。
        /// </summary>
        public readonly ReadOnlyMemory<char> EncodedName { get; }

        /// <summary>
        /// 以原始编码形式获取该名称/值对中的值。
        /// 如需获取解码后的字符串，请调用 <see cref="DecodeValue"/>。
        /// </summary>
        public readonly ReadOnlyMemory<char> EncodedValue { get; }

        /// <summary>
        /// 解码该名称/值对中的名称。
        /// </summary>
        /// <returns>表示解码后名称的字符。</returns>
        public ReadOnlyMemory<char> DecodeName() => Decode(EncodedName);

        /// <summary>
        /// 解码该名称/值对中的值。
        /// </summary>
        /// <returns>表示解码后值的字符。</returns>
        public ReadOnlyMemory<char> DecodeValue() => Decode(EncodedValue);

        private static ReadOnlyMemory<char> Decode(ReadOnlyMemory<char> chars)
        {
            ReadOnlySpan<char> source = chars.Span;
            if (!source.ContainsAny('%', '+'))
            {
                return chars;
            }
            var buffer = new char[source.Length];
            source.Replace(buffer, '+', ' ');
            if (!Uri.TryUnescapeDataString(buffer, buffer, out var unescapedLength))
            {
                // 畸形转义序列无法解码时保留原始文本，避免静默变成空字符串。
                return chars;
            }

            return buffer.AsMemory(0, unescapedLength);
        }
    }

    /// <summary>
    /// 从 URI 查询字符串中提供名称/值对的枚举器。
    /// </summary>
    public struct Enumerator
    {
        private ReadOnlyMemory<char> _query;

        /// <summary>
        /// 初始化枚举器。
        /// </summary>
        /// <param name="query">查询字符串。</param>
        internal Enumerator(ReadOnlyMemory<char> query)
        {
            Current = default;
            _query = query.IsEmpty || query.Span[0] != '?' ? query : query[1..];
        }

        /// <summary>
        /// 获取正在枚举的查询字符串中当前引用的键/值对。
        /// </summary>
        public EncodedNameValuePair Current { get; private set; }

        /// <summary>
        /// 移动到正在枚举的查询字符串中的下一个键/值对。
        /// </summary>
        /// <returns>如果还有下一个键/值对则返回 true，否则返回 false。</returns>
        public bool MoveNext()
        {
            while (!_query.IsEmpty)
            {
                // 截取下一段
                ReadOnlyMemory<char> segment;
                var delimiterIndex = _query.Span.IndexOf('&');
                if (delimiterIndex >= 0)
                {
                    segment = _query[..delimiterIndex];
                    _query = _query[(delimiterIndex + 1)..];
                }
                else
                {
                    segment = _query;
                    _query = default;
                }

                // 非空则输出
                var equalIndex = segment.Span.IndexOf('=');
                if (equalIndex >= 0)
                {
                    Current = new EncodedNameValuePair(segment[..equalIndex], segment[(equalIndex + 1)..]);
                    return true;
                }
                else if (!segment.IsEmpty)
                {
                    Current = new EncodedNameValuePair(segment, default);
                    return true;
                }
            }

            Current = default;
            return false;
        }
    }
}