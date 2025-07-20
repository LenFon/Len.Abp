using Len.Abp.Wpf.Mvvm;
using System.Windows;
using System.Windows.Media;

namespace Len.Abp.Wpf.Navigation.Regions;

internal class RegionBulider(IRegionAdapterFinder regionAdapterFinder) : IRegionBulider
{
    private readonly IRegionAdapterFinder _regionAdapterFinder = regionAdapterFinder;
    private WeakReference? _elementWeakReference;
    private bool _regionCreated;

    private DependencyObject? TargetElement => _elementWeakReference is not null ? _elementWeakReference.Target as DependencyObject : null;

    public void Bulid(DependencyObject element)
    {
        _elementWeakReference = new WeakReference(element);

        WireUpTargetElement();
    }

    private static string GetViewName(DependencyObject targetElement)
    {
        var dp = targetElement;

        while (dp is not null)
        {
            if (dp is IView view)
            {
                return view.ViewName;
            }

            dp = VisualTreeHelper.GetParent(dp);
        }

        throw new InvalidOperationException("Unable to determine the view name for the target element. Ensure that the target element is part of a view.");
    }

    private void CreateRegion(DependencyObject targetElement, string regionName)
    {
        try
        {
            var regionAdapter = _regionAdapterFinder.FindByControlType(targetElement.GetType());
            var viewName = GetViewName(targetElement);
            var region = regionAdapter.Initialize(targetElement, regionName, viewName);
        }
        catch (Exception ex)
        {
            throw new RegionCreationException($"An exception occurred while creating a region with name '{regionName}'. The exception was: {ex}. ", ex);
        }
    }

    private void OnElementLoaded(object sender, RoutedEventArgs e)
    {
        UnWireTargetElement();
        TryCreateRegion();
    }

    private void TryCreateRegion()
    {
        var targetElement = TargetElement;

        if (targetElement is null) return;

        if (targetElement.CheckAccess())
        {
            if (!_regionCreated)
            {
                var regionName = Navigation.Region.GetName(targetElement);
                CreateRegion(targetElement, regionName);
                _regionCreated = true;
            }
        }
    }

    private void UnWireTargetElement()
    {
        if (TargetElement is FrameworkElement element)
        {
            element.Loaded -= OnElementLoaded;
        }
    }

    private void WireUpTargetElement()
    {
        if (TargetElement is FrameworkElement element)
        {
            element.Loaded += OnElementLoaded;
        }
    }
}