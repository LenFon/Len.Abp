namespace Len.Abp.Wpf.Navigation;

public interface INavigationContext
{
    IParameters Parameters { get; }

    Uri Uri { get; }
}