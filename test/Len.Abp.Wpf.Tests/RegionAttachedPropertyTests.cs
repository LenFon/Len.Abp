using Len.Abp.Core;
using Len.Abp.Wpf.Navigation;
using Len.Abp.Wpf.Navigation.Regions;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using Xunit;

namespace Len.Abp.Wpf.Tests;

[Collection(GlobalServiceProviderCollection.Name)]
public class RegionAttachedPropertyTests
{
    /// <summary>
    /// Name 附加属性注册在 <see cref="RegionHost"/> 类型上。
    /// </summary>
    [Fact]
    public void NameProperty_IsRegisteredOnRegionHostType()
    {
        // XAML 命名空间映射到 Len.Abp.Wpf.Navigation，因此该附加属性必须由 RegionHost 声明，
        // `len:RegionHost.Name` 才能解析。
        Assert.Equal("RegionHost", RegionHost.NameProperty.OwnerType.Name);
        Assert.Equal("Name", RegionHost.NameProperty.Name);
        Assert.Equal(typeof(string), RegionHost.NameProperty.PropertyType);
    }

    /// <summary>
    /// 设置区域名称后能读回相同的值。
    /// </summary>
    [Fact]
    public void GetSetName_RoundTrips()
    {
        RunOnSta(() =>
        {
            // 设置 Name 会触发区域创建，而区域创建会从静态服务提供程序解析 IRegionBuilder。
            // 这里提供一个最小化的提供程序，使测试聚焦于属性本身。
            var services = new ServiceCollection();
            services.AddTransient<IRegionBuilder>(_ => new NoOpRegionBuilder());
            AbpServiceProvider.Initialize(services.BuildServiceProvider());

            var element = new DependencyObject();
            RegionHost.SetName(element, "MainRegion");

            Assert.Equal("MainRegion", RegionHost.GetName(element));
        });
    }

    private sealed class NoOpRegionBuilder : IRegionBuilder
    {
        public void Build(DependencyObject element) { }
    }

    /// <summary>
    /// 传入 null 目标时抛出异常。
    /// </summary>
    [Fact]
    public void GetName_NullTarget_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => RegionHost.GetName(null!));
    }

    private static void RunOnSta(Action action)
    {
        Exception? captured = null;
        var thread = new Thread(() =>
        {
            try { action(); }
            catch (Exception ex) { captured = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (captured is not null)
        {
            throw captured;
        }
    }
}
