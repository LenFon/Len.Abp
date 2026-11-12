using Microsoft.Extensions.Hosting;

namespace Len.Abp.Wpf.Tests;

/// <summary>
/// 测试用的宿主生命周期替身，可手动触发停止信号。
/// </summary>
internal sealed class FakeHostLifetime : IHostApplicationLifetime
{
    private readonly CancellationTokenSource _started = new();
    private readonly CancellationTokenSource _stopped = new();
    private readonly CancellationTokenSource _stopping = new();

    /// <inheritdoc/>
    public CancellationToken ApplicationStarted => _started.Token;

    /// <inheritdoc/>
    public CancellationToken ApplicationStopping => _stopping.Token;

    /// <inheritdoc/>
    public CancellationToken ApplicationStopped => _stopped.Token;

    /// <summary>
    /// 是否已请求停止应用。
    /// </summary>
    public bool StopRequested { get; private set; }

    /// <inheritdoc/>
    public void StopApplication()
    {
        StopRequested = true;
        _stopping.Cancel();
    }

    /// <summary>
    /// 模拟宿主发出停止信号，但不记录为主动请求。
    /// </summary>
    public void TriggerStopping() => _stopping.Cancel();
}
