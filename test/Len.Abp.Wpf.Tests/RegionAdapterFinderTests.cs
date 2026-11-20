using Len.Abp.Wpf.Mvvm;
using Len.Abp.Wpf.Navigation.Regions;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;
using Volo.Abp.DependencyInjection;
using Xunit;


namespace Len.Abp.Wpf.Tests;

/// <summary>
/// 覆盖 <see cref="RegionAdapterFinder"/> 沿继承链查找适配器。
/// </summary>
public class RegionAdapterFinderTests
{
    /// <summary>
    /// 控件类型本身注册了适配器时直接返回。
    /// </summary>
    [Fact]
    public void FindByControlType_ExactType_ReturnsAdapter()
    {
        var adapter = new StubAdapter();
        var finder = CreateFinder(new Dictionary<Type, IRegionAdapter> { [typeof(ContentControl)] = adapter });

        Assert.Same(adapter, finder.FindByControlType(typeof(ContentControl)));
    }

    /// <summary>
    /// 控件类型未注册时沿基类向上查找。
    /// </summary>
    [Fact]
    public void FindByControlType_DerivedType_FallsBackToBaseType()
    {
        var adapter = new StubAdapter();
        var finder = CreateFinder(new Dictionary<Type, IRegionAdapter> { [typeof(ContentControl)] = adapter });

        Assert.Same(adapter, finder.FindByControlType(typeof(DerivedContentControl)));
    }

    /// <summary>
    /// 整条继承链都没有适配器时抛出异常。
    /// </summary>
    [Fact]
    public void FindByControlType_NoAdapter_Throws()
    {
        var finder = CreateFinder([]);

        var ex = Assert.Throws<KeyNotFoundException>(() => finder.FindByControlType(typeof(ContentControl)));

        Assert.Contains("未找到控件类型", ex.Message);
    }

    /// <summary>
    /// TabControl 同时匹配自身与 ItemsControl 的适配器时，优先返回自身的。
    /// </summary>
    /// <remarks>
    /// <see cref="TabControl"/> 继承自 <see cref="ItemsControl"/>，两者都注册了适配器。
    /// 查找必须从具体类型开始，否则 TabControl 会被当成普通 ItemsControl 处理，
    /// 失去选项卡语义。
    /// </remarks>
    [Fact]
    public void FindByControlType_TabControl_PrefersTabControlAdapter()
    {
        var itemsAdapter = new StubAdapter();
        var tabAdapter = new StubAdapter();
        var finder = CreateFinder(
            new Dictionary<Type, IRegionAdapter>
            {
                [typeof(ItemsControl)] = itemsAdapter,
                [typeof(TabControl)] = tabAdapter,
            }
        );

        Assert.Same(tabAdapter, finder.FindByControlType(typeof(TabControl)));
        Assert.Same(itemsAdapter, finder.FindByControlType(typeof(ItemsControl)));
    }

    private static RegionAdapterFinder CreateFinder(Dictionary<Type, IRegionAdapter> adapters)
    {
        var services = new ServiceCollection();
        foreach (var (type, adapter) in adapters)
        {
            services.AddKeyedSingleton<IRegionAdapter>(type, adapter);
        }

        var provider = services.BuildServiceProvider();
        var lazyProvider = new AbpLazyServiceProvider(provider);

        return new RegionAdapterFinder(lazyProvider);
    }

    private sealed class DerivedContentControl : ContentControl;

    private sealed class StubAdapter : IRegionAdapter
    {
        public IRegion Initialize(object regionTarget, string regionName, IView ownerView) =>
            new Region(regionName, ownerView.ViewName, ownerView);
    }
}
