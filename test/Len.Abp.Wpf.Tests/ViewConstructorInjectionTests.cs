using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Autofac;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Modularity;
using Xunit;

namespace Len.Abp.Wpf.Tests;

/// <summary>
/// 验证生成的视图构造函数在 Autofac 下会被选中，视图模型确实被注入。
/// </summary>
/// <remarks>
/// <c>MvvmGenerator</c> 生成的视图同时有无参构造函数和接收视图模型的构造函数。
/// 若容器选择无参构造函数，<c>DataContext</c> 会保持为 null，绑定静默失效。
/// </remarks>
public class ViewConstructorInjectionTests
{
    /// <summary>
    /// 容器解析视图时选择接收视图模型的构造函数。
    /// </summary>
    [Fact]
    public async Task Autofac_SelectsConstructorWithViewModel()
    {
        using var application = await AbpApplicationFactory.CreateAsync<AutofacHostModule>(options =>
        {
            options.UseAutofac();
        });
        await application.InitializeAsync();

        var view = application.ServiceProvider.GetRequiredService<SampleView>();

        Assert.NotNull(view.ViewModel);
        Assert.Same(view.ViewModel, view.DataContext);
        Assert.True(view.ParameterlessConstructorCalled);
    }

    /// <summary>
    /// 默认 DI 容器同样选择接收视图模型的构造函数。
    /// </summary>
    /// <remarks>
    /// 这是构造函数注入相对属性注入的主要收益：不依赖 Autofac 的属性注入能力。
    /// </remarks>
    [Fact]
    public async Task DefaultContainer_SelectsConstructorWithViewModel()
    {
        using var application = await AbpApplicationFactory.CreateAsync<DefaultHostModule>();
        await application.InitializeAsync();

        var view = application.ServiceProvider.GetRequiredService<SampleView>();

        Assert.NotNull(view.ViewModel);
        Assert.Same(view.ViewModel, view.DataContext);
        Assert.True(view.ParameterlessConstructorCalled);
    }

    /// <summary>
    /// 声明为单例的视图在多次解析时返回同一实例。
    /// </summary>
    [Fact]
    public async Task SingletonView_IsResolvedAsSameInstance()
    {
        using var application = await AbpApplicationFactory.CreateAsync<DefaultHostModule>();
        await application.InitializeAsync();

        var first = application.ServiceProvider.GetRequiredService<SingletonView>();
        var second = application.ServiceProvider.GetRequiredService<SingletonView>();

        Assert.Same(first, second);
        Assert.NotNull(first.DataContext);
    }

    /// <summary>
    /// 声明为作用域的视图在同一作用域内返回同一实例，跨作用域返回不同实例。
    /// </summary>
    [Fact]
    public async Task ScopedView_IsResolvedPerScope()
    {
        using var application = await AbpApplicationFactory.CreateAsync<DefaultHostModule>();
        await application.InitializeAsync();

        using var scope1 = application.ServiceProvider.CreateScope();
        using var scope2 = application.ServiceProvider.CreateScope();

        var first = scope1.ServiceProvider.GetRequiredService<ScopedView>();
        var second = scope1.ServiceProvider.GetRequiredService<ScopedView>();
        var third = scope2.ServiceProvider.GetRequiredService<ScopedView>();

        Assert.Same(first, second);
        Assert.NotSame(first, third);
    }

    /// <summary>
    /// 模拟生成器产出的视图：无参构造函数负责初始化，带参构造函数注入视图模型。
    /// </summary>
    public class SampleView : ITransientDependency
    {
        /// <summary>
        /// 模拟 XAML 的 <c>InitializeComponent</c>。
        /// </summary>
        public SampleView()
        {
            ParameterlessConstructorCalled = true;
        }

        /// <summary>
        /// 注入视图模型并写入 <c>DataContext</c>。
        /// </summary>
        /// <param name="viewModel">视图模型。</param>
        public SampleView(SampleViewModel viewModel) : this()
        {
            DataContext = viewModel;
        }

        /// <summary>
        /// 获取视图模型。
        /// </summary>
        public SampleViewModel ViewModel => (SampleViewModel)DataContext!;

        /// <summary>
        /// 获取或设置数据上下文。
        /// </summary>
        public object? DataContext { get; private set; }

        /// <summary>
        /// 获取无参构造函数是否被执行。
        /// </summary>
        public bool ParameterlessConstructorCalled { get; }
    }

    /// <summary>
    /// 声明为单例的视图，用于验证生命周期标记接口生效。
    /// </summary>
    /// <param name="viewModel">视图模型。</param>
    public class SingletonView(SampleViewModel viewModel) : ISingletonDependency
    {
        /// <summary>
        /// 获取或设置数据上下文。
        /// </summary>
        public object? DataContext { get; } = viewModel;
    }

    /// <summary>
    /// 声明为作用域的视图，用于验证生命周期标记接口生效。
    /// </summary>
    /// <param name="viewModel">视图模型。</param>
    public class ScopedView(SampleViewModel viewModel) : IScopedDependency
    {
        /// <summary>
        /// 获取或设置数据上下文。
        /// </summary>
        public object? DataContext { get; } = viewModel;
    }

    /// <summary>
    /// 测试用视图模型。
    /// </summary>
    public class SampleViewModel : ITransientDependency;

    [DependsOn(typeof(AbpAutofacModule))]
    public sealed class AutofacHostModule : AbpModule;

    public sealed class DefaultHostModule : AbpModule;
}
