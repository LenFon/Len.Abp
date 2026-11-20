using Len.Abp.Wpf.Mvvm;
using Len.Abp.Wpf.Navigation.Regions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace Len.Abp.Wpf.Navigation;

/// <summary>
/// 解析导航目标视图：优先复用区域中接受本次导航的同名视图，否则从容器解析并加入区域。
/// </summary>
/// <remarks>
/// <para>
/// 新解析的视图来自一次导航专属的 <see cref="IServiceScope"/>，因此 <c>Scoped</c> 视图
/// 每次导航都会得到新实例。作用域与视图一起登记到 <see cref="IViewScopeRegistry"/>，
/// 由区域行为在拆除视图时释放。
/// </para>
/// <para>
/// 非保活的作用域视图不参与复用：它的语义就是"每次导航一个新实例"，
/// 复用区域中的旧实例会让作用域无法释放，也与 <c>Scoped</c> 的预期不符。
/// 保活（<see cref="IRegionMemberLifetime.KeepAlive"/>）与单例仍按原规则复用。
/// </para>
/// </remarks>
internal sealed class NavigationViewResolver(IServiceProvider serviceProvider, IViewScopeRegistry viewScopeRegistry)
    : ISingletonDependency
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly IViewScopeRegistry _viewScopeRegistry = viewScopeRegistry;

    private ILogger<NavigationViewResolver>? Logger =>
        _serviceProvider.GetService<ILogger<NavigationViewResolver>>();

    /// <summary>
    /// 解析目标视图。
    /// </summary>
    /// <param name="region">目标区域。</param>
    /// <param name="context">导航上下文。</param>
    /// <returns>目标视图，以及它是否由本次导航新建。</returns>
    /// <exception cref="InvalidOperationException">容器无法解析该视图时抛出。</exception>
    /// <remarks>
    /// 返回值必须带上"是否新建"：新建的视图在后续校验、取消或激活通知失败时
    /// 需要从区域移除并释放作用域，复用的视图则必须原样保留。
    /// </remarks>
    public async ValueTask<ResolvedView> ResolveAsync(IRegion region, INavigationContext context)
    {
        var viewName = NavigationUri.GetViewName(context.Uri);

        if (await FindReusableAsync(region, context, viewName) is { } reusable)
        {
            return new ResolvedView(reusable, IsNew: false);
        }

        return Create(region, viewName);
    }

    /// <summary>
    /// 在区域中查找可复用的同名视图。
    /// </summary>
    /// <param name="region">目标区域。</param>
    /// <param name="context">导航上下文。</param>
    /// <param name="viewName">目标视图名称。</param>
    /// <returns>可复用的视图；没有时返回 <see langword="null"/>。</returns>
    private static async ValueTask<IView?> FindReusableAsync(IRegion region, INavigationContext context, string viewName)
    {
        foreach (var item in region.Views)
        {
            if (item.ViewName != viewName)
            {
                continue;
            }

            // 非保活的作用域视图每次导航都应新建，不能复用区域中的旧实例。
            if (IsNonKeepAliveScoped(item))
            {
                continue;
            }

            // 视图名匹配是复用的前提；INavigationAware 只是额外的“是否接受本次导航”判断。
            // 视图拒绝时继续查找下一个同名视图，都不接受才解析新实例。
            if (item.ViewModel is INavigationAware aware && !await aware.IsNavigationTargetAsync(context))
            {
                continue;
            }

            return item;
        }

        return null;
    }

    /// <summary>
    /// 从容器解析新视图并加入区域。
    /// </summary>
    /// <param name="region">目标区域。</param>
    /// <param name="viewName">目标视图名称。</param>
    /// <returns>解析出的视图，以及它是否由本次调用新建。</returns>
    /// <remarks>
    /// 容器返回的实例可能已经在区域中：单例视图被拒绝复用时，解析到的仍是同一实例。
    /// 此时不能再 <see cref="IRegion.Add"/>（会抛"该视图已存在于区域中"），
    /// 也不能声称"新建"——调用方据此决定失败时是否回收，回收一个复用的实例会误伤。
    /// </remarks>
    private ResolvedView Create(IRegion region, string viewName)
    {
        // 每次导航一个作用域：Scoped 视图与它依赖的 Scoped 服务都在这个作用域内解析。
        // 作用域随视图一起交给注册表，视图被拆除时释放。
        var scope = _serviceProvider.CreateScope();

        IView view;

        try
        {
            view = scope.ServiceProvider.GetRequiredKeyedService<IView>(viewName);
        }
        catch (Exception ex)
        {
            // 解析失败时作用域没有被任何视图持有，必须在这里释放，否则泄漏。
            scope.Dispose();

            // 容器原生异常只提到服务类型，不含视图名。补上视图名与区域名，
            // 使用者才能区分"ViewName 拼错""忘记加 [View]""视图未注册"。
            Logger?.LogError(
                ex,
                "导航失败：无法从容器解析视图 {ViewName}（区域 {RegionName}）。",
                viewName,
                region.Name
            );

            throw new InvalidOperationException(
                $"无法解析视图 \"{viewName}\"（区域 \"{region.Name}\"）。"
                    + "请确认该视图已标注 [View] 且 ViewName 正确。",
                ex
            );
        }

        // 单例视图被拒绝复用时容器返回同一实例，它已在区域中。
        // 复用该实例而不是重复加入：单例语义下无法通过新建实例满足"拒绝导航"。
        if (region.Views.Contains(view))
        {
            scope.Dispose();

            // Debug 通常被关闭，先判断级别，避免无谓地求值参数。
            if (Logger is { } logger && logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug(
                    "视图 {ViewName}（区域 {RegionName}）已存在于区域中，复用该实例。",
                    viewName,
                    region.Name
                );
            }

            return new ResolvedView(view, IsNew: false);
        }

        _viewScopeRegistry.Attach(view, scope);
        region.Add(view);

        return new ResolvedView(view, IsNew: true);
    }

    /// <summary>
    /// 判断视图是否为"不保活的作用域视图"。
    /// </summary>
    /// <param name="view">待判断的视图。</param>
    /// <returns>是作用域视图且未声明保活时返回 <see langword="true"/>。</returns>
    private static bool IsNonKeepAliveScoped(IView view)
    {
        var isScoped = view is IScopedDependency || view.ViewModel is IScopedDependency;

        if (!isScoped)
        {
            return false;
        }

        return view.ViewModel is not IRegionMemberLifetime { KeepAlive: true };
    }
}

/// <summary>
/// 视图解析结果。
/// </summary>
/// <param name="View">解析出的视图。</param>
/// <param name="IsNew">视图是否由本次导航新建并加入区域。</param>
/// <remarks>
/// 导航失败时只有新建的视图需要回收：复用的视图属于之前的导航，
/// 移除它会破坏调用方看到的"界面回到导航前"语义。
/// </remarks>
internal readonly record struct ResolvedView(IView View, bool IsNew);
