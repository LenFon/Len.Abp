using Len.Abp.Core.Extensions;

namespace Len.Abp.Wpf.Navigation;

internal class NavigationContext : INavigationContext
{
    public NavigationContext(Uri uri, IParameters? parameters = null)
    {
        Uri = uri;
        Parameters = ParseQuery(uri);

        parameters?.ForEach(item => Parameters.Add(item.Key, item.Value));
    }

    public IParameters Parameters { get; private set; }

    public Uri Uri { get; private set; }

    private static Uri EnsureAbsolute(Uri uri)
    {
        if (uri.IsAbsoluteUri)
        {
            return uri;
        }

        if (uri is not null && !uri.OriginalString.StartsWith('/'))
        {
            return new Uri("http://localhost/" + uri, UriKind.Absolute);
        }

        return new Uri("http://localhost" + uri, UriKind.Absolute);
    }

    private static string GetQuery(Uri uri) => EnsureAbsolute(uri).Query;

    private static IParameters ParseQuery(Uri uri) => IParameters.Create(GetQuery(uri));
}