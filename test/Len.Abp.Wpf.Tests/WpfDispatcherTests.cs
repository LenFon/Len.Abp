using Len.Abp.Wpf;
using System.Windows.Threading;
using Xunit;

namespace Len.Abp.Wpf.Tests;

/// <summary>
/// 覆盖 <see cref="WpfDispatcher"/> 对 <see cref="WpfContext"/> 的包装。
/// </summary>
/// <remarks>
/// 用独立 STA 线程上的真实 <see cref="Dispatcher"/> 验证转发，不依赖
/// <see cref="System.Windows.Application"/>（同一 AppDomain 只能有一个实例）。
/// </remarks>
public class WpfDispatcherTests
{
    /// <summary>
    /// 应用未启动时给出明确异常，而不是 <see cref="NullReferenceException"/>。
    /// </summary>
    [Fact]
    public void CheckAccess_WhenNotRunning_ThrowsInvalidOperationException()
    {
        var context = new WpfContext { IsRunning = false };
        var dispatcher = new WpfDispatcher(context);

        var exception = Assert.Throws<InvalidOperationException>(() => dispatcher.CheckAccess());

        Assert.Contains("尚未启动", exception.Message);
    }

    /// <summary>
    /// 应用未启动时封送同样给出明确异常。
    /// </summary>
    [Fact]
    public async Task InvokeAsync_WhenNotRunning_ThrowsInvalidOperationException()
    {
        var context = new WpfContext { IsRunning = false };
        var dispatcher = new WpfDispatcher(context);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await dispatcher.InvokeAsync(() => Task.FromResult(1))
        );

        Assert.Contains("尚未启动", exception.Message);
    }

    /// <summary>
    /// 运行中时 <see cref="WpfDispatcher.CheckAccess"/> 转发到真实调度器。
    /// </summary>
    [Fact]
    public void CheckAccess_WhenRunning_ForwardsToDispatcher()
    {
        Sta.Run(() =>
        {
            var context = new DispatcherContext(Dispatcher.CurrentDispatcher) { IsRunning = true };
            var dispatcher = new WpfDispatcher(context);

            Assert.True(dispatcher.CheckAccess());
        });
    }

    /// <summary>
    /// 运行中时封送在调度器线程上执行并返回结果。
    /// </summary>
    /// <remarks>
    /// <see cref="Dispatcher.InvokeAsync{TResult}(Func{TResult})"/> 即使在调度器线程上调用也只是排队，
    /// 不会同步执行，因此必须泵消息帧，不能直接阻塞等待。
    /// </remarks>
    [Fact]
    public void InvokeAsync_WhenRunning_ExecutesOnDispatcherThread()
    {
        Sta.Run(() =>
        {
            var dispatcher = Dispatcher.CurrentDispatcher;
            var context = new DispatcherContext(dispatcher) { IsRunning = true };
            var wrapper = new WpfDispatcher(context);

            var task = wrapper.InvokeAsync(() => Task.FromResult(dispatcher.Thread.ManagedThreadId));

            // 排队的操作是 Normal 优先级；Background 优先级的停止帧被泵送时，
            // Normal 队列必然已清空，任务随之完成。
            while (!task.IsCompleted)
            {
                var frame = new DispatcherFrame();
                dispatcher.BeginInvoke(
                    DispatcherPriority.Background,
                    new Action(() => frame.Continue = false)
                );
                Dispatcher.PushFrame(frame);
            }

            Assert.Equal(dispatcher.Thread.ManagedThreadId, task.GetAwaiter().GetResult());
        });
    }

    /// <summary>
    /// 传入 null 操作时抛出异常。
    /// </summary>
    [Fact]
    public async Task InvokeAsync_NullAction_Throws()
    {
        var context = new WpfContext { IsRunning = true };
        var dispatcher = new WpfDispatcher(context);

        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await dispatcher.InvokeAsync<int>(null!)
        );
    }

    /// <summary>
    /// 使用指定调度器的上下文替身，避免依赖 <see cref="System.Windows.Application"/>。
    /// </summary>
    private sealed class DispatcherContext(Dispatcher dispatcher) : WpfContext
    {
        public override Dispatcher Dispatcher => dispatcher;
    }
}
