namespace Len.Abp.Wpf.Navigation;

/// <summary>
/// 导航结果，描述本次导航是否成功以及失败原因。
/// </summary>
public interface INavigationResult
{
    /// <summary>
    /// 获取本次导航的上下文。
    /// </summary>
    INavigationContext Context { get; }

    /// <summary>
    /// 获取导航过程中抛出的异常；导航成功时为 <see langword="null"/>。
    /// </summary>
    Exception? Exception { get; }

    /// <summary>
    /// 获取导航是否成功。
    /// </summary>
    bool Result { get; }
}
