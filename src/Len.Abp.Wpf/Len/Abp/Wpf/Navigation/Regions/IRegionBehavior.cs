namespace Len.Abp.Wpf.Navigation.Regions;

/// <summary>
/// 区域行为，在区域创建后附加，用于扩展区域语义（如注册、保活）。
/// </summary>
public interface IRegionBehavior
{
    /// <summary>
    /// 获取或设置行为所属的区域。由区域行为集合在附加时赋值。
    /// </summary>
    IRegion Region { get; set; }

    /// <summary>
    /// 把行为附加到 <see cref="Region"/>。
    /// </summary>
    void Attach();
}
