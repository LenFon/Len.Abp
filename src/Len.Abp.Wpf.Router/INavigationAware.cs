namespace Len.Abp.Wpf.Router;

/// <summary>
/// Provides a way for objects involved in navigation to be notified of navigation activities.
/// </summary>
public interface INavigationAware : INavigatedToAware, INavigatedFromAware, IRefreshAware { }
