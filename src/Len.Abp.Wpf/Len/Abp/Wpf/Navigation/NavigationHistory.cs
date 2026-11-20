using System.Collections.Concurrent;

namespace Len.Abp.Wpf.Navigation;

/// <summary>
/// 按区域维护的导航历史栈。
/// </summary>
/// <remarks>
/// <para>
/// 每个区域一条独立栈：跨区域回退会产生不可预期的视图状态——父区域回退后，
/// 子区域的视图可能已不存在。区域名是导航的定位单位，因此以它为键。
/// </para>
/// <para>
/// 只记录 <c>(视图名, 参数)</c> 而不记录视图实例：实例可能已被保活策略释放，
/// 回退时按名称重新解析即可，与"导航到该视图"走同一条路径。
/// </para>
/// </remarks>
internal sealed class NavigationHistory(int depth)
{
    private readonly ConcurrentDictionary<string, LinkedList<HistoryEntry>> _stacks = new(StringComparer.Ordinal);
    private readonly int _depth = depth > 0 ? depth : NavigationOptions.DefaultHistoryDepth;

    /// <summary>
    /// 当前异步流是否处于回退之中。
    /// </summary>
    /// <remarks>
    /// 回退本身也是一次导航，若照常压栈，A→B 回退到 A 会把 A 压回栈顶，
    /// 再次回退又回到 B，在两个视图之间来回震荡。
    /// 用 <see cref="AsyncLocal{T}"/> 而不是实例字段：并发导航各自独立，互不影响。
    /// </remarks>
    private readonly AsyncLocal<bool> _isReverting = new();

    /// <summary>
    /// 在"回退中"标记下执行一段导航，期间的压栈请求被忽略。
    /// </summary>
    /// <param name="action">回退导航。</param>
    /// <returns>导航结果。</returns>
    public async ValueTask<INavigationResult> SuppressRecordingAsync(Func<ValueTask<INavigationResult>> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        _isReverting.Value = true;

        try
        {
            return await action();
        }
        finally
        {
            _isReverting.Value = false;
        }
    }

    /// <summary>
    /// 记录一次成功导航；处于回退中时忽略。
    /// </summary>
    /// <param name="regionName">区域名称。</param>
    /// <param name="entry">历史条目。</param>
    /// <remarks>
    /// 与栈顶相同的条目不再重复压入：连续导航到同一视图（例如刷新）不应产生
    /// "回退一次仍在原地"的观感。
    /// </remarks>
    public void Push(string regionName, HistoryEntry entry)
    {
        ArgumentException.ThrowIfNullOrEmpty(regionName);

        if (_isReverting.Value)
        {
            return;
        }

        var stack = _stacks.GetOrAdd(regionName, _ => new LinkedList<HistoryEntry>());

        lock (stack)
        {
            // 只比较视图名：IParameters 是接口，record 的自动相等会退化为引用比较，
            // 每次导航都是新实例，永远不相等。参数不同但视图相同仍视为同一次停留。
            if (stack.Last is { } last && string.Equals(last.Value.ViewName, entry.ViewName, StringComparison.Ordinal))
            {
                return;
            }

            stack.AddLast(entry);

            while (stack.Count > _depth)
            {
                stack.RemoveFirst();
            }
        }
    }

    /// <summary>
    /// 查看最近一条历史之前的条目，不修改栈。
    /// </summary>
    /// <param name="regionName">区域名称。</param>
    /// <param name="previous">回退目标；栈中没有更早的条目时为 <see langword="null"/>。</param>
    /// <returns>可以回退时返回 <see langword="true"/>。</returns>
    /// <remarks>
    /// 与 <see cref="CommitPop"/> 配对使用：先 peek 拿到目标并执行导航，
    /// 只有导航成功后才真正弹出。导航失败时栈保持不变，
    /// 否则一次失败的回退会永久丢掉一条历史，用户再点后退会跳步。
    /// </remarks>
    public bool TryPeekPrevious(string regionName, out HistoryEntry? previous)
    {
        ArgumentException.ThrowIfNullOrEmpty(regionName);

        previous = null;

        if (!_stacks.TryGetValue(regionName, out var stack))
        {
            return false;
        }

        lock (stack)
        {
            if (stack.Count < 2)
            {
                return false;
            }

            previous = stack.Last!.Previous!.Value;

            return true;
        }
    }

    /// <summary>
    /// 弹出栈顶条目，确认回退已完成。
    /// </summary>
    /// <param name="regionName">区域名称。</param>
    /// <remarks>
    /// 只在 <see cref="TryPeekPrevious"/> 返回成功且导航成功后调用。
    /// 栈里只剩一条时不做任何修改，避免一次失败的回退把历史清空。
    /// </remarks>
    public void CommitPop(string regionName)
    {
        ArgumentException.ThrowIfNullOrEmpty(regionName);

        if (!_stacks.TryGetValue(regionName, out var stack))
        {
            return;
        }

        lock (stack)
        {
            if (stack.Count < 2)
            {
                return;
            }

            stack.RemoveLast();
        }
    }

    /// <summary>
    /// 一条导航历史。
    /// </summary>
    /// <param name="ViewName">视图名称。</param>
    /// <param name="Parameters">导航参数。</param>
    internal readonly record struct HistoryEntry(string ViewName, IParameters Parameters);
}
