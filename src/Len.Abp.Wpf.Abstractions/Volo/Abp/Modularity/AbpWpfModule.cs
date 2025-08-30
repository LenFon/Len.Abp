using System.Collections.ObjectModel;
using System.Windows;

namespace Volo.Abp.Modularity;

public abstract class AbpWpfModule : AbpModule
{
    public virtual void ConfigureResources(Collection<ResourceDictionary> resources)
    {
    }
}