using Len.Abp.Wpf.Navigation.Regions;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;
using Volo.Abp.DependencyInjection;
using Xunit;

namespace Len.Abp.Wpf.Tests;

/// <summary>
/// 覆盖 <see cref="ContentControlRegionAdapter"/> 的内容同步与自动激活。
/// </summary>
public class ContentControlRegionAdapterTests
{
    /// <summary>
    /// 适配后区域活动视图会同步到 ContentControl.Content。
    /// </summary>
    [Fact]
    public void Adapt_SyncsActiveViewToContent()
    {
        Sta.Run(() =>
        {
            var adapter = CreateAdapter();
            var contentControl = new ContentControl();
            var region = Initialize(adapter, contentControl, "MainRegion", "MainView");

            var view = new FakeView("demo");
            region.Add(view);
            region.Activate(view);

            Assert.Same(view, contentControl.Content);
        });
    }

    /// <summary>
    /// 区域中没有活动视图时 Content 为 null。
    /// </summary>
    [Fact]
    public void Adapt_NoActiveView_LeavesContentNull()
    {
        Sta.Run(() =>
        {
            var adapter = CreateAdapter();
            var contentControl = new ContentControl();
            var region = Initialize(adapter, contentControl, "MainRegion", "MainView");

            var view = new FakeView("demo");
            region.Add(view);
            region.Deactivate(view);

            Assert.Empty(region.ActiveViews);
            Assert.Null(contentControl.Content);
        });
    }

    /// <summary>
    /// 添加首个视图时自动激活，无需显式调用 Activate。
    /// </summary>
    [Fact]
    public void Add_FirstView_AutoActivates()
    {
        Sta.Run(() =>
        {
            var adapter = CreateAdapter();
            var contentControl = new ContentControl();
            var region = Initialize(adapter, contentControl, "MainRegion", "MainView");

            var view = new FakeView("demo");
            region.Add(view);

            Assert.True(region.ActiveViews.Contains(view));
            Assert.Same(view, contentControl.Content);
        });
    }

    /// <summary>
    /// Content 已被占用时适配抛出异常。
    /// </summary>
    [Fact]
    public void Adapt_ContentAlreadySet_Throws()
    {
        Sta.Run(() =>
        {
            var adapter = CreateAdapter();
            var contentControl = new ContentControl { Content = "occupied" };

            var ex = Assert.Throws<ArgumentException>(
                () => Initialize(adapter, contentControl, "MainRegion", "MainView")
            );

            Assert.Equal("regionTarget", ex.ParamName);
        });
    }

    /// <summary>
    /// 创建的区域是单活动视图区域。
    /// </summary>
    [Fact]
    public void Initialize_CreatesSingleActiveRegion()
    {
        Sta.Run(() =>
        {
            var adapter = CreateAdapter();

            var region = Initialize(adapter, new ContentControl(), "MainRegion", "MainView");

            Assert.IsType<SingleActiveRegion>(region);
            Assert.Equal("MainRegion", region.Name);
            Assert.Equal("MainView", region.Owner);
        });
    }

    /// <summary>
    /// 区域目标为 null 时抛出异常。
    /// </summary>
    [Fact]
    public void Initialize_NullTarget_Throws()
    {
        Sta.Run(() =>
        {
            var adapter = CreateAdapter();

            Assert.Throws<ArgumentNullException>(() => Initialize(adapter, null!, "MainRegion", "MainView"));
        });
    }

    /// <summary>
    /// 区域名称为 null 时抛出异常。
    /// </summary>
    [Fact]
    public void Initialize_NullRegionName_Throws()
    {
        Sta.Run(() =>
        {
            var adapter = CreateAdapter();

            Assert.Throws<ArgumentNullException>(() => Initialize(adapter, new ContentControl(), null!, "MainView"));
        });
    }

    /// <summary>
    /// 视图名称为 null 时抛出异常。
    /// </summary>
    [Fact]
    public void Initialize_NullViewName_Throws()
    {
        Sta.Run(() =>
        {
            var adapter = CreateAdapter();

            Assert.Throws<ArgumentNullException>(() => Initialize(adapter, new ContentControl(), "MainRegion", null!));
        });
    }

    /// <summary>
    /// 区域目标类型不匹配时抛出异常。
    /// </summary>
    [Fact]
    public void Initialize_WrongTargetType_Throws()
    {
        Sta.Run(() =>
        {
            var adapter = CreateAdapter();

            var ex = Assert.Throws<ArgumentException>(() => Initialize(adapter, new object(), "MainRegion", "MainView"));

            Assert.Equal("regionTarget", ex.ParamName);
        });
    }

    private static ContentControlRegionAdapter CreateAdapter() => new() { AbpLazyServiceProvider = new AbpLazyServiceProvider(new ServiceCollection().AddOptions().BuildServiceProvider()) };

    /// <summary>
    /// <see cref="IRegionAdapter.Initialize"/> 是显式接口实现，测试中通过接口调用。
    /// </summary>
    private static IRegion Initialize(IRegionAdapter adapter, object target, string regionName, string viewName) =>
        adapter.Initialize(target, regionName, viewName);
}
