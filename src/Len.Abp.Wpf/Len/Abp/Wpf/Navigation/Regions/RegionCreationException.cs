using Volo.Abp;

namespace Len.Abp.Wpf.Navigation.Regions;

public class RegionCreationException(string regionName, Exception innerException)
    : AbpException($"创建名为“{regionName}”的区域时发生异常。", innerException);