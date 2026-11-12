using Len.Abp.Wpf.Extensions;
using Len.Abp.Wpf.Mvvm;
using System.Windows;

namespace Len.Abp.Wpf.Navigation.Regions;

/// <summary>
/// <see cref="IRegionBuilder"/> 的默认实现，在控件加载后为其创建区域。
/// </summary>
internal sealed class RegionBuilder(IRegionAdapterFinder regionAdapterFinder) : IRegionBuilder
{
    private readonly IRegionAdapterFinder _regionAdapterFinder = regionAdapterFinder;
    private WeakReference? _elementWeakReference;

    private DependencyObject? TargetElement => _elementWeakReference?.Target as DependencyObject;

    /// <inheritdoc/>
    public void Build(DependencyObject element)
    {
        _elementWeakReference = new WeakReference(element);

        if (TargetElement is FrameworkElement frameworkElement)
        {
            frameworkElement.Loaded += OnElementLoaded;
        }
    }

    private static IView GetOwnerView(DependencyObject targetElement) =>
        (targetElement as IView) ?? targetElement.GetParent<IView>()
        ?? throw new ArgumentException("无法确定目标元素的视图名称。请确认该元素位于某个视图内。");

    private void CreateRegion(DependencyObject targetElement, string regionName)
    {
        try
        {
            var regionAdapter = _regionAdapterFinder.FindByControlType(targetElement.GetType());
            var ownerView = GetOwnerView(targetElement);

            // 创建出的区域通过 RegionProviderRegistrationBehavior 自行完成注册。
            regionAdapter.Initialize(targetElement, regionName, ownerView);
        }
        catch (Exception ex)
        {
            throw new RegionCreationException(regionName, ex);
        }
    }

    private void OnElementLoaded(object sender, RoutedEventArgs e)
    {
        // 先解绑再创建：Loaded 只处理一次，无需额外的“已创建”标志。
        if (TargetElement is FrameworkElement element)
        {
            element.Loaded -= OnElementLoaded;
        }

        if (TargetElement is not { } targetElement || !targetElement.CheckAccess())
        {
            return;
        }

        CreateRegion(targetElement, Navigation.Region.GetName(targetElement));
    }
}