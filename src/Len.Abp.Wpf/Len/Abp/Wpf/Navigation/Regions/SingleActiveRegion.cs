using Len.Abp.Wpf.Mvvm;

namespace Len.Abp.Wpf.Navigation.Regions;

/// <summary>
/// 同一时刻只允许一个视图处于激活状态的区域。
/// </summary>
/// <param name="name">区域名称。</param>
/// <param name="owner">区域所属的视图名称。</param>
public class SingleActiveRegion(string name, string owner) : Region(name, owner)
{
    /// <inheritdoc/>
    public override void Activate(IView view)
    {
        // 先验证目标视图属于本区域，避免失活旧视图后才发现目标无效，留下不一致状态。
        FindMetadata(view);

        var currentActiveView = ActiveViews.FirstOrDefault();

        if (currentActiveView is not null && currentActiveView != view)
        {
            base.Deactivate(currentActiveView);
        }

        base.Activate(view);
    }
}
