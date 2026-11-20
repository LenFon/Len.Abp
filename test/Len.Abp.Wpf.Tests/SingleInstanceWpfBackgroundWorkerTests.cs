using Len.Abp.Wpf;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Xunit;

namespace Len.Abp.Wpf.Tests;

/// <summary>
/// 覆盖 <see cref="SingleInstanceWpfBackgroundWorker"/> 的单实例检测与激活通知。
/// </summary>
[Collection(GlobalServiceProviderCollection.Name)]
public class SingleInstanceWpfBackgroundWorkerTests
{
    /// <summary>
    /// 未启用单实例时不创建通道，也不停止应用。
    /// </summary>
    [Fact]
    public async Task StartAsync_WhenDisabled_DoesNotStopApplication()
    {
        var lifetime = new FakeHostLifetime();
        var channel = new FakeInstanceActivationChannel(isPrimary: true);
        var worker = CreateWorker(singleInstance: false, lifetime, channel);

        await worker.StartAsync();

        Assert.False(lifetime.StopRequested);
        Assert.False(channel.Claimed);
    }

    /// <summary>
    /// 首次启动时成为主实例并开始监听，不停止应用。
    /// </summary>
    [Fact]
    public async Task StartAsync_FirstInstance_StartsListening()
    {
        var lifetime = new FakeHostLifetime();
        var channel = new FakeInstanceActivationChannel(isPrimary: true);
        var worker = CreateWorker(singleInstance: true, lifetime, channel);

        await worker.StartAsync();

        Assert.True(channel.Claimed);
        Assert.True(channel.IsListening);
        Assert.False(lifetime.StopRequested);

        await worker.StopAsync();
    }

    /// <summary>
    /// 第二个实例通知主实例后停止应用。
    /// </summary>
    [Fact]
    public async Task StartAsync_SecondInstance_SignalsPrimaryAndStopsApplication()
    {
        var lifetime = new FakeHostLifetime();
        var channel = new FakeInstanceActivationChannel(isPrimary: false);
        var worker = CreateWorker(singleInstance: true, lifetime, channel);

        await worker.StartAsync();

        Assert.True(channel.Signaled);
        Assert.False(channel.IsListening);
        Assert.True(lifetime.StopRequested);
    }

    /// <summary>
    /// 取消令牌已取消时不执行单实例检测。
    /// </summary>
    [Fact]
    public async Task StartAsync_WithCancelledToken_DoesNotStopApplication()
    {
        var lifetime = new FakeHostLifetime();
        var channel = new FakeInstanceActivationChannel(isPrimary: true);
        var worker = CreateWorker(singleInstance: true, lifetime, channel);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await worker.StartAsync(cts.Token);

        Assert.False(lifetime.StopRequested);
        Assert.False(channel.Claimed);
    }

    /// <summary>
    /// 应用尚未启动完成时收到激活请求，不解析激活器。
    /// </summary>
    [Fact]
    public async Task ActivationRequest_BeforeUiStarts_IsIgnored()
    {
        var lifetime = new FakeHostLifetime();
        var channel = new FakeInstanceActivationChannel(isPrimary: true);
        var activator = new RecordingShellActivator();
        var worker = CreateWorker(singleInstance: true, lifetime, channel, activator);

        await worker.StartAsync();
        channel.RaiseActivated();

        Assert.Equal(0, activator.CallCount);

        await worker.StopAsync();
    }

    /// <summary>
    /// 应用运行中收到激活请求时，在 UI 线程上调用 Shell 激活。
    /// </summary>
    /// <remarks>
    /// 用独立的 STA 线程与真实调度器模拟 UI 线程，避免依赖 <see cref="System.Windows.Application"/>
    /// （同一 AppDomain 只能有一个实例，已被其他测试占用）。
    /// </remarks>
    [Fact]
    public void ActivationRequest_WhenRunning_ActivatesOnDispatcherThread()
    {
        var lifetime = new FakeHostLifetime();
        var channel = new FakeInstanceActivationChannel(isPrimary: true);
        var activator = new RecordingShellActivator();

        Sta.Run(() =>
        {
            var dispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;
            var context = new DispatcherContext(dispatcher) { IsRunning = true };
            var worker = CreateWorker(singleInstance: true, lifetime, channel, activator, context);

            worker.StartAsync().GetAwaiter().GetResult();
            channel.RaiseActivated();

            // 激活通过 InvokeAsync 以 Normal 优先级排队。在 Background 优先级排一个停止帧的操作，
            // 帧被泵送时 Normal 队列必然已清空，激活随之完成。
            var frame = new System.Windows.Threading.DispatcherFrame();
            dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Background,
                new Action(() => frame.Continue = false)
            );
            System.Windows.Threading.Dispatcher.PushFrame(frame);

            Assert.Equal(1, activator.CallCount);
            Assert.True(activator.CalledOnDispatcherThread);

            worker.StopAsync().GetAwaiter().GetResult();
        });
    }

    /// <summary>
    /// 停止时释放通道，监听线程随之结束。
    /// </summary>
    [Fact]
    public async Task StopAsync_DisposesChannel()
    {
        var lifetime = new FakeHostLifetime();
        var channel = new FakeInstanceActivationChannel(isPrimary: true);
        var worker = CreateWorker(singleInstance: true, lifetime, channel);

        await worker.StartAsync();
        lifetime.TriggerStopping();

        Assert.True(channel.IsDisposed);
    }

    /// <summary>
    /// 激活 Shell 失败时只记录警告，不影响主实例继续运行。
    /// </summary>
    [Fact]
    public void ActivationRequest_WhenActivatorThrows_LogsWarning()
    {
        var lifetime = new FakeHostLifetime();
        var channel = new FakeInstanceActivationChannel(isPrimary: true);
        var activator = new ThrowingShellActivator();
        var loggerProvider = new RecordingLoggerProvider();

        Sta.Run(() =>
        {
            var dispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;
            var context = new DispatcherContext(dispatcher) { IsRunning = true };
            var worker = CreateWorker(
                singleInstance: true,
                lifetime,
                channel,
                activator,
                context,
                loggerProvider
            );

            worker.StartAsync().GetAwaiter().GetResult();
            channel.RaiseActivated();

            var frame = new System.Windows.Threading.DispatcherFrame();
            dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Background,
                new Action(() => frame.Continue = false)
            );
            System.Windows.Threading.Dispatcher.PushFrame(frame);

            Assert.Contains(
                loggerProvider.Entries,
                entry => entry.Level == Microsoft.Extensions.Logging.LogLevel.Warning
                    && entry.Exception is InvalidOperationException
            );

            worker.StopAsync().GetAwaiter().GetResult();
        });
    }

    private static SingleInstanceWpfBackgroundWorker CreateWorker(
        bool singleInstance,
        FakeHostLifetime lifetime,
        FakeInstanceActivationChannel channel,
        ShellActivator? activator = null,
        WpfContext? context = null,
        RecordingLoggerProvider? loggerProvider = null
    )
    {
        var services = new ServiceCollection();
        if (loggerProvider is null)
        {
            services.AddLogging();
        }
        else
        {
            services.AddLogging(builder => builder.AddProvider(loggerProvider));
        }

        services.AddSingleton<IHostApplicationLifetime>(lifetime);
        services.AddSingleton<IHostEnvironment>(new FakeHostEnvironment("test-app"));
        services.AddSingleton<IInstanceActivationChannel>(channel);
        services.AddSingleton<WpfContext>(context ?? new WpfContext());
        services.AddSingleton<ShellActivator>(activator ?? new RecordingShellActivator());
        services.Configure<WpfOptions>(o => o.SingleInstance = singleInstance);

        var provider = services.BuildServiceProvider();

        return new SingleInstanceWpfBackgroundWorker
        {
            LazyServiceProvider = new AbpLazyServiceProvider(provider),
        };
    }

    /// <summary>
    /// 使用指定调度器的上下文替身，避免依赖 <see cref="System.Windows.Application"/>。
    /// </summary>
    private sealed class DispatcherContext(System.Windows.Threading.Dispatcher dispatcher) : WpfContext
    {
        public override System.Windows.Threading.Dispatcher Dispatcher => dispatcher;
    }

    /// <summary>
    /// 单实例通道替身，记录调用并允许手动触发激活回调。
    /// </summary>
    private sealed class FakeInstanceActivationChannel(bool isPrimary) : IInstanceActivationChannel
    {
        private Action? _onActivated;

        public bool Claimed { get; private set; }

        public bool IsDisposed { get; private set; }

        public bool IsListening => _onActivated is not null;

        public bool Signaled { get; private set; }

        public void Dispose()
        {
            IsDisposed = true;
            _onActivated = null;
        }

        public void RaiseActivated() => _onActivated?.Invoke();

        public void SignalPrimary() => Signaled = true;

        public void StartListening(Action onActivated) => _onActivated = onActivated;

        public bool TryClaimPrimary()
        {
            Claimed = true;
            return isPrimary;
        }
    }

    /// <summary>
    /// Shell 激活器替身，记录调用次数。
    /// </summary>
    private sealed class RecordingShellActivator : ShellActivator
    {
        public RecordingShellActivator()
            : base(null!)
        {
        }

        public int CallCount { get; private set; }

        public bool CalledOnDispatcherThread { get; private set; }

        public override void Activate()
        {
            CallCount++;
            CalledOnDispatcherThread = System.Windows.Threading.Dispatcher.CurrentDispatcher.CheckAccess();
        }
    }

    /// <summary>
    /// 激活时抛异常的 Shell 激活器替身。
    /// </summary>
    private sealed class ThrowingShellActivator : ShellActivator
    {
        public ThrowingShellActivator()
            : base(null!)
        {
        }

        public override void Activate() => throw new InvalidOperationException("激活失败。");
    }
}
