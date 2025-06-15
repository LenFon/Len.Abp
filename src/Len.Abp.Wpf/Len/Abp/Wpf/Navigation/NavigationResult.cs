namespace Len.Abp.Wpf.Navigation;

internal class NavigationResult : INavigationResult
{
    public NavigationResult(INavigationContext context, bool result)
    {
        Context = context;
        Result = result;
    }

    public NavigationResult(INavigationContext context, Exception exception)
    {
        Context = context;
        Exception = exception;
        Result = false;
    }

    public INavigationContext Context { get; private set; }

    public Exception? Exception { get; private set; }

    public bool Result { get; private set; }
}