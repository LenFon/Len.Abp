namespace Len.Abp.Wpf.Navigation;

/// <summary>
/// 导航相关配置。
/// </summary>
/// <remarks>
/// 与 <see cref="Regions.RegionOptions"/> 分开：后者保存区域行为键，属于区域引擎的装配配置；
/// 本类保存导航服务自身的行为参数，两者变化原因不同。
/// </remarks>
public sealed class NavigationOptions
{
    /// <summary>
    /// 历史栈的默认深度。
    /// </summary>
    public const int DefaultHistoryDepth = 10;

    /// <summary>
    /// 获取或设置每个区域保留的导航历史条数。
    /// </summary>
    /// <remarks>
    /// 超出深度时丢弃最旧的条目。设为 0 或负数时回退到 <see cref="DefaultHistoryDepth"/>，
    /// 避免配置失误让 <c>GoBack</c> 完全失效。
    /// </remarks>
    public int HistoryDepth { get; set; } = DefaultHistoryDepth;
}
