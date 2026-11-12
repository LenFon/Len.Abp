namespace System.Windows;

/// <summary>
/// 使用 URI 字符串指定资源字典来源的 <see cref="ResourceDictionary"/>。
/// </summary>
public class UriResourceDictionary : ResourceDictionary
{
    /// <summary>
    /// 使用绝对 URI 初始化资源字典。
    /// </summary>
    /// <param name="uri">资源字典的 URI。</param>
    public UriResourceDictionary(string uri) : this(uri, UriKind.Absolute)
    {
    }

    /// <summary>
    /// 使用指定类型的 URI 初始化资源字典。
    /// </summary>
    /// <param name="uri">资源字典的 URI。</param>
    /// <param name="uriKind">URI 的类型。</param>
    public UriResourceDictionary(string uri, UriKind uriKind)
    {
        Source = new Uri(uri, uriKind);
    }
}
