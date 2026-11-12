using System.Windows;

namespace Volo.Abp.Modularity;

/// <summary>
/// WPF 应用的 ABP 模块基类，支持向应用合并资源字典。
/// </summary>
public abstract class AbpWpfModule : AbpModule
{
    /// <summary>
    /// 配置模块要合并到 WPF 应用中的资源字典。
    /// </summary>
    /// <param name="resources">应用合并字典的集合。</param>
    public virtual void ConfigureResources(ICollection<ResourceDictionary> resources)
    {
    }
}
