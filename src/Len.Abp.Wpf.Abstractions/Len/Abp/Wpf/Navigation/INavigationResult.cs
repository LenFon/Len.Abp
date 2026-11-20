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
    /// <remarks>
    /// <see langword="null"/> 有两种含义：导航成功，或目标区域不存在。
    /// 区域不存在属于调用方配置错误而非运行时故障，因此不构造异常；
    /// 需要区分时请同时检查 <see cref="Result"/>，并查看导航服务的诊断日志。
    /// </remarks>
    Exception? Exception { get; }

    /// <summary>
    /// 获取导航是否成功。
    /// </summary>
    bool Result { get; }
}
