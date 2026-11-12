using Len.Abp.Wpf.Mvvm;
using Len.Abp.Wpf.Navigation.Regions;
using System.Collections.Specialized;
using Xunit;

namespace Len.Abp.Wpf.Tests;

/// <summary>
/// 覆盖 <see cref="Region"/> 的视图增删、激活状态与查询行为。
/// </summary>
public class RegionTests
{
    /// <summary>
    /// 新建区域时名称与所有者按参数保存，视图集合为空。
    /// </summary>
    [Fact]
    public void Constructor_SetsNameOwnerAndEmptyCollections()
    {
        var region = new Region("MainRegion", "MainView");

        Assert.Equal("MainRegion", region.Name);
        Assert.Equal("MainView", region.Owner);
        Assert.Empty(region.Views);
        Assert.Empty(region.ActiveViews);
        Assert.Equal(0, region.Views.Count);
    }

    /// <summary>
    /// 添加视图后可从视图集合中查到。
    /// </summary>
    [Fact]
    public void Add_AddsViewToViewsCollection()
    {
        var region = new Region("MainRegion", "MainView");
        var view = new FakeView("demo");

        region.Add(view);

        Assert.Equal(1, region.Views.Count);
        Assert.True(region.Views.Contains(view));
        Assert.Same(view, Assert.Single(region.Views));
    }

    /// <summary>
    /// 重复添加同一视图抛出异常。
    /// </summary>
    [Fact]
    public void Add_DuplicateView_Throws()
    {
        var region = new Region("MainRegion", "MainView");
        var view = new FakeView("demo");
        region.Add(view);

        var ex = Assert.Throws<ArgumentException>(() => region.Add(view));

        Assert.Contains("已存在", ex.Message);
    }

    /// <summary>
    /// 激活视图后该视图出现在活动视图集合中。
    /// </summary>
    [Fact]
    public void Activate_AddsViewToActiveViews()
    {
        var region = new Region("MainRegion", "MainView");
        var view = new FakeView("demo");
        region.Add(view);

        region.Activate(view);

        Assert.Equal(1, region.ActiveViews.Count);
        Assert.True(region.ActiveViews.Contains(view));
    }

    /// <summary>
    /// 重复激活同一视图不会重复加入活动视图集合。
    /// </summary>
    [Fact]
    public void Activate_Twice_KeepsSingleActiveEntry()
    {
        var region = new Region("MainRegion", "MainView");
        var view = new FakeView("demo");
        region.Add(view);

        region.Activate(view);
        region.Activate(view);

        Assert.Equal(1, region.ActiveViews.Count);
    }

    /// <summary>
    /// 激活不在区域中的视图抛出异常。
    /// </summary>
    [Fact]
    public void Activate_UnknownView_Throws()
    {
        var region = new Region("MainRegion", "MainView");

        var ex = Assert.Throws<ArgumentException>(() => region.Activate(new FakeView("demo")));

        Assert.Contains("不包含指定的视图", ex.Message);
    }

    /// <summary>
    /// 失活视图后该视图从活动视图集合中移除。
    /// </summary>
    [Fact]
    public void Deactivate_RemovesViewFromActiveViews()
    {
        var region = new Region("MainRegion", "MainView");
        var view = new FakeView("demo");
        region.Add(view);
        region.Activate(view);

        region.Deactivate(view);

        Assert.Empty(region.ActiveViews);
        Assert.Equal(1, region.Views.Count);
    }

    /// <summary>
    /// 重复失活同一视图不会抛异常。
    /// </summary>
    [Fact]
    public void Deactivate_Twice_IsIdempotent()
    {
        var region = new Region("MainRegion", "MainView");
        var view = new FakeView("demo");
        region.Add(view);
        region.Activate(view);

        region.Deactivate(view);
        region.Deactivate(view);

        Assert.Empty(region.ActiveViews);
    }

    /// <summary>
    /// 失活不在区域中的视图抛出异常。
    /// </summary>
    [Fact]
    public void Deactivate_UnknownView_Throws()
    {
        var region = new Region("MainRegion", "MainView");

        Assert.Throws<ArgumentException>(() => region.Deactivate(new FakeView("demo")));
    }

    /// <summary>
    /// GetView 按视图名称返回视图。
    /// </summary>
    [Fact]
    public void GetView_ByViewName_ReturnsView()
    {
        var region = new Region("MainRegion", "MainView");
        var view = new FakeView("demo");
        region.Add(view);

        Assert.Same(view, region.GetView("demo"));
    }

    /// <summary>
    /// GetView 对不存在的名称返回 null。
    /// </summary>
    [Fact]
    public void GetView_MissingName_ReturnsNull()
    {
        var region = new Region("MainRegion", "MainView");

        Assert.Null(region.GetView("missing"));
    }

    /// <summary>
    /// 移除视图后视图集合与活动视图集合都不再包含它。
    /// </summary>
    [Fact]
    public void Remove_RemovesViewFromBothCollections()
    {
        var region = new Region("MainRegion", "MainView");
        var view = new FakeView("demo");
        region.Add(view);
        region.Activate(view);

        region.Remove(view);

        Assert.Empty(region.Views);
        Assert.Empty(region.ActiveViews);
    }

    /// <summary>
    /// 移除不在区域中的视图抛出异常。
    /// </summary>
    [Fact]
    public void Remove_UnknownView_Throws()
    {
        var region = new Region("MainRegion", "MainView");

        Assert.Throws<ArgumentException>(() => region.Remove(new FakeView("demo")));
    }

    /// <summary>
    /// RemoveAll 清空区域中的所有视图。
    /// </summary>
    [Fact]
    public void RemoveAll_ClearsAllViews()
    {
        var region = new Region("MainRegion", "MainView");
        region.Add(new FakeView("a"));
        region.Add(new FakeView("b"));
        region.Add(new FakeView("c"));

        region.RemoveAll();

        Assert.Empty(region.Views);
        Assert.Empty(region.ActiveViews);
    }

    /// <summary>
    /// RemoveAll 之后仍可继续添加视图，且视图集合恢复可用。
    /// </summary>
    [Fact]
    public void RemoveAll_ThenAdd_WorksAgain()
    {
        var region = new Region("MainRegion", "MainView");
        region.Add(new FakeView("a"));
        region.RemoveAll();

        var view = new FakeView("b");
        region.Add(view);
        region.Activate(view);

        Assert.Same(view, Assert.Single(region.Views));
        Assert.Same(view, Assert.Single(region.ActiveViews));
    }

    /// <summary>
    /// RemoveAll 对空区域是幂等的。
    /// </summary>
    [Fact]
    public void RemoveAll_OnEmptyRegion_IsIdempotent()
    {
        var region = new Region("MainRegion", "MainView");

        region.RemoveAll();
        region.RemoveAll();

        Assert.Empty(region.Views);
    }

    /// <summary>
    /// 重复激活同一视图只触发一次活动视图变更通知。
    /// </summary>
    [Fact]
    public void Activate_Twice_RaisesSingleNotification()
    {
        var region = new Region("MainRegion", "MainView");
        var view = new FakeView("demo");
        region.Add(view);

        var actions = new List<NotifyCollectionChangedAction>();
        region.ActiveViews.CollectionChanged += (_, e) => actions.Add(e.Action);

        region.Activate(view);
        region.Activate(view);

        Assert.Equal([NotifyCollectionChangedAction.Add], actions);
    }

    /// <summary>
    /// 重复失活同一视图只触发一次活动视图变更通知。
    /// </summary>
    [Fact]
    public void Deactivate_Twice_RaisesSingleNotification()
    {
        var region = new Region("MainRegion", "MainView");
        var view = new FakeView("demo");
        region.Add(view);
        region.Activate(view);

        var actions = new List<NotifyCollectionChangedAction>();
        region.ActiveViews.CollectionChanged += (_, e) => actions.Add(e.Action);

        region.Deactivate(view);
        region.Deactivate(view);

        Assert.Equal([NotifyCollectionChangedAction.Remove], actions);
    }

    /// <summary>
    /// 添加视图时视图集合发出 Add 通知。
    /// </summary>
    [Fact]
    public void Add_RaisesCollectionChangedAdd()
    {
        var region = new Region("MainRegion", "MainView");
        var actions = new List<NotifyCollectionChangedAction>();
        region.Views.CollectionChanged += (_, e) => actions.Add(e.Action);

        region.Add(new FakeView("demo"));

        Assert.Equal([NotifyCollectionChangedAction.Add], actions);
    }

    /// <summary>
    /// 激活视图时活动视图集合发出 Add 通知。
    /// </summary>
    [Fact]
    public void Activate_RaisesActiveViewsCollectionChangedAdd()
    {
        var region = new Region("MainRegion", "MainView");
        var view = new FakeView("demo");
        region.Add(view);

        var actions = new List<NotifyCollectionChangedAction>();
        region.ActiveViews.CollectionChanged += (_, e) => actions.Add(e.Action);

        region.Activate(view);

        Assert.Equal([NotifyCollectionChangedAction.Add], actions);
    }

    /// <summary>
    /// 失活视图时活动视图集合发出 Remove 通知。
    /// </summary>
    [Fact]
    public void Deactivate_RaisesActiveViewsCollectionChangedRemove()
    {
        var region = new Region("MainRegion", "MainView");
        var view = new FakeView("demo");
        region.Add(view);
        region.Activate(view);

        var actions = new List<NotifyCollectionChangedAction>();
        region.ActiveViews.CollectionChanged += (_, e) => actions.Add(e.Action);

        region.Deactivate(view);

        Assert.Equal([NotifyCollectionChangedAction.Remove], actions);
    }

    /// <summary>
    /// 视图集合按视图名称排序。
    /// </summary>
    [Fact]
    public void Views_AreOrderedByViewName()
    {
        var region = new Region("MainRegion", "MainView");
        region.Add(new FakeView("charlie"));
        region.Add(new FakeView("alpha"));
        region.Add(new FakeView("bravo"));

        Assert.Equal(["alpha", "bravo", "charlie"], region.Views.Select(v => v.ViewName));
    }

    /// <summary>
    /// 移除视图时视图集合发出 Remove 通知。
    /// </summary>
    [Fact]
    public void Remove_RaisesCollectionChangedRemove()
    {
        var region = new Region("MainRegion", "MainView");
        var view = new FakeView("demo");
        region.Add(view);

        var actions = new List<NotifyCollectionChangedAction>();
        region.Views.CollectionChanged += (_, e) => actions.Add(e.Action);

        region.Remove(view);

        Assert.Equal([NotifyCollectionChangedAction.Remove], actions);
    }
}
