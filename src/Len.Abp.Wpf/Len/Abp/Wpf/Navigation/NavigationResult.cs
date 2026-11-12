namespace Len.Abp.Wpf.Navigation;

/// <summary>
/// <see cref="INavigationResult"/> 的默认实现。
/// </summary>
internal class NavigationResult : INavigationResult
{
    /// <summary>
    /// 初始化表示成功的导航结果。
    /// </summary>
    /// <param name="context">导航上下文。</param>
    /// <param name="result">导航是否成功。</param>
    public NavigationResult(INavigationContext context, bool result)
    {
        Context = context;
        Result = result;
    }

    /// <summary>
    /// 初始化表示失败的导航结果。
    /// </summary>
    /// <param name="context">导航上下文。</param>
    /// <param name="exception">导致导航失败的异常。</param>
    public NavigationResult(INavigationContext context, Exception exception)
    {
        Context = context;
        Exception = exception;
        Result = false;
    }

    /// <inheritdoc/>
    public INavigationContext Context { get; private set; }

    /// <inheritdoc/>
    public Exception? Exception { get; private set; }

    /// <inheritdoc/>
    public bool Result { get; private set; }
}
