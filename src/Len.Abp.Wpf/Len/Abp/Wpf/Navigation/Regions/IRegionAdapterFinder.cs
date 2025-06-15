using Volo.Abp.DependencyInjection;

namespace Len.Abp.Wpf.Navigation.Regions;

public interface IRegionAdapterFinder : ISingletonDependency
{
    IRegionAdapter FindByControlType(Type controlType);
}