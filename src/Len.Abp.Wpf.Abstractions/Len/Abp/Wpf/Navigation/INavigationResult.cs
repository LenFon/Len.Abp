namespace Len.Abp.Wpf.Navigation;

public interface INavigationResult
{
    INavigationContext Context { get; }

    Exception? Exception { get; }

    bool Result { get; }

    static INavigationResult Create(INavigationContext context, bool result) => new NavigationResult(context, result);

    static INavigationResult Create(INavigationContext context, Exception exception) => new NavigationResult(context, exception);
}

file class NavigationResult : INavigationResult
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