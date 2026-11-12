using Len.Abp.Wpf.Mvvm;
using Len.Abp.Wpf.Navigation.Regions;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.DependencyInjection;

namespace Len.Abp.Wpf.Navigation;

/// <summary>
/// <see cref="INavigationService"/> 的默认实现，负责解析视图、切换区域活动视图并通知导航生命周期。
/// </summary>
internal class NavigationService(IRegionFinder regionFinder, IServiceProvider serviceProvider) : INavigationService, ISingletonDependency
{
    private readonly IRegionFinder _regionFinder = regionFinder;
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    /// <summary>
    /// 导航到指定名称的视图。
    /// </summary>
    /// <param name="regionName">目标区域名称。</param>
    /// <param name="viewName">目标视图名称。</param>
    /// <param name="navigationParameters">导航参数。</param>
    /// <returns>导航结果。</returns>
    public async ValueTask<INavigationResult> NavigateAsync(
        string regionName,
        string viewName,
        IParameters navigationParameters
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(viewName, nameof(viewName));

        return await NavigateAsync(regionName, new Uri(viewName, UriKind.RelativeOrAbsolute), navigationParameters);
    }

    /// <summary>
    /// 导航到指定 URI 对应的视图。
    /// </summary>
    /// <param name="regionName">目标区域名称。</param>
    /// <param name="target">目标 URI。</param>
    /// <param name="navigationParameters">导航参数。</param>
    /// <returns>导航结果。</returns>
    public async ValueTask<INavigationResult> NavigateAsync(
        string regionName,
        Uri target,
        IParameters navigationParameters
    )
    {
        var region = _regionFinder.FindRegion(regionName);
        if (region is not null)
        {
            return await NavigateAsync(region, target, navigationParameters);
        }
        else
        {
            return new NavigationResult(new NavigationContext(target, navigationParameters), false);
        }
    }

    private static async Task NotifyActiveViewsNavigatingFromAsync(INavigationContext context, IView[] views)
    {
        foreach (var activeView in views)
        {
            if (activeView.ViewModel is INavigationAware aware)
            {
                await aware.OnNavigatedFromAsync(context);
            }
        }
    }

    private static async Task NotifyActiveViewsNavigatingToAsync(INavigationContext context, IView view)
    {
        if (view.ViewModel is INavigationAware aware)
        {
            await aware.OnNavigatedToAsync(context);
        }
    }

    private async ValueTask<IView> LoadContentAsync(IRegion region, NavigationContext context)
    {
        var viewName = NavigationUri.GetViewName(context.Uri);
        IView? view = null;
        foreach (var item in region.Views)
        {
            if (item.ViewName != viewName)
            {
                continue;
            }

            // 视图名匹配是复用的前提；INavigationAware 只是额外的“是否接受本次导航”判断。
            // 缺少该判断时，普通视图每次导航都会解析出新实例并累积在区域中。
            if (item.ViewModel is INavigationAware aware && !await aware.IsNavigationTargetAsync(context))
            {
                continue;
            }

            view = item;
            break;
        }

        if (view is not null)
        {
            return view;
        }

        view = _serviceProvider.GetRequiredKeyedService<IView>(viewName);
        region.Add(view);

        return view;
    }

    private async ValueTask<INavigationResult> NavigateAsync(
        IRegion region,
        Uri target,
        IParameters navigationParameters
    )
    {
        var context = new NavigationContext(target, navigationParameters);

        try
        {
            // 先解析目标视图：解析失败时不应让当前活动视图收到 OnNavigatedFrom，
            // 否则视图模型会认为自己已经离开，而界面仍显示它。
            var view = await LoadContentAsync(region, context);

            await NotifyActiveViewsNavigatingFromAsync(context, [.. region.ActiveViews]);

            region.Activate(view);

            await NotifyActiveViewsNavigatingToAsync(context, view);

            return new NavigationResult(context, true);
        }
        catch (Exception ex)
        {
            return new NavigationResult(context, ex);
        }
    }
}