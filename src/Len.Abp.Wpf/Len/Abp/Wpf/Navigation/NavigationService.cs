using Len.Abp.Wpf.Mvvm;
using Len.Abp.Wpf.Navigation.Regions;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.DependencyInjection;

namespace Len.Abp.Wpf.Navigation;

internal class NavigationService(IRegionFinder regionFinder, IServiceProvider serviceProvider) : INavigationService, ISingletonDependency
{
    private readonly IRegionFinder _regionFinder = regionFinder;
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public async ValueTask<INavigationResult> NavigateAsync(
        string regionName,
        string viewName,
        IParameters navigationParameters
    )
    {
        ArgumentException.ThrowIfNullOrEmpty(viewName, nameof(viewName));

        return await NavigateAsync(regionName, new Uri(viewName, UriKind.RelativeOrAbsolute), navigationParameters);
    }

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

    private static Uri EnsureAbsolute(Uri uri)
    {
        if (uri.IsAbsoluteUri)
        {
            return uri;
        }

        if ((uri != null) && !uri.OriginalString.StartsWith('/'))
        {
            return new Uri("http://localhost/" + uri, UriKind.Absolute);
        }
        return new Uri("http://localhost" + uri, UriKind.Absolute);
    }

    private static string GetAbsolutePath(Uri uri)
    {
        return EnsureAbsolute(uri).AbsolutePath;
    }

    private static string GetViewName(Uri uri) => GetAbsolutePath(uri).TrimStart('/');

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

    private async ValueTask<IView> LoadContentAsync(IRegion region, INavigationContext context)
    {
        var viewName = GetViewName(context.Uri);
        IView? view = null;
        foreach (var item in region.Views)
        {
            if (item.ViewName == viewName && item.ViewModel is INavigationAware aware && await aware.IsNavigationTargetAsync(context))
            {
                view = item;
                break;
            }
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
            await NotifyActiveViewsNavigatingFromAsync(context, [.. region.ActiveViews]);

            var view = await LoadContentAsync(region, context);

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