using System.Windows;

namespace Len.Abp.Wpf.Navigation.Regions.Behaviors;

internal class DelayedRegionCreationBehavior(IRegionAdapterFinder regionAdapterFinder) : IDelayedRegionCreationBehavior
{
    private readonly IRegionAdapterFinder _regionAdapterFinder = regionAdapterFinder;
    private WeakReference? _elementWeakReference;
    private bool _regionCreated;

    public DependencyObject? TargetElement
    {
        get => _elementWeakReference is not null ? _elementWeakReference.Target as DependencyObject : null;
        set => _elementWeakReference = new WeakReference(value);
    }

    public void Attach()
    {
        WireUpTargetElement();
    }

    public void Detach()
    {
        UnWireTargetElement();
    }

    private void CreateRegion(DependencyObject targetElement, string regionName)
    {
        try
        {
            var regionAdapter = _regionAdapterFinder.FindByControlType(targetElement.GetType());
            var region = regionAdapter.Initialize(targetElement, regionName);
        }
        catch (Exception ex)
        {
            throw new RegionCreationException($"An exception occurred while creating a region with name '{regionName}'. The exception was: {ex}. ", ex);
        }
        throw new NotImplementedException();
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
            Detach();

            if (!_regionCreated)
            {
                var regionName = Navigation.GetRegionName(targetElement);
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