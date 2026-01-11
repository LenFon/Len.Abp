using Microsoft.Extensions.Primitives;

namespace Len.Abp.Wpf;

/// <summary>
/// Provides methods for parsing and manipulating query strings.
/// https://github.com/dotnet/aspnetcore/blob/main/src/Http/WebUtilities/src/QueryHelpers.cs
/// </summary>
internal static class QueryHelpers
{
    /// <summary>
    /// Parse a query string into its component key and value parts.
    /// </summary>
    /// <param name="queryString">The raw query string value, with or without the leading '?'.</param>
    /// <returns>A collection of parsed keys and values, null if there are no entries.</returns>
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