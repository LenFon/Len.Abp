using Len.Abp.Wpf.Extensions;
using Len.Abp.Wpf.Mvvm;
using System.Windows;

namespace Len.Abp.Wpf.Navigation.Regions;

internal sealed class RegionBulider(IRegionAdapterFinder regionAdapterFinder) : IRegionBulider
{
    private readonly IRegionAdapterFinder _regionAdapterFinder = regionAdapterFinder;
    private WeakReference? _elementWeakReference;
    private bool _regionCreated;

    private DependencyObject? TargetElement => _elementWeakReference?.Target as DependencyObject;

    public void Bulid(DependencyObject element)
    {
        _elementWeakReference = new WeakReference(element);

        WireUpTargetElement();
    }

    private static string GetViewName(DependencyObject targetElement)
    {
        var view = (targetElement as IView) ?? targetElement.GetParent<IView>();

        return view is null
            ? throw new ArgumentException(
                "Unable to determine the view name for the target element. Ensure that the target element is part of a view."
            )
            : view.ViewName;
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
            throw new RegionCreationException(regionName, ex);
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

        if (targetElement is null || _regionCreated || !targetElement.CheckAccess())
            return;

        var regionName = Navigation.Region.GetName(targetElement);
        CreateRegion(targetElement, regionName);
        _regionCreated = true;
    }

    private void UnWireTargetElement()
    {
        if (TargetElement is not FrameworkElement element)
            return;

        element.Loaded -= OnElementLoaded;
    }

    private void WireUpTargetElement()
    {
        if (TargetElement is not FrameworkElement element)
            return;

        element.Loaded += OnElementLoaded;
    }
}