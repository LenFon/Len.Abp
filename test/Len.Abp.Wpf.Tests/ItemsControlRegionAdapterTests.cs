using Len.Abp.Wpf.Mvvm;
using Len.Abp.Wpf.Navigation.Regions;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;
using Volo.Abp.DependencyInjection;
using Xunit;

namespace Len.Abp.Wpf.Tests;

/// <summary>
/// 覆盖 <see cref="ItemsControlRegionAdapter"/> 的项同步与多活动视图语义。
/// </summary>
public class ItemsControlRegionAdapterTests
{
    /// <summary>
    /// 区域中的视图同步到 ItemsSource。
    /// </summary>
    [Fact]
    public void Adapt_SyncsViewsToItemsSource()
    {
        Sta.Run(() =>
        {
            var itemsControl = new ItemsControl();
            var region = Initialize(itemsControl);

            var first = new FakeView("first");
            var second = new FakeView("second");
            region.Add(first);
            region.Add(second);

            Assert.Equal([first, second], itemsControl.Items.Cast<IView>());
        });
    }

    /// <summary>
    /// 视图按加入顺序显示，不按视图名排序。
    /// </summary>
    [Fact]
    public void Adapt_PreservesNavigationOrder()
    {
        Sta.Run(() =>
        {
            var itemsControl = new ItemsControl();
            var region = Initialize(itemsControl);

            region.Add(new FakeView("charlie"));
            region.Add(new FakeView("alpha"));
            region.Add(new FakeView("bravo"));

            Assert.Equal(["charlie", "alpha", "bravo"], itemsControl.Items.Cast<IView>().Select(v => v.ViewName));
        });
    }

    /// <summary>
    /// 移除视图时对应项也从 ItemsSource 消失。
    /// </summary>
    [Fact]
    public void Remove_RemovesItem()
    {
        Sta.Run(() =>
        {
            var itemsControl = new ItemsControl();
            var region = Initialize(itemsControl);
            var view = new FakeView("demo");
            region.Add(view);

            region.Remove(view);

            Assert.Empty(itemsControl.Items.Cast<IView>());
        });
    }

    /// <summary>
    /// 创建的区域支持多活动视图。
    /// </summary>
    [Fact]
    public void Initialize_CreatesMultiActiveRegion()
    {
        Sta.Run(() =>
        {
            var region = Initialize(new ItemsControl());

            Assert.IsType<Region>(region);
            Assert.IsNotType<SingleActiveRegion>(region);
        });
    }

    /// <summary>
    /// 多个视图可同时激活，且全部显示。
    /// </summary>
    [Fact]
    public void Activate_MultipleViews_AllRemainVisible()
    {
        Sta.Run(() =>
        {
            var itemsControl = new ItemsControl();
            var region = Initialize(itemsControl);
            var first = new FakeView("first");
            var second = new FakeView("second");
            region.Add(first);
            region.Add(second);

            region.Activate(first);
            region.Activate(second);

            Assert.Equal(2, region.ActiveViews.Count);
            Assert.Equal([first, second], itemsControl.Items.Cast<IView>());
        });
    }

    /// <summary>
    /// ItemsSource 已被占用时适配抛出异常。
    /// </summary>
    [Fact]
    public void Adapt_ItemsSourceAlreadySet_Throws()
    {
        Sta.Run(() =>
        {
            // ItemsSource 是非泛型 IEnumerable，集合表达式 [] 无法用于它（CS9174）；
            // Array.Empty 同样是空序列，且不涉及集合初始化。
            var itemsControl = new ItemsControl { ItemsSource = Array.Empty<string>() };

            var ex = Assert.Throws<ArgumentException>(() => Initialize(itemsControl));

            Assert.Equal("regionTarget", ex.ParamName);
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

            Assert.Throws<ArgumentNullException>(
                () => ((IRegionAdapter)adapter).Initialize(new ItemsControl(), null!, new FakeView("MainView"))
            );
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

            var ex = Assert.Throws<ArgumentException>(
                () => ((IRegionAdapter)adapter).Initialize(new object(), "MainRegion", new FakeView("MainView"))
            );

            Assert.Equal("regionTarget", ex.ParamName);
        });
    }

    private static ItemsControlRegionAdapter CreateAdapter() =>
        new(new AbpLazyServiceProvider(new ServiceCollection().AddOptions().BuildServiceProvider()));

    private static IRegion Initialize(ItemsControl itemsControl) =>
        ((IRegionAdapter)CreateAdapter()).Initialize(itemsControl, "MainRegion", new FakeView("MainView"));
}
