using Volo.Abp;

namespace Len.Abp.Wpf.Navigation.Regions;

public class RegionCreationException(string regionName, Exception innerException)
    : AbpException($"An exception occurred while creating a region with name '{regionName}'.", innerException);