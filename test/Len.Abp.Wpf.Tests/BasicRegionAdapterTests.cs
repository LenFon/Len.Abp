using Len.Abp.Wpf.Mvvm;
using Len.Abp.Wpf.Navigation.Regions;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.DependencyInjection;
using System.Windows.Controls;
using Xunit;

namespace Len.Abp.Wpf.Tests;

/// <summary>
/// 覆盖 <see cref="BasicRegionAdapter{T}"/> 的默认行为与参数校验。
/// </summary>
public class BasicRegionAdapterTests
{
    /// <summary>
    /// 默认不附加自定义行为，但会附加配置中的默认行为。
    /// </summary>
    [Fact]
    public void Initialize_AttachesDefaultBehaviors()
    {
        Sta.Run(() =>
        {
            var behavior = new RecordingBehavior();
            var adapter = CreateAdapter(behavior);

            var region = ((IRegionAdapter)adapter).Initialize(new ContentControl(), "MainRegion", new FakeView("MainView"));

            Assert.True(region.Behaviors.ContainsKey("test"));
            Assert.Same(behavior, region.Behaviors["test"]);
        });
    }

    /// <summary>
    /// 已存在同名行为时不重复添加。
    /// </summary>
    [Fact]
    public void Initialize_ExistingBehavior_IsNotReplaced()
    {
        Sta.Run(() =>
        {
            var behavior = new RecordingBehavior();
            var adapter = CreateAdapter(behavior);

            var region = ((IRegionAdapter)adapter).Initialize(new ContentControl(), "MainRegion", new FakeView("MainView"));
            var first = region.Behaviors["test"];

            // 再次初始化同一适配器，验证 ContainsKey 分支。
            var second = ((IRegionAdapter)adapter).Initialize(new ContentControl(), "MainRegion2", new FakeView("MainView2"));

            Assert.Same(first, region.Behaviors["test"]);
            Assert.True(second.Behaviors.ContainsKey("test"));
        });
    }

    /// <summary>
    /// 适配器在创建区域后调用 Adapt 与 AttachBehaviors。
    /// </summary>
    [Fact]
    public void Initialize_CallsAdaptAndAttachBehaviors()
    {
        Sta.Run(() =>
        {
            var adapter = CreateAdapter();

            var region = ((IRegionAdapter)adapter).Initialize(new ContentControl(), "MainRegion", new FakeView("MainView"));

            Assert.True(adapter.AdaptCalled);
            Assert.True(adapter.AttachBehaviorsCalled);
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
        var adapter = CreateAdapter();

        Assert.Throws<ArgumentNullException>(
            () => ((IRegionAdapter)adapter).Initialize(null!, "MainRegion", new FakeView("MainView"))
        );
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
                () => ((IRegionAdapter)adapter).Initialize(new ContentControl(), null!, new FakeView("MainView"))
            );
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
        var adapter = CreateAdapter();

        var ex = Assert.Throws<ArgumentException>(
            () => ((IRegionAdapter)adapter).Initialize(new object(), "MainRegion", new FakeView("MainView"))
        );

        Assert.Equal("regionTarget", ex.ParamName);
    }

    private static TestRegionAdapter CreateAdapter(params IRegionBehavior[] behaviors)
    {
        var services = new ServiceCollection();
        services.AddOptions();
        services.Configure<RegionOptions>(options =>
        {
            foreach (var _ in behaviors)
            {
                options.AddBehaviorKey("test");
            }
        });

        for (var i = 0; i < behaviors.Length; i++)
        {
            services.AddKeyedSingleton<IRegionBehavior>("test", behaviors[i]);
        }

        var provider = services.BuildServiceProvider();

        return new TestRegionAdapter { AbpLazyServiceProvider = new AbpLazyServiceProvider(provider) };
    }

    private sealed class TestRegionAdapter : BasicRegionAdapter<ContentControl>
    {
        public bool AdaptCalled { get; private set; }

        public bool AttachBehaviorsCalled { get; private set; }

        protected override void Adapt(IRegion region, ContentControl regionTarget) => AdaptCalled = true;

        protected override void AttachBehaviors(IRegion region, ContentControl regionTarget) => AttachBehaviorsCalled = true;

        protected override IRegion CreateRegion(string regionName, IView ownerView) =>
            new Region(regionName, ownerView.ViewName, ownerView);
    }

    private sealed class RecordingBehavior : IRegionBehavior
    {
        public IRegion Region { get; set; } = default!;

        public void Attach() { }
    }
}
