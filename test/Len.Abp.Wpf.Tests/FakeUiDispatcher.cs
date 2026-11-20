namespace Len.Abp.Wpf.Tests;

/// <summary>
/// 调度器替身：可配置当前线程是否为 UI 线程，并记录封送次数。
/// </summary>
/// <remarks>
/// 默认 <see cref="CheckAccess"/> 返回 <see langword="true"/>，使既有测试走"已在 UI 线程"的快路径，
/// 不改变原有行为。需要验证封送路径的测试把它设为 <see langword="false"/>。
/// </remarks>
internal sealed class FakeUiDispatcher : IUiDispatcher
{
    /// <summary>
    /// 获取或设置 <see cref="CheckAccess"/> 的返回值。
    /// </summary>
    public bool IsUiThread { get; set; } = true;

    /// <summary>
    /// 获取 <see cref="InvokeAsync"/> 被调用的次数。
    /// </summary>
    public int InvokeCount { get; private set; }

    /// <inheritdoc/>
    public bool CheckAccess() => IsUiThread;

    /// <inheritdoc/>
    public async Task<T> InvokeAsync<T>(Func<Task<T>> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        InvokeCount++;

        // 真实调度器在 UI 线程上执行 action，且异步续体也在 UI 线程上运行，
        // 因此 action 内部再调用 CheckAccess 会返回 true。这里必须如实模拟，
        // 否则公开入口会无限递归封送，直到栈溢出。
        var wasUiThread = IsUiThread;
        IsUiThread = true;

        try
        {
            // 同步执行：测试没有消息泵，异步排队会永久挂起。
            return await action();
        }
        finally
        {
            IsUiThread = wasUiThread;
        }
    }
}
