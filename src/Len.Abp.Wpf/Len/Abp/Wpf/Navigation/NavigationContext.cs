using Len.Abp.Core.Extensions;

namespace Len.Abp.Wpf.Navigation;

/// <summary>
/// <see cref="INavigationContext"/> 的默认实现，从 URI 查询字符串解析导航参数。
/// </summary>
internal class NavigationContext : INavigationContext
{
    /// <summary>
    /// 初始化导航上下文。
    /// </summary>
    /// <param name="uri">导航目标 URI。</param>
    /// <param name="parameters">附加的导航参数，会合并到查询字符串解析结果中。</param>
    public NavigationContext(Uri uri, IParameters? parameters = null)
    {
        Uri = uri;
        Parameters = ParseQuery(uri);

        parameters?.ForEach(item => Parameters.Add(item.Key, item.Value));
    }

    /// <inheritdoc/>
    public IParameters Parameters { get; private set; }

    /// <inheritdoc/>
    public Uri Uri { get; private set; }

    private static IParameters ParseQuery(Uri uri) => IParameters.Create(NavigationUri.GetQuery(uri));
}
