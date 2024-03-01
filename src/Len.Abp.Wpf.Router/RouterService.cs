using System.Windows;
using Len.Abp.Wpf.Mvvm;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace Len.Abp.Wpf.Router;

/// <summary>
/// Provides methods to navigate between pages in the application.
/// </summary>
/// <param name="routerOptions"></param>
/// <param name="serviceProvider"></param>
internal class RouterService(IOptions<RouterOptions> routerOptions, IServiceProvider serviceProvider)
    : IRouterService,
        ISingletonDependency
{
    private readonly Stack<INavigationJournalEntry> _backStack = [];
    private readonly Stack<INavigationJournalEntry> _forwardStack = [];
    private readonly IOptions<RouterOptions> _routerOptions = routerOptions;
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    /// <inheritdoc/>
    public bool CanGoBack => _backStack.Count > 0;

    /// <inheritdoc/>
    public bool CanGoForward => _forwardStack.Count > 0;

    /// <inheritdoc/>
    public INavigationJournalEntry? CurrentEntry { get; private set; }

    /// <inheritdoc/>
    public bool GoBack()
    {
        if (!CanGoBack)
            return false;
        if (CurrentEntry is null)
            return false;

        var journalEntry = _backStack.Peek();
        var previousCurrentEntry = CurrentEntry;

        NavigateTo(journalEntry.Path, journalEntry.Parameters);

        _forwardStack.Push(previousCurrentEntry);
        _backStack.Pop();
        _backStack.Pop();

        return true;
    }

    /// <inheritdoc/>
    public bool GoForward()
    {
        if (!CanGoForward)
            return false;

        var journalEntry = _forwardStack.Peek();

        NavigateTo(journalEntry.Path, journalEntry.Parameters);

        _forwardStack.Pop();

        return true;
    }

    /// <inheritdoc/>
    public void NavigateTo(string url) => NavigateTo(url, IParameters.Create());

    /// <inheritdoc/>
    public void NavigateTo(string url, IParameters parameters)
    {
        var routerShell = _serviceProvider.GetRequiredService<IRouterShell>();
        var routerViews = GetRouterViews(routerShell);
        var context = new NavigationContext(parameters, CurrentEntry?.Path, url);

        _routerOptions.Value.BeforeEach(
            context,
            newUrl =>
            {
                context = new(context.Parameters, CurrentEntry?.Path, newUrl);

                NavigateTo(newUrl, _routerOptions.Value.Routes, routerViews, context);

                var navigationJournalEntry = new NavigationJournalEntry(url, parameters);

                RecordNavigation(navigationJournalEntry);

                _routerOptions.Value.AfterEach?.Invoke(context);
            }
        );
    }

    /// <summary>
    /// Call the <see cref="INavigatedFromAware.OnNavigatedFrom"/> method on the specified content and its children.
    /// </summary>
    /// <param name="content">The content to call the method on.</param>
    /// <param name="context">The navigation context.</param>
    private static void CallRouterViewNavigatedFromAction(object? content, INavigationContext context)
    {
        var routerViews = GetRouterViews(content);

        foreach (var routerView in routerViews)
        {
            CallViewAndViewModelAction<INavigatedFromAware>(
                routerView.Content,
                aware => aware.OnNavigatedFrom(context)
            );
            CallRouterViewNavigatedFromAction(routerView, context);
        }
    }

    /// <summary>
    /// Call the specified action on the view and view model.
    /// </summary>
    /// <typeparam name="T">The type of the interface to call.</typeparam>
    /// <param name="view">The view to call the action on.</param>
    /// <param name="action">The action to call.</param>
    private static void CallViewAndViewModelAction<T>(object? view, Action<T> action)
        where T : class
    {
        if (view is T viewAsT)
            action(viewAsT);

        if (view is FrameworkElement { DataContext: T viewModelAsT } and not IView)
        {
            action(viewModelAsT);
        }
    }

    /// <summary>
    /// Get the router views from the specified control.
    /// </summary>
    /// <param name="control">The control to get the router views from.</param>
    /// <returns>The list of router views.</returns>
    private static List<RouterView> GetRouterViews(object? control)
    {
        List<RouterView> routerViews = [];

        if (control is FrameworkElement frameworkElement)
        {
            foreach (var item in LogicalTreeHelper.GetChildren(frameworkElement))
            {
                if (item is RouterView routerView)
                {
                    routerViews.Add(routerView);

                    continue;
                }

                routerViews.AddRange(GetRouterViews(item));
            }
        }

        return routerViews;
    }

    /// <summary>
    /// Navigate to the specified URL.
    /// </summary>
    /// <param name="targetUrl">The URL to navigate to.</param>
    /// <param name="routes">The routes to use for navigation.</param>
    /// <param name="routerViews">The router views to use for navigation.</param>
    /// <param name="context">The navigation context.</param>
    private void NavigateTo(
        string targetUrl,
        IEnumerable<Route> routes,
        IEnumerable<RouterView> routerViews,
        INavigationContext context
    )
    {
        var path = targetUrl == "/" ? "" : targetUrl.Split("/", StringSplitOptions.RemoveEmptyEntries)[0];
        var targetRoute = routes.FirstOrDefault(route => route.Path.Trim('/') == path);
        var newRouterViews = new List<RouterView>();

        foreach (var routerView in routerViews)
        {
            if (targetRoute?.GetComponents().TryGetValue(routerView.RouterName, out var contentType) == true)
            {
                if (routerView.Content?.GetType() == contentType)
                {
                    CallViewAndViewModelAction<IRefreshAware>(routerView.Content, aware => aware.OnRefresh(context));
                }
                else
                {
                    CallViewAndViewModelAction<INavigatedFromAware>(
                        routerView.Content,
                        aware => aware.OnNavigatedFrom(context)
                    );

                    CallRouterViewNavigatedFromAction(routerView.Content, context);

                    routerView.Content = _serviceProvider.GetService(contentType);

                    CallViewAndViewModelAction<INavigatedToAware>(
                        routerView.Content,
                        aware => aware.OnNavigatedTo(context)
                    );
                }
            }
            else
            {
                CallViewAndViewModelAction<INavigatedFromAware>(
                    routerView.Content,
                    aware => aware.OnNavigatedFrom(context)
                );

                routerView.Content = null;
            }

            newRouterViews.AddRange(GetRouterViews(routerView.Content));
        }

        var newPath = targetUrl.ReplaceFirst($"/{path}", "");
        var newRoutes = targetRoute?.Children;

        if (!string.IsNullOrEmpty(newPath) && newRoutes?.Count > 0 && newRouterViews.Count > 0)
        {
            NavigateTo(newPath, newRoutes ?? [], newRouterViews, context);
        }
    }

    /// <summary>
    /// Record the navigation.
    /// </summary>
    /// <param name="entry">The navigation journal entry.</param>
    private void RecordNavigation(INavigationJournalEntry entry)
    {
        if (CurrentEntry is not null)
            _backStack.Push(CurrentEntry);

        CurrentEntry = entry;
    }
}
