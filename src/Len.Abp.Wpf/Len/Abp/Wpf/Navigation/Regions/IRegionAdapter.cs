namespace Len.Abp.Wpf.Navigation.Regions;

public interface IRegionAdapter
{
    IRegion Initialize(object regionTarget, string regionName);
}