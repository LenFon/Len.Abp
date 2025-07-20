using Len.Abp.Wpf.Mvvm;

namespace Len.Abp.Wpf.Navigation.Regions;

public class SingleActiveRegion(string name, string owner) : Region(name, owner)
{
    public override void Activate(IView view)
    {
        var currentActiveView = ActiveViews.FirstOrDefault();

        if (currentActiveView != null && currentActiveView != view && Views.Contains(currentActiveView))
        {
            base.Deactivate(currentActiveView);
        }

        base.Activate(view);
    }
}