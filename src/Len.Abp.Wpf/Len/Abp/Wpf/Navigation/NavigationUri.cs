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
    /// 获取视图名称，即去掉前导斜杠并还原转义的绝对路径。
    /// </summary>
    /// <param name="uri">导航目标 URI。</param>
    /// <returns>视图名称。</returns>
    /// <remarks>
    /// <para>
    /// <see cref="Uri.AbsolutePath"/> 返回的是<b>转义后</b>的形式：中文会变成
    /// <c>%E9%A1%B5%E9%9D%A2</c>，空格会变成 <c>%20</c>。而视图名是注册时写入的
    /// 原始字符串（<c>ExposeKeyedService</c> 的键），两者必须一致才能解析到视图，
    /// 因此这里必须还原转义。
    /// </para>
    /// <para>
    /// 没有 <c>%</c> 时直接返回，避免为常见路径分配缓冲区。
    /// 用 <see cref="Uri.TryUnescapeDataString(ReadOnlySpan{char}, Span{char}, out int)"/>
    /// 而不是 <see cref="Uri.UnescapeDataString(string)"/>：前者写入调用方提供的缓冲区，
    /// 不额外分配字符串。两者对畸形转义序列都退化为原文本、不抛异常——
    /// 导航失败时使用者看到的是自己写的视图名，不是异常堆栈。
    /// </para>
    /// <para>
    /// <b>已知边界</b>：视图名里字面出现合法的转义序列（如 <c>a%2Fb</c>）会被还原成
    /// <c>a/b</c>。这类名称无法与"路径分隔符"区分，属于 <see cref="Uri"/> 的固有歧义，
    /// 不建议在视图名中使用 <c>%</c>。
    /// </para>
    /// </remarks>
    public static string GetViewName(Uri uri)
    {
        var path = EnsureAbsolute(uri).AbsolutePath.TrimStart('/');

        if (!path.Contains('%'))
        {
            return path;
        }

        // 还原只会缩短，输入长度足够容纳结果。
        var buffer = new char[path.Length];

        return Uri.TryUnescapeDataString(path, buffer, out var length)
            ? new string(buffer, 0, length)
            : path;
    }

    /// <summary>
    /// 获取 <paramref name="uri"/> 的查询字符串，包含前导的 <c>?</c>。
    /// </summary>
    public static string GetQuery(Uri uri) => EnsureAbsolute(uri).Query;
}
