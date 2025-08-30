namespace System.Windows;

public class UriResourceDictionary : ResourceDictionary
{
    public UriResourceDictionary(string uri) : this(uri, UriKind.Absolute)
    {
    }

    public UriResourceDictionary(string uri, UriKind uriKind)
    {
        Source = new Uri(uri, uriKind);
    }
}