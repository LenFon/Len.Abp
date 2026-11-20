namespace Len.Abp.Wpf;

/// <summary>
/// 可从 URI 查询字符串中提供名称/值对的可枚举对象。
/// </summary>
/// <param name="queryString">查询字符串。</param>
internal readonly struct QueryStringEnumerable(ReadOnlyMemory<char> queryString)
{
    private readonly ReadOnlyMemory<char> _queryString = queryString;

    /// <summary>
    /// 使用字符串初始化查询字符串枚举对象。
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

        /// <summary>
        /// 解码百分号转义。
        /// </summary>
        /// <param name="chars">编码后的字符。</param>
        /// <returns>解码后的字符。</returns>
        /// <remarks>
        /// <para>
        /// <b>不把 <c>+</c> 解码成空格。</b>那是 HTML 表单（<c>application/x-www-form-urlencoded</c>）
        /// 的规则，导航 URI 是普通 URI：<c>?token=abc+def</c> 里的 <c>+</c> 是字面字符，
        /// 按表单规则解码会把 base64 令牌之类的值静默改坏。
        /// </para>
        /// <para>
        /// 需要空格时写 <c>%20</c>，与 RFC 3986 一致。
        /// </para>
        /// </remarks>
        private static ReadOnlyMemory<char> Decode(ReadOnlyMemory<char> chars)
        {
            ReadOnlySpan<char> source = chars.Span;
            if (!source.Contains('%'))
            {
                return chars;
            }

            var buffer = new char[source.Length];
            source.CopyTo(buffer);
            if (!Uri.TryUnescapeDataString(buffer, buffer, out var unescapedLength))
            {
                // 只有目标缓冲区不足时才会失败；这里缓冲区与源等长、还原只会缩短，
                // 因此该分支实际不可达，保留原始文本作为兜底。
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