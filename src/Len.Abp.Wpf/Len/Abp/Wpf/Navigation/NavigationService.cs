using Len.Abp.Wpf.Mvvm;
using Len.Abp.Wpf.Navigation.Regions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace Len.Abp.Wpf.Navigation;

/// <summary>
/// <see cref="INavigationService"/> 的默认实现，负责解析视图、切换区域活动视图并通知导航生命周期。
/// </summary>
/// <remarks>
/// 本类只做编排：视图解析交给 <see cref="NavigationViewResolver"/>，
/// 参数校验交给 <see cref="NavigationParameterValidator"/>，
/// 串行化与重入识别交给 <see cref="NavigationGate"/>，
/// 历史记录交给 <see cref="NavigationHistory"/>。
/// </remarks>
internal class NavigationService(
    IRegionFinder regionFinder,
    IRegionProvider regionProvider,
    IServiceProvider serviceProvider,
    IUiDispatcher uiDispatcher,
    NavigationViewResolver viewResolver,
    IOptions<NavigationOptions> navigationOptions
) : INavigationService, ISingletonDependency
{
    private readonly IRegionFinder _regionFinder = regionFinder;
    private readonly IRegionProvider _regionProvider = regionProvider;
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly IUiDispatcher _uiDispatcher = uiDispatcher;
    private readonly NavigationViewResolver _viewResolver = viewResolver;
    private readonly NavigationHistory _history = new(navigationOptions.Value.HistoryDepth);

    /// <summary>
    /// 导航串行化闸门；检测到并发导航时记录警告。
    /// </summary>
    /// <remarks>
    /// 延迟创建：报告回调需要 <see cref="Logger"/>，而它是实例属性，
    /// 字段初始化器里拿不到。导航本身在 UI 线程上串行执行，
    /// 因此 <c>??=</c> 的竞态最多多建一个实例，不影响正确性。
    /// </remarks>
    private NavigationGate? _gate;

    private NavigationGate Gate =>
        _gate ??= new NavigationGate(message => Logger?.LogWarning("{Message}", message));
    /// <summary>
    /// 获取日志；容器未注册日志服务时为 <see langword="null"/>。
    /// </summary>
    private ILogger<NavigationService>? Logger =>
        _serviceProvider.GetService<ILogger<NavigationService>>();

    /// <summary>
    /// 导航到指定名称的视图。
    /// </summary>
    /// <param name="regionName">目标区域名称。</param>
    /// <param name="viewName">目标视图名称。</param>
    /// <param name="navigationParameters">导航参数。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>导航结果。</returns>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> 已取消时抛出。</exception>
    public async ValueTask<INavigationResult> NavigateAsync(
        string regionName,
        string viewName,
        IParameters navigationParameters,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(viewName, nameof(viewName));
        cancellationToken.ThrowIfCancellationRequested();

        return await NavigateAsync(
            regionName,
            new Uri(viewName, UriKind.RelativeOrAbsolute),
            navigationParameters,
            cancellationToken
        );
    }

    /// <summary>
    /// 导航到指定名称的视图，不附加导航参数。
    /// </summary>
    /// <param name="regionName">目标区域名称。</param>
    /// <param name="viewName">目标视图名称。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>导航结果。</returns>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> 已取消时抛出。</exception>
    public ValueTask<INavigationResult> NavigateAsync(
        string regionName,
        string viewName,
        CancellationToken cancellationToken = default
    ) => NavigateAsync(regionName, viewName, IParameters.Create(), cancellationToken);

    /// <summary>
    /// 导航到指定 URI 对应的视图。
    /// </summary>
    /// <param name="regionName">目标区域名称。</param>
    /// <param name="target">目标 URI。</param>
    /// <param name="navigationParameters">导航参数。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>导航结果。</returns>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> 已取消时抛出。</exception>
    /// <exception cref="InvalidOperationException">WPF 应用尚未启动时抛出。</exception>
    /// <remarks>
    /// 这是所有公开重载的终点。封送与串行化统一由 <see cref="RunOnUiThreadAsync"/> 处理：
    /// 视图构造与区域激活都触碰 WPF 对象，必须在 UI 线程上执行，
    /// 且只封送整次导航，不在内部各层重复封送，否则重入会重复触发 <c>OnNavigatedFrom</c>。
    /// </remarks>
    public ValueTask<INavigationResult> NavigateAsync(
        string regionName,
        Uri target,
        IParameters navigationParameters,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        return RunOnUiThreadAsync(
            () => NavigateByNameAsync(regionName, target, navigationParameters, cancellationToken),
            cancellationToken
        );
    }

    /// <summary>
    /// 回退到指定区域的上一个视图。
    /// </summary>
    /// <param name="regionName">目标区域名称。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>导航结果；区域没有历史或已到最早一条时返回失败结果。</returns>
    /// <remarks>
    /// 与 <see cref="NavigateAsync(string, Uri, IParameters, CancellationToken)"/> 走同一条
    /// 封送与串行化路径，因此回退与普通导航不会交错执行。
    /// 回退本身不产生新的历史条目，否则会在两个视图之间来回震荡。
    /// </remarks>
    public ValueTask<INavigationResult> GoBackAsync(
        string regionName,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(regionName, nameof(regionName));
        cancellationToken.ThrowIfCancellationRequested();

        return RunOnUiThreadAsync(() => GoBackRegionAsync(regionName, cancellationToken), cancellationToken);
    }

    /// <summary>
    /// 在 UI 线程上串行执行一次导航动作。
    /// </summary>
    /// <param name="action">导航动作，触碰 WPF 对象，必须在 UI 线程上执行。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>导航结果。</returns>
    /// <remarks>
    /// 只封送整次导航，不在内部各层重复封送，否则重入会重复触发 <c>OnNavigatedFrom</c>。
    /// 串行化锁也在 UI 线程上获取，保证回退与普通导航不会交错执行。
    /// </remarks>
    private ValueTask<INavigationResult> RunOnUiThreadAsync(
        Func<ValueTask<INavigationResult>> action,
        CancellationToken cancellationToken
    )
    {
        if (_uiDispatcher.CheckAccess())
        {
            return Gate.ExecuteAsync(action, cancellationToken);
        }

        // ValueTask<T> 不能直接作为 Func<Task<T>> 的返回值，需要 AsTask()。
        return new ValueTask<INavigationResult>(
            _uiDispatcher.InvokeAsync(() => RunOnUiThreadAsync(action, cancellationToken).AsTask())
        );
    }

    private async ValueTask<INavigationResult> GoBackRegionAsync(string regionName, CancellationToken cancellationToken)
    {
        var region = _regionFinder.FindRegion(regionName);

        if (region is null)
        {
            Logger?.LogWarning(
                "回退失败：区域 {RegionName} 不存在。当前已注册的区域：{AvailableRegions}",
                regionName,
                FormatAvailableRegions()
            );

            return CreateBackFailure();
        }

        if (!_history.TryPeekPrevious(regionName, out var previous) || previous is null)
        {
            // 栈空不是异常：调用方通常用它判断"还能不能回退"。
            // Debug 通常被关闭，先判断级别，避免无谓地求值参数。
            if (Logger is { } logger && logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("回退失败：区域 {RegionName} 没有更早的导航历史。", regionName);
            }

            return CreateBackFailure();
        }

        // 回退不记录新历史，否则 A→B 后回退到 A 会把 A 压回栈顶，再次回退又回到 B。
        var result = await _history.SuppressRecordingAsync(() =>
            NavigateInRegionAsync(
                region,
                new Uri(previous.Value.ViewName, UriKind.RelativeOrAbsolute),
                previous.Value.Parameters,
                cancellationToken
            )
        );

        // 只有回退成功才弹出历史：失败时栈必须保持原样，
        // 否则用户再点一次后退会跳过本该回到的视图。
        if (result.Result)
        {
            _history.CommitPop(regionName);
        }

        return result;
    }

    private static NavigationResult CreateBackFailure() =>
        new(new NavigationContext(new Uri("back", UriKind.Relative), IParameters.Create()), false);

    /// <summary>
    /// 执行一次导航：定位区域并切换其活动视图。
    /// </summary>
    /// <param name="regionName">目标区域名称。</param>
    /// <param name="target">目标 URI。</param>
    /// <param name="navigationParameters">导航参数。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>导航结果。</returns>
    private async ValueTask<INavigationResult> NavigateByNameAsync(
        string regionName,
        Uri target,
        IParameters navigationParameters,
        CancellationToken cancellationToken
    )
    {
        var region = _regionFinder.FindRegion(regionName);
        if (region is not null)
        {
            return await NavigateInRegionAsync(region, target, navigationParameters, cancellationToken);
        }

        // 区域不存在时 Exception 保持为 null（既有契约），因此日志是唯一的诊断线索。
        // 列出已注册区域，使用者才能发现是区域名拼错还是区域根本没创建。
        Logger?.LogWarning(
            "导航失败：区域 {RegionName} 不存在。当前已注册的区域：{AvailableRegions}",
            regionName,
            FormatAvailableRegions()
        );

        return new NavigationResult(new NavigationContext(target, navigationParameters), false);
    }

    /// <summary>
    /// 把已注册的区域名格式化为逗号分隔的列表，用于诊断日志。
    /// </summary>
    /// <returns>区域名列表；没有区域时返回"（无）"。</returns>
    private string FormatAvailableRegions()
    {
        var names = _regionProvider.Regions.Select(region => region.Name).Distinct().ToArray();

        return names.Length == 0 ? "（无）" : string.Join(", ", names);
    }

    /// <summary>
    /// 通知即将离开的活动视图，并在回调失败时记录视图名与阶段。
    /// </summary>
    /// <param name="region">目标区域。</param>
    /// <param name="context">导航上下文。</param>
    /// <param name="views">即将离开的活动视图。</param>
    private async Task NotifyActiveViewsNavigatingFromAsync(IRegion region, INavigationContext context, IView[] views)
    {
        foreach (var activeView in views)
        {
            if (activeView.ViewModel is INavigationAware aware)
            {
                await InvokeNavigationCallbackAsync(
                    region,
                    activeView,
                    nameof(INavigationAware.OnNavigatedFromAsync),
                    () => aware.OnNavigatedFromAsync(context)
                );
            }
        }
    }

    /// <summary>
    /// 通知目标视图已进入，并在回调失败时记录视图名与阶段。
    /// </summary>
    /// <param name="region">目标区域。</param>
    /// <param name="context">导航上下文。</param>
    /// <param name="view">目标视图。</param>
    private Task NotifyActiveViewsNavigatingToAsync(IRegion region, INavigationContext context, IView view)
    {
        if (view.ViewModel is not INavigationAware aware)
        {
            return Task.CompletedTask;
        }

        return InvokeNavigationCallbackAsync(
            region,
            view,
            nameof(INavigationAware.OnNavigatedToAsync),
            () => aware.OnNavigatedToAsync(context)
        );
    }

    /// <summary>
    /// 调用一个导航生命周期回调，失败时记录视图名与阶段后重抛。
    /// </summary>
    /// <param name="region">目标区域。</param>
    /// <param name="view">回调所属的视图。</param>
    /// <param name="stage">回调名称，用于日志。</param>
    /// <param name="callback">要调用的回调。</param>
    /// <remarks>
    /// 日志在通知处记录而不是在最外层 <c>catch</c>：外层只能拿到异常，
    /// 不知道是哪个视图、哪个阶段失败的。
    /// </remarks>
    private async Task InvokeNavigationCallbackAsync(
        IRegion region,
        IView view,
        string stage,
        Func<Task> callback
    )
    {
        try
        {
            await callback();
        }
        catch (Exception ex)
        {
            Logger?.LogError(
                ex,
                "导航失败：视图 {ViewName}（区域 {RegionName}）的 {Stage} 抛出异常。",
                view.ViewName,
                region.Name,
                stage
            );

            throw;
        }
    }

    private async ValueTask<INavigationResult> NavigateInRegionAsync(
        IRegion region,
        Uri target,
        IParameters navigationParameters,
        CancellationToken cancellationToken
    )
    {
        var context = new NavigationContext(target, navigationParameters);

        // 解析结果在 try 外声明：失败分支需要知道本次是否新建了视图，才能决定是否回收。
        var resolved = default(ResolvedView);

        try
        {
            // 必须在解析之前记录活动视图：区域适配器可能在 Add 时自动激活新视图
            // （ContentControlRegionAdapter 在区域为空时就这么做），
            // 解析之后再取会把新视图自己算成"即将离开"，让它先收到 OnNavigatedFrom。
            // 这里记录的是"本次导航开始前的状态"，与适配器是否自动激活无关。
            var previousActive = region.ActiveViews.ToArray();

            // 先解析目标视图：解析失败时不应让当前活动视图收到 OnNavigatedFrom，
            // 否则视图模型会认为自己已经离开，而界面仍显示它。
            resolved = await _viewResolver.ResolveAsync(region, context);
            var view = resolved.View;

            // 参数校验同样放在激活之前：失败时界面状态保持不变。
            NavigationParameterValidator.Validate(view, context);

            // 这是唯一能干净退出的取消检查点：区域尚未激活任何视图，旧视图也未收到离开通知。
            // 一旦开始通知 OnNavigatedFrom，旧视图就已经"认为"自己离开，
            // 此时再抛取消会留下"已通知离开但仍处于激活状态"的不一致，因此之后不再检查。
            //
            // 新建的视图此时已加入区域但未激活，必须回收：作用域视图不会复用
            // （见 NavigationViewResolver.IsNonKeepAliveScoped），留着只会随每次失败累积。
            cancellationToken.ThrowIfCancellationRequested();

            await NotifyActiveViewsNavigatingFromAsync(region, context, previousActive);

            if (await ActivateAndNotifyAsync(region, context, view, previousActive) is { } failure)
            {
                return failure;
            }

            // 只有完整成功才记入历史：失败或取消的导航不应出现在回退路径上。
            // 回退产生的导航由 NavigationHistory 自行忽略。
            _history.Push(
                region.Name,
                new NavigationHistory.HistoryEntry(view.ViewName, context.Parameters)
            );

            return new NavigationResult(context, true);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // 取消是唯一穿透的异常：调用方需要区分"被取消"与"导航失败"。
            // when 过滤不能省：视图模型自己抛出的 OperationCanceledException
            // （例如它内部的超时）仍应保持既有行为，包装成失败结果。
            ReclaimUnactivatedView(region, resolved);
            throw;
        }
        catch (Exception ex)
        {
            // 解析成功但校验失败时，新建的视图已加入区域却从未激活，必须回收。
            ReclaimUnactivatedView(region, resolved);
            return new NavigationResult(context, ex);
        }
    }

    /// <summary>
    /// 激活目标视图并通知它已进入，失败时回滚到导航前的状态。
    /// </summary>
    /// <param name="region">目标区域。</param>
    /// <param name="context">导航上下文。</param>
    /// <param name="view">目标视图。</param>
    /// <param name="previousActive">导航前的活动视图。</param>
    /// <returns>激活通知失败时的失败结果；成功时返回 <see langword="null"/>。</returns>
    /// <remarks>
    /// <para>
    /// 延迟拆除的开启、提交与取消都收在这里：调用方只需要知道"成功"或"失败"，
    /// 不必关心延迟状态何时结束。漏掉任何一个结束点，后续导航都会一直累积待拆除视图。
    /// </para>
    /// <para>
    /// 激活或提交本身抛异常时照常向上抛：那种情况不属于"导航失败"，
    /// 而是框架状态出了问题，调用方需要看到真实异常。
    /// </para>
    /// </remarks>
    private async ValueTask<INavigationResult?> ActivateAndNotifyAsync(
        IRegion region,
        INavigationContext context,
        IView view,
        IView[] previousActive
    )
    {
        // 延迟拆除旧视图：激活会让它们失活，若此时立即拆除，回滚就没有可恢复的实例。
        // 成功则提交（真正拆除），失败则取消（旧视图原样留在区域中）。
        var deferrable = AsDeferrable(region);

        deferrable?.BeginDeferral();

        try
        {
            region.Activate(view);

            try
            {
                await NotifyActiveViewsNavigatingToAsync(region, context, view);
            }
            catch (Exception ex)
            {
                // 激活是不可逆的，但通知失败必须让状态与返回值一致：
                // 调用方看到失败，界面就应回到导航前的样子。
                deferrable?.CancelDeferral();
                Rollback(region, view, previousActive);
                return new NavigationResult(context, ex);
            }

            deferrable?.CommitDeferral();

            return null;
        }
        catch
        {
            // 激活或提交本身抛异常时同样要结束延迟状态，否则后续导航会一直累积待拆除视图。
            deferrable?.CancelDeferral();
            throw;
        }
    }

    /// <summary>
    /// 回收一次"已解析但未激活"的导航所新建的视图。
    /// </summary>
    /// <param name="region">目标区域。</param>
    /// <param name="resolved">解析结果；未解析成功时 <see cref="ResolvedView.IsNew"/> 为默认值。</param>
    /// <remarks>
    /// <para>
    /// 只回收本次新建的视图：复用的视图属于之前的导航，移除它会破坏
    /// "界面回到导航前"的语义。
    /// </para>
    /// <para>
    /// 不保活的作用域视图不会被后续导航复用（见
    /// <see cref="NavigationViewResolver"/>），留着只会随每次失败累积，
    /// 连同其导航作用域一起泄漏。
    /// </para>
    /// <para>
    /// 回收本身失败不能覆盖调用方拿到的原始异常，只记录日志。
    /// </para>
    /// </remarks>
    private void ReclaimUnactivatedView(IRegion region, ResolvedView resolved)
    {
        if (!resolved.IsNew || !region.Views.Contains(resolved.View))
        {
            return;
        }

        try
        {
            region.Remove(resolved.View);
        }
        catch (Exception ex)
        {
            Logger?.LogError(
                ex,
                "回收未激活的视图 {ViewName}（区域 {RegionName}）失败，区域可能残留该视图。",
                resolved.View.ViewName,
                region.Name
            );
        }
    }

    /// <summary>
    /// 取区域的延迟拆除能力；区域实现不支持时返回 <see langword="null"/>。
    /// </summary>
    /// <param name="region">目标区域。</param>
    /// <returns>支持延迟拆除的区域；否则返回 <see langword="null"/>。</returns>
    /// <remarks>
    /// 只有框架自带的 <see cref="Region"/> 支持延迟拆除。使用者自定义的
    /// <see cref="IRegion"/> 实现没有该能力，此时退化为"激活即拆除"——
    /// 回滚可能无法恢复旧视图，但不影响导航本身。
    /// </remarks>
    private static Region? AsDeferrable(IRegion region) => region as Region;
    /// <summary>
    /// 回滚一次已激活但激活通知失败的导航。
    /// </summary>
    /// <param name="region">目标区域。</param>
    /// <param name="view">已激活的新视图。</param>
    /// <param name="previousActive">导航前的活动视图。</param>
    /// <remarks>
    /// <para>
    /// 回滚只恢复区域状态，不重新通知旧视图 <c>OnNavigatedToAsync</c>——它从未真正离开。
    /// 新视图的 <c>OnNavigatedFromAsync</c> 也不通知：它的 <c>OnNavigatedToAsync</c> 已经失败，
    /// 视图模型没有进入"已激活"状态，再通知离开会让状态机更混乱。
    /// </para>
    /// <para>
    /// <b>新视图若原本就是活动视图，不能失活它。</b>重复导航到当前视图时
    /// <paramref name="view"/> 与 <paramref name="previousActive"/> 是同一个实例，
    /// 调用方此时已取消延迟拆除，<c>Deactivate</c> 会立即触发拆除——
    /// 被拆掉的正是要恢复的那个实例，结果是"导航失败"把用户正在看的界面清空。
    /// 这种情况什么都不用做：视图本来就处于导航前的状态。
    /// </para>
    /// <para>
    /// 旧视图能被恢复，是因为调用方在激活前开启了延迟拆除：失活只登记、不释放，
    /// 取消延迟后它们原样留在区域中。若区域实现不支持延迟拆除，旧视图可能已被移除，
    /// 此时跳过恢复并记录日志。
    /// </para>
    /// <para>
    /// 回滚本身可能失败，此时只记录日志，不改变调用方拿到的原始异常。
    /// </para>
    /// </remarks>
    private void Rollback(IRegion region, IView view, IView[] previousActive)
    {
        try
        {
            // 目标视图就是导航前的活动视图：它从未真正离开，失活只会触发拆除。
            if (!previousActive.Contains(view) && region.Views.Contains(view))
            {
                region.Deactivate(view);
            }

            foreach (var previous in previousActive)
            {
                // 旧视图可能已因保活策略被移出区域，此时无法恢复，跳过即可。
                if (region.Views.Contains(previous))
                {
                    region.Activate(previous);
                }
            }
        }
        catch (Exception rollbackException)
        {
            // 回滚失败不能覆盖原始异常：调用方需要知道真正的失败原因。
            Logger?.LogError(
                rollbackException,
                "回滚视图 {viewName} 的导航失败，区域 {regionName} 可能处于不一致状态。",
                view.ViewName,
                region.Name
            );
        }
    }
}
