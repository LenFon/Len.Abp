using System.Windows;

namespace Volo.Abp.Modularity;

public abstract class AbpWpfModule : AbpModule
{
    public virtual void ConfigureResources(ICollection<ResourceDictionary> resources)
    {
    }
}