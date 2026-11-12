using Len.Abp.Core;
using System.Reflection;
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
    [Fact]
    public void Module_DependsOnCoreModule()
    {
        var dependsOn = typeof(LenAbpWpfAbstractionsModule)
            .GetCustomAttributes(typeof(DependsOnAttribute), inherit: false)
            .Cast<DependsOnAttribute>()
            .SelectMany(attribute => attribute.DependedTypes);

        Assert.Contains(typeof(LenAbpCoreModule), dependsOn);
    }

    /// <summary>
    /// 抽象层模块不覆盖初始化钩子，静态提供程序只由核心模块设置一次。
    /// </summary>
    /// <remarks>
    /// 两个模块都调用 <see cref="AbpServiceProvider.Initialize"/> 时，后执行的那个会覆盖前者。
    /// 目前两者传入同一个提供程序，结果相同；但一旦某个模块改用作用域提供程序，
    /// 静态入口就会指向错误的容器。初始化职责因此只保留在核心模块。
    /// </remarks>
    [Fact]
    public void Module_DoesNotOverridePreApplicationInitialization()
    {
        var method = typeof(LenAbpWpfAbstractionsModule).GetMethod(
            nameof(AbpModule.OnPreApplicationInitialization),
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly
        );

        Assert.Null(method);
    }

    [DependsOn(typeof(LenAbpWpfAbstractionsModule))]
    public sealed class AbstractionsModule : AbpModule;
}
