namespace Len.Abp.Wpf;

/// <summary>
/// 单实例通道，负责判定主实例并支持后续实例请求主实例激活窗口。
/// </summary>
/// <remarks>
/// 抽象出接口是为了让单实例逻辑可测：跨进程通知依赖命名内核对象，
/// 真实实现无法在单元测试里断言两端行为，测试用替身即可验证调用时序。
/// </remarks>
internal interface IInstanceActivationChannel : IDisposable
{
    /// <summary>
    /// 尝试成为主实例。
    /// </summary>
    /// <returns>本次调用成为主实例时返回 <see langword="true"/>；已有实例在运行时返回 <see langword="false"/>。</returns>
    bool TryClaimPrimary();

    /// <summary>
    /// 请求主实例激活已有窗口。
    /// </summary>
    /// <remarks>
    /// 只在 <see cref="TryClaimPrimary"/> 返回 <see langword="false"/> 的实例上调用。
    /// </remarks>
    void SignalPrimary();

    /// <summary>
    /// 开始监听后续实例的激活请求。
    /// </summary>
    /// <param name="onActivated">收到请求时执行的回调，运行在后台线程上。</param>
    /// <remarks>
    /// 只在 <see cref="TryClaimPrimary"/> 返回 <see langword="true"/> 的实例上调用。
    /// </remarks>
    void StartListening(Action onActivated);
}
