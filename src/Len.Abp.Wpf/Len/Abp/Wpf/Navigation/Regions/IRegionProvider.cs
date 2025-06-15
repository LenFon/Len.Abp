namespace Len.Abp.Wpf.Navigation.Regions;

public interface IRegionProvider
{
    IReadOnlyList<IRegion> Regions { get; }

    void Add(IRegion region);

    void RemoveByViewName(string viewName);
}