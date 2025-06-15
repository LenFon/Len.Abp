namespace Len.Abp.Wpf.Navigation.Regions;

public interface IRegionFinder
{
    IRegion? FindRegion(string regionName);
}