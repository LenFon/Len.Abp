using Volo.Abp;

namespace Len.Abp.Wpf.Navigation.Regions;

/// <summary>
/// 区域创建失败时抛出的异常。
/// </summary>
/// <remarks>
/// 适配器初始化、控件状态检查或区域行为附加失败都会包装成该异常，
/// 原始异常保留为 <see cref="Exception.InnerException"/>。
/// 最常见的原因是区域宿主控件已被占用（<c>Content</c> 或 <c>ItemsSource</c> 不为空），
/// 或区域改名重建时自定义适配器未实现 <see cref="IRegionAdapterDetach"/>。
/// </remarks>
/// <param name="regionName">创建失败的区域名称。</param>
/// <param name="innerException">导致失败的原始异常。</param>
public class RegionCreationException(string regionName, Exception innerException)
    : AbpException($"创建名为“{regionName}”的区域时发生异常。", innerException);