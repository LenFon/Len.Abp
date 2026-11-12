using Microsoft.Extensions.Primitives;

namespace Len.Abp.Wpf;

/// <summary>
/// 提供解析和操作查询字符串的方法。
/// https://github.com/dotnet/aspnetcore/blob/main/src/Http/WebUtilities/src/QueryHelpers.cs
/// </summary>
internal static class QueryHelpers
{
    /// <summary>
    /// 将查询字符串解析为键和值两部分。
    /// </summary>
    /// <param name="queryString">原始查询字符串，可带或不带前导的 '?'。</param>
    /// <returns>解析后的键值集合；没有任何条目时返回 null。</returns>
    public static Dictionary<string, StringValues>? ParseNullableQuery(string? queryString)
    {
        var accumulator = new KeyValueAccumulator();
        var enumerable = new QueryStringEnumerable(queryString);

        foreach (var pair in enumerable)
        {
            accumulator.Append(pair.DecodeName().ToString(), pair.DecodeValue().ToString());
        }

        if (!accumulator.HasValues)
        {
            return null;
        }

        return accumulator.GetResults();
    }
}