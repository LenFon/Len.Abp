using System.Threading;

namespace Len.Abp.Wpf;

/// <summary>
/// 基于命名内核对象的单实例通道：命名 <see cref="Mutex"/> 判定主实例，命名 <see cref="EventWaitHandle"/> 传递激活请求。
/// </summary>
/// <remarks>
/// 用事件而不是命名管道：只需"通知一次"的语义，无需处理连接生命周期与消息边界。
/// 名称带 <c>Local\</c> 前缀，作用域限于当前登录会话，符合桌面应用的常规预期。
/// 事件句柄在构造时就创建，早于互斥量判定：否则第二个实例可能在主实例建好监听之前发出请求，
/// 信号会因找不到事件对象而丢失，用户看到的仍是"双击没反应"。
/// </remarks>
internal sealed class NamedInstanceActivationChannel : IInstanceActivationChannel
{
    private readonly EventWaitHandle _activationEvent;
    private readonly string _mutexName;
    private Mutex? _mutex;
    private Thread? _listener;
    private volatile bool _listening;
    private int _disposed;

    /// <summary>
    /// 初始化单实例通道。
    /// </summary>
    /// <param name="applicationName">应用名称，用于拼接内核对象名。</param>
    public NamedInstanceActivationChannel(string applicationName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationName);

        _mutexName = $@"Local\Len.Abp.Wpf.{applicationName}";
        _activationEvent = new EventWaitHandle(
            initialState: false,
            EventResetMode.AutoReset,
            $@"Local\Len.Abp.Wpf.Activate.{applicationName}"
        );
    }

    /// <inheritdoc/>
    public bool TryClaimPrimary()
    {
        // 该互斥量从不被持有：只用它的存在性作为“已在运行”的标记。
        // 这样检测不涉及线程亲和性，关闭时也无需调用 ReleaseMutex。
        _mutex = new Mutex(initiallyOwned: false, _mutexName, out var createdNew);

        if (createdNew)
        {
            return true;
        }

        // 另一个实例已持有标记；释放本实例的句柄，避免继续占用该名称。
        _mutex.Dispose();
        _mutex = null;

        return false;
    }

    /// <inheritdoc/>
    public void SignalPrimary() => _activationEvent.Set();

    /// <inheritdoc/>
    public void StartListening(Action onActivated)
    {
        ArgumentNullException.ThrowIfNull(onActivated);

        if (_listening)
        {
            return;
        }

        _listening = true;

        _listener = new Thread(() =>
        {
            // WaitOne 无超时，只在收到信号或句柄被释放时返回。
            // Dispose 会先置 _listening 为 false 再 Set 一次，借此唤醒线程退出。
            while (_activationEvent.WaitOne())
            {
                if (!_listening)
                {
                    return;
                }

                onActivated();
            }
        })
        {
            IsBackground = true,
            Name = "Len.Abp.Wpf 单实例激活监听",
        };
        _listener.Start();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        // 通道是进程级资源，工作器停止流程与容器释放都会调用这里，必须可重入。
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
        {
            return;
        }

        _listening = false;

        // 唤醒监听线程，否则它会一直阻塞在 WaitOne 上，直到进程退出。
        _activationEvent.Set();
        _listener?.Join(TimeSpan.FromSeconds(1));
        _listener = null;

        _activationEvent.Dispose();

        _mutex?.Dispose();
        _mutex = null;
    }
}
