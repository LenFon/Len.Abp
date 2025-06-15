namespace Len.Abp.Wpf.Navigation.Regions;

public interface IRegionBehavior
{
    IRegion Region { get; set; }

    void Attach();
}