using Len.Abp.Wpf;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Autofac;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Modularity;
using Xunit;

namespace Len.Abp.Wpf.Tests;

/// <summary>
/// 验证 <c>MvvmGenerator</c> 所依赖的生命周期契约。
/// Shell 视图必须是单例，这样两次解析才能得到同一个窗口；
/// 在 <see cref="IShell"/> 之上再加 <c>ITransientDependency</c> 会静默破坏该契约。
/// </summary>
public class ShellLifetimeTests
{
    /// <summary>
    /// 仅实现 IShell 的类型解析为单例。
    /// </summary>
    [Fact]
    public async Task TypeImplementingIShellOnly_IsSingleton()
    {
        using var application = await AbpApplicationFactory.CreateAsync<LifetimeModule>();
        await application.InitializeAsync();

        var first = application.ServiceProvider.GetRequiredService<ShellOnly>();
        var second = application.ServiceProvider.GetRequiredService<ShellOnly>();

        Assert.Same(first, second);
    }

    /// <summary>
    /// 同时声明 ITransientDependency 的类型解析为瞬态。
    /// </summary>
    [Fact]
    public async Task TypeImplementingIShellAndTransient_IsTransient()
    {
        // 这是 MvvmGenerator 过去为 IsShell = true 生成的形态。
        // 它说明了为什么生成器不能给 Shell 添加 ITransientDependency。
        using var application = await AbpApplicationFactory.CreateAsync<LifetimeModule>();
        await application.InitializeAsync();

        var first = application.ServiceProvider.GetRequiredService<ShellAndTransient>();
        var second = application.ServiceProvider.GetRequiredService<ShellAndTransient>();

        Assert.NotSame(first, second);
    }

    [DependsOn(typeof(AbpAutofacModule))]
    public sealed class LifetimeModule : AbpModule;

    [ExposeServices(typeof(IShell), typeof(ShellOnly))]
    public sealed class ShellOnly : IShell;

    [ExposeServices(typeof(IShell), typeof(ShellAndTransient))]
    public sealed class ShellAndTransient : IShell, ITransientDependency;
}
