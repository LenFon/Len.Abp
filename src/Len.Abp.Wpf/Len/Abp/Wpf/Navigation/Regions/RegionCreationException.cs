using Volo.Abp;

namespace Len.Abp.Wpf.Navigation.Regions;

public class RegionCreationException(string message, Exception inner) : AbpException(message, inner)
{
}