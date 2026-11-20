namespace Len.Abp.Wpf.Navigation;

/// <summary>
/// 导航串行化闸门：同一时刻只允许一次导航执行，并识别回调触发的重入导航。
/// </summary>
/// <remarks>
/// <para>
/// 重入导航若继续等待锁会自锁：锁被外层导航持有，而外层正在等待回调完成。
/// 因此重入时直接执行，不再排队。
/// </para>
/// <para>
/// <see cref="AsyncLocal{T}"/> 只在当前异步流内可见，不影响其他并发导航；
/// 在方法内赋值也不会泄漏给调用方。
/// </para>
/// <para>
/// <b>标记带生命周期。</b><see cref="AsyncLocal{T}"/> 会随执行上下文流进
/// 回调里启动的游离任务（<c>Task.Run</c>、未 await 的调用），
/// 若只看"标记是否存在"，这些任务会误判为重入而跳过锁，
/// 与外层导航并发修改区域状态。因此标记是一个带 <see cref="NavigationScope.IsActive"/>
/// 的作用域对象：外层导航结束时置为失效，残留标记不再生效，后续导航正常排队。
/// </para>
/// <para>
/// <b>残留的已知边界：</b>游离任务若在外层导航<b>仍在执行时</b>就开始导航，
/// 标记尚未失效，仍会跳过锁。这种情况无法在入口处与"回调里 await 的合法嵌套"区分
/// （两者都在外层执行期间进入），因此改为在外层结束时检测并报告，
/// 让误用可见而不是静默并发。
/// </para>
/// </remarks>
/// <param name="reportConcurrentNavigation">
/// 检测到并发导航时的报告回调；为 <see langword="null"/> 时静默。
/// </param>
internal sealed class NavigationGate(Action<string>? reportConcurrentNavigation = null)
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly AsyncLocal<NavigationScope?> _current = new();
    private readonly Action<string>? _reportConcurrentNavigation = reportConcurrentNavigation;

    /// <summary>
    /// 执行一次导航，必要时先获取串行化锁。
    /// </summary>
    /// <param name="action">导航主体。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>导航结果。</returns>
    /// <remarks>
    /// 取消检查放在获取锁之后：已取消的导航不应占用队列。
    /// <see cref="SemaphoreSlim.WaitAsync(CancellationToken)"/> 自身也接受令牌，
    /// 等待期间取消会立即退出。
    /// </remarks>
    public async ValueTask<INavigationResult> ExecuteAsync(
        Func<ValueTask<INavigationResult>> action,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(action);

        // 只有"外层导航仍在执行"才算重入。外层已结束时残留的标记来自
        // 回调里未 await 的游离任务，此时必须正常排队。
        if (_current.Value is { IsActive: true } scope)
        {
            scope.EnterReentrant();

            try
            {
                return await action();
            }
            finally
            {
                scope.ExitReentrant();
            }
        }

        await _lock.WaitAsync(cancellationToken);

        var ownScope = new NavigationScope();

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            _current.Value = ownScope;

            try
            {
                return await action();
            }
            finally
            {
                ownScope.Complete();

                // 外层已结束但仍有重入导航在跑：那是回调里未 await 的游离任务，
                // 它与外层并发执行过。无法在入口处拦截，至少让使用者知道。
                if (ownScope.HasRunningReentrant)
                {
                    _reportConcurrentNavigation?.Invoke(
                        "检测到并发导航：回调中启动了未 await 的导航，它与外层导航并发执行，"
                            + "区域状态可能不一致。请改为 await 该导航，或在其完成后另行发起。"
                    );
                }

                _current.Value = null;
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// 一次导航的执行作用域，记录是否仍有效以及有多少重入导航在跑。
    /// </summary>
    /// <remarks>
    /// 用对象而不是布尔值：布尔值无法表达"外层已结束"，
    /// 而残留的布尔标记正是游离任务误判重入的根源。
    /// </remarks>
    private sealed class NavigationScope
    {
        private int _reentrantCount;
        private volatile bool _isActive = true;

        /// <summary>
        /// 获取该作用域是否仍对应一次执行中的导航。
        /// </summary>
        public bool IsActive => _isActive;

        /// <summary>
        /// 获取是否仍有重入导航在执行。
        /// </summary>
        public bool HasRunningReentrant => Volatile.Read(ref _reentrantCount) > 0;

        /// <summary>
        /// 登记一个重入导航开始执行。
        /// </summary>
        public void EnterReentrant() => Interlocked.Increment(ref _reentrantCount);

        /// <summary>
        /// 登记一个重入导航结束执行。
        /// </summary>
        public void ExitReentrant() => Interlocked.Decrement(ref _reentrantCount);

        /// <summary>
        /// 标记外层导航已结束，使残留标记失效。
        /// </summary>
        public void Complete() => _isActive = false;
    }
}
