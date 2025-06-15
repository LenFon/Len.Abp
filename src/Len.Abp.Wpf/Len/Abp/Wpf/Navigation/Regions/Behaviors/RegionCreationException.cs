using Volo.Abp;

namespace Len.Abp.Wpf.Navigation.Regions.Behaviors;

public class RegionCreationException(string message, Exception inner) : AbpException(message, inner)
{
}