using Len.Abp.Wpf;
using Xunit;

namespace Len.Abp.Wpf.Tests;

/// <summary>
/// 覆盖 <see cref="NamedInstanceActivationChannel"/> 的主实例判定与激活通知。
/// </summary>
/// <remarks>
/// 命名内核对象在同一进程内同样可用，因此无需真实双开即可验证两端行为。
/// 每个测试使用唯一应用名，避免并行执行时互相干扰。
/// </remarks>
public class NamedInstanceActivationChannelTests
{
    /// <summary>
    /// 第一个通道成为主实例，同名通道判定为非主实例。
    /// </summary>
    [Fact]
    public void TryClaimPrimary_SecondChannelWithSameName_ReturnsFalse()
    {
        var applicationName = $"claim-{Guid.NewGuid():N}";
        using var primary = new NamedInstanceActivationChannel(applicationName);
        using var secondary = new NamedInstanceActivationChannel(applicationName);

        Assert.True(primary.TryClaimPrimary());
        Assert.False(secondary.TryClaimPrimary());
    }

    /// <summary>
    /// 主实例释放后，新通道可以重新成为主实例。
    /// </summary>
    [Fact]
    public void TryClaimPrimary_AfterPrimaryDisposed_ReturnsTrue()
    {
        var applicationName = $"reclaim-{Guid.NewGuid():N}";

        using (var primary = new NamedInstanceActivationChannel(applicationName))
        {
            Assert.True(primary.TryClaimPrimary());
        }

        using var next = new NamedInstanceActivationChannel(applicationName);

        Assert.True(next.TryClaimPrimary());
    }

    /// <summary>
    /// 不同应用名的通道互不影响。
    /// </summary>
    [Fact]
    public void TryClaimPrimary_DifferentApplicationNames_AreIndependent()
    {
        using var first = new NamedInstanceActivationChannel($"a-{Guid.NewGuid():N}");
        using var second = new NamedInstanceActivationChannel($"b-{Guid.NewGuid():N}");

        Assert.True(first.TryClaimPrimary());
        Assert.True(second.TryClaimPrimary());
    }

    /// <summary>
    /// 第二个实例发出的信号会触发主实例的激活回调。
    /// </summary>
    [Fact]
    public async Task SignalPrimary_TriggersListeningCallback()
    {
        var applicationName = $"signal-{Guid.NewGuid():N}";
        using var primary = new NamedInstanceActivationChannel(applicationName);
        using var secondary = new NamedInstanceActivationChannel(applicationName);

        Assert.True(primary.TryClaimPrimary());
        Assert.False(secondary.TryClaimPrimary());

        var activated = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        primary.StartListening(() => activated.TrySetResult());

        secondary.SignalPrimary();

        var completed = await Task.WhenAny(activated.Task, Task.Delay(TimeSpan.FromSeconds(5)));

        Assert.Same(activated.Task, completed);
    }

    /// <summary>
    /// 重复调用 StartListening 只启动一个监听线程，回调不会重复触发。
    /// </summary>
    [Fact]
    public async Task StartListening_CalledTwice_RegistersSingleListener()
    {
        var applicationName = $"twice-{Guid.NewGuid():N}";
        using var primary = new NamedInstanceActivationChannel(applicationName);
        using var secondary = new NamedInstanceActivationChannel(applicationName);

        primary.TryClaimPrimary();

        var count = 0;
        var activated = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        primary.StartListening(() =>
        {
            if (Interlocked.Increment(ref count) == 1)
            {
                activated.TrySetResult();
            }
        });
        primary.StartListening(() => Interlocked.Increment(ref count));

        secondary.SignalPrimary();
        await Task.WhenAny(activated.Task, Task.Delay(TimeSpan.FromSeconds(5)));

        // 给可能存在的第二个监听线程留出触发时间。
        await Task.Delay(100);

        Assert.Equal(1, Volatile.Read(ref count));
    }

    /// <summary>
    /// 释放后监听线程退出，不再响应信号。
    /// </summary>
    [Fact]
    public async Task Dispose_StopsListening()
    {
        var applicationName = $"dispose-{Guid.NewGuid():N}";
        var primary = new NamedInstanceActivationChannel(applicationName);
        using var secondary = new NamedInstanceActivationChannel(applicationName);

        primary.TryClaimPrimary();

        var count = 0;
        primary.StartListening(() => Interlocked.Increment(ref count));

        primary.Dispose();

        // 主实例已释放，信号不再有监听者；这里只验证不抛异常且回调未增加。
        secondary.SignalPrimary();
        await Task.Delay(100);

        Assert.Equal(0, Volatile.Read(ref count));
    }

    /// <summary>
    /// 重复释放不抛异常。
    /// </summary>
    /// <remarks>
    /// 通道是进程级资源，工作器停止流程与容器释放都会调用 <see cref="NamedInstanceActivationChannel.Dispose"/>。
    /// 第二次释放若继续操作已释放的事件句柄，会抛 <see cref="ObjectDisposedException"/>，
    /// 表现为关闭窗口时宿主崩溃。
    /// </remarks>
    [Fact]
    public void Dispose_CalledTwice_DoesNotThrow()
    {
        var primary = new NamedInstanceActivationChannel($"twice-dispose-{Guid.NewGuid():N}");
        primary.TryClaimPrimary();
        primary.StartListening(() => { });

        primary.Dispose();
        primary.Dispose();
    }

    /// <summary>
    /// 应用名为空时构造抛出异常。
    /// </summary>
    /// <param name="applicationName">非法的应用名。</param>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_InvalidApplicationName_Throws(string? applicationName)
    {
        Assert.ThrowsAny<ArgumentException>(() => new NamedInstanceActivationChannel(applicationName!));
    }
}
