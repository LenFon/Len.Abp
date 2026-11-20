using Len.Abp.Core;
using Volo.Abp;
using Volo.Abp.Modularity;
using Xunit;

namespace Len.Abp.Wpf.Abstractions.Tests;

/// <summary>
/// 覆盖 WPF 抽象层模块对静态服务提供程序的依赖关系。
/// </summary>
/// <remarks>
/// 静态提供程序由 <see cref="LenAbpCoreModule"/> 初始化；抽象层模块依赖它，
/// 因此只需验证依赖链能正确完成初始化，而不应重复初始化。
/// </remarks>
[Collection(GlobalServiceProviderCollection.Name)]
public class LenAbpWpfAbstractionsModuleTests
{
    /// <summary>
    /// 通过抽象层模块启动时，静态入口由核心模块初始化并可解析容器中的服务。
    /// </summary>
    [Fact]
    public async Task Initialize_SetsStaticProviderThroughCoreModule()
    {
        using var application = await AbpApplicationFactory.CreateAsync<AbstractionsModule>();
        await application.InitializeAsync();

        // 模块初始化后静态入口应能解析容器中的服务。
        Assert.NotNull(AbpServiceProvider.Current);
        Assert.Same(application.ServiceProvider, AbpServiceProvider.Current);
    }

    /// <summary>
    /// 抽象层模块声明了对核心模块的依赖，保证静态入口一定被初始化。
    /// </summary>
    /// <remarks>
    /// 断言模块图里实际包含核心模块，而不是读 <see cref="DependsOnAttribute"/> 的反射结果：
    /// 模块图是 ABP 真正用来决定初始化顺序的东西，直接依赖它更贴近实际效果，
    /// 也避免 <c>GetCustomAttributes</c> 这类反射调用。
    /// </remarks>
    [Fact]
    public async Task Module_DependsOnCoreModule()
    {
        using var application = await AbpApplicationFactory.CreateAsync<AbstractionsModule>();
        await application.InitializeAsync();

        Assert.Contains(application.Modules, module => module.Type == typeof(LenAbpCoreModule));
    }

    /// <summary>
    /// 抽象层模块不覆盖初始化钩子，静态提供程序只由核心模块设置一次。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 两个模块都调用 <see cref="AbpServiceProvider.Initialize"/> 时，后执行的那个会覆盖前者。
    /// 目前两者传入同一个提供程序，结果相同；但一旦某个模块改用作用域提供程序，
    /// 静态入口就会指向错误的容器。初始化职责因此只保留在核心模块。
    /// </para>
    /// <para>
    /// 断言方式：初始化完成后静态入口仍指向根容器。若抽象层模块也覆盖了钩子并传入
    /// 初始化作用域，这里的引用比较会失败。这比"检查类型上是否声明了某个重写"更贴近
    /// 实际风险——后者只能证明当前没写，无法覆盖将来通过其它途径改写静态入口的情况。
    /// </para>
    /// </remarks>
    [Fact]
    public async Task Module_DoesNotOverridePreApplicationInitialization()
    {
        using var application = await AbpApplicationFactory.CreateAsync<AbstractionsModule>();
        await application.InitializeAsync();

        Assert.Same(application.ServiceProvider, AbpServiceProvider.Current);
    }

    [DependsOn(typeof(LenAbpWpfAbstractionsModule))]
    public sealed class AbstractionsModule : AbpModule;
}
