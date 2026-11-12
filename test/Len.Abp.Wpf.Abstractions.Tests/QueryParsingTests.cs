using Len.Abp.Wpf;
using Xunit;

namespace Len.Abp.Wpf.Abstractions.Tests;

/// <summary>
/// 覆盖 <see cref="QueryHelpers"/> 与 <see cref="QueryStringEnumerable"/> 的解析细节。
/// </summary>
public class QueryParsingTests
{
    /// <summary>
    /// 空查询字符串返回 null。
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("?")]
    public void ParseNullableQuery_EmptyInput_ReturnsNull(string? query)
    {
        Assert.Null(QueryHelpers.ParseNullableQuery(query));
    }

    /// <summary>
    /// 单值查询解析为单个条目。
    /// </summary>
    [Fact]
    public void ParseNullableQuery_SinglePair_ReturnsOneEntry()
    {
        var result = QueryHelpers.ParseNullableQuery("?a=1");

        Assert.NotNull(result);
        Assert.Equal("1", result["a"].ToString());
    }

    /// <summary>
    /// 同名键的多个值按顺序累积。
    /// </summary>
    [Fact]
    public void ParseNullableQuery_DuplicateKeys_AccumulatesValues()
    {
        var result = QueryHelpers.ParseNullableQuery("?a=1&a=2&a=3&a=4");

        Assert.NotNull(result);
        Assert.Equal(["1", "2", "3", "4"], [.. result["a"].Select(v => v!)]);
    }

    /// <summary>
    /// 键名大小写不敏感。
    /// </summary>
    [Fact]
    public void ParseNullableQuery_KeyIsCaseInsensitive()
    {
        var result = QueryHelpers.ParseNullableQuery("?Name=abc");

        Assert.NotNull(result);
        Assert.Equal("abc", result["name"].ToString());
    }

    /// <summary>
    /// 没有等号的分段被当作只有名称的条目。
    /// </summary>
    [Fact]
    public void ParseNullableQuery_SegmentWithoutEquals_TreatedAsNameOnly()
    {
        var result = QueryHelpers.ParseNullableQuery("?flag");

        Assert.NotNull(result);
        Assert.Equal(string.Empty, result["flag"].ToString());
    }

    /// <summary>
    /// 空分段被跳过。
    /// </summary>
    [Fact]
    public void ParseNullableQuery_EmptySegments_AreSkipped()
    {
        var result = QueryHelpers.ParseNullableQuery("?a=1&&b=2&");

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }

    /// <summary>
    /// 加号被解码为空格，百分号编码被解码。
    /// </summary>
    [Fact]
    public void ParseNullableQuery_DecodesPlusAndPercentEncoding()
    {
        var result = QueryHelpers.ParseNullableQuery("?name=hello+world&path=a%2Fb");

        Assert.NotNull(result);
        Assert.Equal("hello world", result["name"].ToString());
        Assert.Equal("a/b", result["path"].ToString());
    }

    /// <summary>
    /// 畸形转义序列无法解码时保留原始文本，而不是静默变成空字符串。
    /// </summary>
    [Fact]
    public void ParseNullableQuery_MalformedEscape_KeepsOriginalText()
    {
        var result = QueryHelpers.ParseNullableQuery("?path=a%ZZb");

        Assert.NotNull(result);
        Assert.Equal("a%ZZb", result["path"].ToString());
    }

    /// <summary>
    /// 枚举器在空查询上立即结束。
    /// </summary>
    [Fact]
    public void Enumerator_EmptyQuery_ReturnsFalseImmediately()
    {
        var enumerator = new QueryStringEnumerable((string?)null).GetEnumerator();

        Assert.False(enumerator.MoveNext());
        Assert.Equal(default, enumerator.Current);
    }

    /// <summary>
    /// 枚举器跳过前导问号。
    /// </summary>
    [Fact]
    public void Enumerator_SkipsLeadingQuestionMark()
    {
        var enumerator = new QueryStringEnumerable("?a=1").GetEnumerator();

        Assert.True(enumerator.MoveNext());
        Assert.Equal("a", enumerator.Current.EncodedName.ToString());
        Assert.Equal("1", enumerator.Current.EncodedValue.ToString());
    }

    /// <summary>
    /// 枚举器逐个返回所有键值对。
    /// </summary>
    [Fact]
    public void Enumerator_ReturnsAllPairs()
    {
        var enumerator = new QueryStringEnumerable("a=1&b=2&c=3").GetEnumerator();
        var names = new List<string>();

        while (enumerator.MoveNext())
        {
            names.Add(enumerator.Current.EncodedName.ToString());
        }

        Assert.Equal(["a", "b", "c"], names);
    }

    /// <summary>
    /// 解码名称与值时可处理不含编码字符的输入。
    /// </summary>
    [Fact]
    public void EncodedNameValuePair_DecodeWithoutEncoding_ReturnsOriginal()
    {
        var enumerator = new QueryStringEnumerable("name=value").GetEnumerator();
        Assert.True(enumerator.MoveNext());

        Assert.Equal("name", enumerator.Current.DecodeName().ToString());
        Assert.Equal("value", enumerator.Current.DecodeValue().ToString());
    }

    /// <summary>
    /// 通过内存构造的枚举器与字符串构造行为一致。
    /// </summary>
    [Fact]
    public void Enumerator_FromMemory_BehavesLikeStringConstructor()
    {
        var enumerator = new QueryStringEnumerable("?a=1".AsMemory()).GetEnumerator();

        Assert.True(enumerator.MoveNext());
        Assert.Equal("a", enumerator.Current.EncodedName.ToString());
    }
}
