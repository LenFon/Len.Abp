namespace Len.Abp.Wpf.Navigation;

/// <summary>
/// 将导航使用的相对视图 URI 归一化为绝对 URI。
/// </summary>
internal static class NavigationUri
{
    private const string BaseAddress = "http://localhost";

    /// <summary>
    /// 将 <paramref name="uri"/> 转换为绝对 URI，相对路径视为以 <c>http://localhost</c> 为根。
    /// </summary>
    public static Uri EnsureAbsolute(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);

        if (uri.IsAbsoluteUri)
        {
            return uri;
        }

        return uri.OriginalString.StartsWith('/')
            ? new Uri(BaseAddress + uri, UriKind.Absolute)
            : new Uri(BaseAddress + "/" + uri, UriKind.Absolute);
    }

    /// <summary>
    /// 获取视图名称，即去掉前导斜杠的绝对路径。
    /// </summary>
    public static string GetViewName(Uri uri) => EnsureAbsolute(uri).AbsolutePath.TrimStart('/');

    /// <summary>
    /// 获取 <paramref name="uri"/> 的查询字符串，包含前导的 <c>?</c>。
    /// </summary>
    public static string GetQuery(Uri uri) => EnsureAbsolute(uri).Query;
}
