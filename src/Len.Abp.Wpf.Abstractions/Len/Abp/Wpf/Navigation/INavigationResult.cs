namespace Len.Abp.Wpf.Navigation;

public interface INavigationResult
{
    INavigationContext Context { get; }

    Exception? Exception { get; }

    bool Result { get; }
}