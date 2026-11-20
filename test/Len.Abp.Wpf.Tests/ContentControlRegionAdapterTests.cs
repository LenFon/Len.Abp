using Len.Abp.Wpf.Mvvm;
using Len.Abp.Wpf.Navigation.Regions;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Specialized;
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
    /// 所属视图为 null 时抛出异常。
    /// </summary>
    [Fact]
    public void Initialize_NullOwnerView_Throws()
    {
        Sta.Run(() =>
        {
            var adapter = CreateAdapter();

            Assert.Throws<ArgumentNullException>(
                () => ((IRegionAdapter)adapter).Initialize(new ContentControl(), "MainRegion", null!)
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

            var ex = Assert.Throws<ArgumentException>(() => Initialize(adapter, new object(), "MainRegion", "MainView"));

            Assert.Equal("regionTarget", ex.ParamName);
        });
    }

    /// <summary>
    /// 解除适配后，旧区域不再驱动控件。
    /// </summary>
    /// <remarks>
    /// 回归：<c>Adapt</c> 订阅了区域的 <c>Views</c> / <c>ActiveViews</c>，
    /// 而 <c>Detach</c> 只清空 <c>Content</c>、从不解订阅。
    /// 区域改名重建时旧区域被丢弃，但它的处理器仍会写入控件，
    /// 且控件持有闭包导致旧区域与视图无法回收。
    /// </remarks>
    [Fact]
    public void Detach_UnsubscribesAndStopsDrivingControl()
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

            ((IRegionAdapterDetach)adapter).Detach(region, contentControl);

            Assert.Null(contentControl.Content);

            // 旧区域已被丢弃：它的激活不应再写入控件。
            var another = new FakeView("another");
            region.Add(another);
            region.Activate(another);

            Assert.Null(contentControl.Content);
        });
    }

    /// <summary>
    /// 派生类漏调 <c>base.Detach</c> 时，订阅清理由基类的显式接口实现兜底。
    /// </summary>
    /// <remarks>
    /// 回归：这条约束原先只写在 XML 注释里，漏调不会编译报错，
    /// 后果是事件订阅泄漏——被丢弃的区域继续驱动控件，控件因持有闭包无法回收。
    /// 现在 <see cref="IRegionAdapterDetach.Detach"/> 的显式实现先执行
    /// <c>OnDetach</c> 登记的动作，再调用派生类的 <c>Detach</c>，
    /// 因此漏调的后果降级为"控件状态未清除"（重建时立刻暴露），而不是静默泄漏。
    /// </remarks>
    [Fact]
    public void Detach_OverrideWithoutCallingBase_StillUnsubscribes()
    {
        Sta.Run(() =>
        {
            var adapter = new ForgetfulAdapter(
                new AbpLazyServiceProvider(new ServiceCollection().AddOptions().BuildServiceProvider())
            );
            var contentControl = new ContentControl();
            var region = Initialize(adapter, contentControl, "MainRegion", "MainView");

            var view = new FakeView("demo");
            region.Add(view);
            region.Activate(view);
            Assert.Same(view, contentControl.Content);

            ((IRegionAdapterDetach)adapter).Detach(region, contentControl);

            // 派生类没清 Content（它漏调了 base，也没自己清），但订阅必须已解除。
            var another = new FakeView("another");
            region.Add(another);
            region.Activate(another);

            Assert.NotSame(another, contentControl.Content);
        });
    }

    /// <summary>
    /// 重写 <c>Detach</c> 但故意不调用 <c>base.Detach</c> 的适配器。
    /// </summary>
    private sealed class ForgetfulAdapter(IAbpLazyServiceProvider provider) : BasicRegionAdapter<ContentControl>(provider)
    {
        protected override void Adapt(IRegion region, ContentControl regionTarget)
        {
            void OnActiveViewsChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
                regionTarget.Content = region.ActiveViews.LastOrDefault();

            region.ActiveViews.CollectionChanged += OnActiveViewsChanged;

            // 订阅必须通过 OnDetach 登记，基类才能在 Detach 时解除它。
            OnDetach(region, () => region.ActiveViews.CollectionChanged -= OnActiveViewsChanged);
        }

        protected override IRegion CreateRegion(string regionName, IView ownerView) =>
            new SingleActiveRegion(regionName, ownerView.ViewName, ownerView);

        // 故意不调用 base.Detach：验证基类的显式接口实现仍会解除订阅。
        protected override void Detach(IRegion region, ContentControl regionTarget)
        {
        }
    }

    private static ContentControlRegionAdapter CreateAdapter() =>
        new(new AbpLazyServiceProvider(new ServiceCollection().AddOptions().BuildServiceProvider()));

    /// <summary>
    /// <see cref="IRegionAdapter.Initialize"/> 是显式接口实现，测试中通过接口调用。
    /// </summary>
    private static IRegion Initialize(IRegionAdapter adapter, object target, string regionName, string viewName) =>
        adapter.Initialize(target, regionName, new FakeView(viewName));
}
