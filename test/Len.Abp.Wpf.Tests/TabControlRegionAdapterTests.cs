using Len.Abp.Wpf.Mvvm;
using Len.Abp.Wpf.Navigation;
using Len.Abp.Wpf.Navigation.Regions;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;
using Volo.Abp.DependencyInjection;
using Xunit;

namespace Len.Abp.Wpf.Tests;

/// <summary>
/// 覆盖 <see cref="TabControlRegionAdapter"/> 的选项卡生成、标题与选中同步。
/// </summary>
public class TabControlRegionAdapterTests
{
    /// <summary>
    /// 每个视图生成一个选项卡，内容为视图本身。
    /// </summary>
    [Fact]
    public void Adapt_CreatesTabPerView()
    {
        Sta.Run(() =>
        {
            var tabControl = new TabControl();
            var region = Initialize(tabControl);

            var first = new FakeView("first");
            var second = new FakeView("second");
            region.Add(first);
            region.Add(second);

            var tabs = GetTabs(tabControl);
            Assert.Equal(2, tabs.Count);
            Assert.Same(first, tabs[0].Content);
            Assert.Same(second, tabs[1].Content);
        });
    }

    /// <summary>
    /// 未设置 TabHeader 时用视图名作为标题。
    /// </summary>
    [Fact]
    public void Adapt_WithoutTabHeader_UsesViewName()
    {
        Sta.Run(() =>
        {
            var tabControl = new TabControl();
            var region = Initialize(tabControl);

            region.Add(new FakeView("lifetime-scoped"));

            Assert.Equal("lifetime-scoped", GetTabs(tabControl)[0].Header);
        });
    }

    /// <summary>
    /// 设置 TabHeader 后用它作为标题。
    /// </summary>
    [Fact]
    public void Adapt_WithTabHeader_UsesHeader()
    {
        Sta.Run(() =>
        {
            var tabControl = new TabControl();
            var region = Initialize(tabControl);
            var view = new HeaderView("lifetime-scoped", "作用域视图");

            region.Add(view);

            Assert.Equal("作用域视图", GetTabs(tabControl)[0].Header);
        });
    }

    /// <summary>
    /// 激活视图时选中对应选项卡。
    /// </summary>
    [Fact]
    public void Activate_SelectsCorrespondingTab()
    {
        Sta.Run(() =>
        {
            var tabControl = new TabControl();
            var region = Initialize(tabControl);
            var first = new FakeView("first");
            var second = new FakeView("second");
            region.Add(first);
            region.Add(second);

            region.Activate(second);

            var tabs = GetTabs(tabControl);
            Assert.Same(tabs[1], tabControl.SelectedItem);
        });
    }

    /// <summary>
    /// 切换选项卡时激活对应视图，旧视图失活。
    /// </summary>
    [Fact]
    public void SelectTab_ActivatesView()
    {
        Sta.Run(() =>
        {
            var tabControl = new TabControl();
            var region = Initialize(tabControl);
            var first = new FakeView("first");
            var second = new FakeView("second");
            region.Add(first);
            region.Add(second);

            region.Activate(first);
            var tabs = GetTabs(tabControl);
            tabControl.SelectedItem = tabs[1];

            Assert.True(region.ActiveViews.Contains(second));
            Assert.False(region.ActiveViews.Contains(first));
        });
    }

    /// <summary>
    /// 移除视图时对应选项卡消失。
    /// </summary>
    [Fact]
    public void Remove_RemovesTab()
    {
        Sta.Run(() =>
        {
            var tabControl = new TabControl();
            var region = Initialize(tabControl);
            var view = new FakeView("demo");
            region.Add(view);

            region.Remove(view);

            Assert.Empty(GetTabs(tabControl));
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
            var region = Initialize(new TabControl());

            Assert.IsType<SingleActiveRegion>(region);
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
            var tabControl = new TabControl { ItemsSource = Array.Empty<string>() };

            var ex = Assert.Throws<ArgumentException>(() => Initialize(tabControl));

            Assert.Equal("regionTarget", ex.ParamName);
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
                () => ((IRegionAdapter)adapter).Initialize(new ItemsControl(), "MainRegion", new FakeView("MainView"))
            );

            Assert.Equal("regionTarget", ex.ParamName);
        });
    }

    /// <summary>
    /// 批量变更（Reset）后按区域当前视图重建全部选项卡。
    /// </summary>
    /// <remarks>
    /// <c>RemoveAll</c> 会先清空元数据再逐项处理，集合变更因此表现为 Reset。
    /// 若不重建，选项卡会残留已移除视图的标题。
    /// </remarks>
    [Fact]
    public void RemoveAll_RebuildsTabsFromRemainingViews()
    {
        Sta.Run(() =>
        {
            var tabControl = new TabControl();
            var region = Initialize(tabControl);
            region.Add(new FakeView("first"));
            region.Add(new FakeView("second"));

            region.RemoveAll();

            Assert.Empty(GetTabs(tabControl));
        });
    }

    /// <summary>
    /// 程序化选中选项卡时不会反向激活视图，避免激活与选中互相递归。
    /// </summary>
    [Fact]
    public void Activate_DoesNotReenterSelectionSync()
    {
        Sta.Run(() =>
        {
            var tabControl = new TabControl();
            var region = Initialize(tabControl);
            var first = new FakeView("first");
            var second = new FakeView("second");
            region.Add(first);
            region.Add(second);

            region.Activate(second);
            region.Activate(first);

            Assert.Equal([first], region.ActiveViews);
            Assert.Same(GetTabs(tabControl)[0], tabControl.SelectedItem);
        });
    }

    /// <summary>
    /// 解除适配后，旧区域不再驱动控件，也不会因用户切换选项卡而抛异常。
    /// </summary>
    /// <remarks>
    /// 回归：<c>Adapt</c> 订阅了区域的 <c>Views</c> / <c>ActiveViews</c> 与控件的
    /// <c>SelectionChanged</c>，而 <c>Detach</c> 只清空 <c>ItemsSource</c>、从不解订阅。
    /// 区域改名重建时旧区域被丢弃，但它的处理器仍挂在控件上：
    /// 用户切换选项卡会对已清空的旧区域调用 <c>Activate</c> 并抛
    /// <see cref="ArgumentException"/>，同时控件持有闭包导致旧区域与视图无法回收。
    /// </remarks>
    [Fact]
    public void Detach_UnsubscribesAndStopsDrivingControl()
    {
        Sta.Run(() =>
        {
            var tabControl = new TabControl();
            var adapter = CreateAdapter();
            var region = ((IRegionAdapter)adapter).Initialize(tabControl, "MainRegion", new FakeView("MainView"));

            region.Add(new FakeView("first"));

            ((IRegionAdapterDetach)adapter).Detach(region, tabControl);

            Assert.Null(tabControl.ItemsSource);

            // 旧区域已被丢弃：它的视图变化不应再写入控件。
            region.Add(new FakeView("second"));
            Assert.Empty(GetTabs(tabControl));

            // 控件上不应残留旧区域的 SelectionChanged 处理器。
            tabControl.SelectedIndex = -1;
        });
    }

    private static List<TabItem> GetTabs(TabControl tabControl) =>
        [.. tabControl.Items.Cast<TabItem>()];

    private static TabControlRegionAdapter CreateAdapter() =>
        new(new AbpLazyServiceProvider(new ServiceCollection().AddOptions().BuildServiceProvider()));

    private static IRegion Initialize(TabControl tabControl) =>
        ((IRegionAdapter)CreateAdapter()).Initialize(tabControl, "MainRegion", new FakeView("MainView"));

    /// <summary>
    /// 设置了选项卡标题的视图。
    /// </summary>
    /// <remarks>
    /// 必须是 <see cref="System.Windows.DependencyObject"/> 才能承载 <c>RegionHost.TabHeader</c> 附加属性，
    /// 真实视图都是 <see cref="System.Windows.FrameworkElement"/>，这里用 <see cref="ContentControl"/> 模拟。
    /// </remarks>
    private sealed class HeaderView : ContentControl, IView
    {
        public HeaderView(string viewName, string header)
        {
            ViewName = viewName;
            RegionHost.SetTabHeader(this, header);
        }

        public object ViewModel { get; } = new object();

        public string ViewName { get; }

        public IReadOnlyDictionary<string, Type?> RequiredNavigationParameters { get; } =
            new Dictionary<string, Type?>(StringComparer.OrdinalIgnoreCase);
    }
}
