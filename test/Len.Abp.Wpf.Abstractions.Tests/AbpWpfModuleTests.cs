using System.Windows;
using Xunit;

namespace Len.Abp.Wpf.Abstractions.Tests;

/// <summary>
/// 覆盖 <see cref="Volo.Abp.Modularity.AbpWpfModule"/> 的默认资源合并行为。
/// </summary>
public class AbpWpfModuleTests
{
    /// <summary>
    /// 默认实现不向资源字典集合添加任何内容。
    /// </summary>
    [Fact]
    public void ConfigureResources_DefaultImplementation_AddsNothing()
    {
        Sta.Run(() =>
        {
            var module = new SampleWpfModule();
            var resources = new List<ResourceDictionary>();

            module.ConfigureResources(resources);

            Assert.Empty(resources);
        });
    }

    private sealed class SampleWpfModule : Volo.Abp.Modularity.AbpWpfModule;
}
