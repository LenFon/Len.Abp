using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Modularity;
using Xunit;

namespace Len.Abp.Wpf.Tests;

/// <summary>
/// 验证 <see cref="IWpfExceptionHandler"/> 可被使用者替换，且框架调用的是替换后的实现。
/// </summary>
/// <remarks>
/// 默认实现是 <c>internal</c>，使用者只能通过接口替换，因此这里必须走 ABP 容器解析，
/// 而不是手工构造。替换用 ABP 的 <c>[Dependency(ReplaceServices = true)]</c>，
/// 与真实使用者的做法一致。
/// </remarks>
public class WpfExceptionHandlerReplacementTests
{
    /// <summary>
    /// 默认实现以接口形式注册，容器能解析到 <see cref="IWpfExceptionHandler"/>。
    /// </summary>
    /// <remarks>
    /// 不能走 <c>AbpApplicationFactory</c>：ABP 会扫描整个测试程序集，
    /// 本文件里的 <see cref="RecordingExceptionHandler"/> 会全局替换默认实现，
    /// 导致这条断言永远看到替换后的类型。这里直接注册默认实现验证其暴露的服务类型。
    /// </remarks>
    [Fact]
    public void DefaultImplementation_IsRegisteredAsInterface()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<WpfContext>();
        services.AddSingleton<IAbpLazyServiceProvider>(provider =>
            new AbpLazyServiceProvider(provider)
        );
        services.AddSingleton<WpfExceptionHandler>();
        services.AddSingleton<IWpfExceptionHandler>(provider =>
            provider.GetRequiredService<WpfExceptionHandler>()
        );

        var provider = services.BuildServiceProvider();

        Assert.IsType<WpfExceptionHandler>(provider.GetRequiredService<IWpfExceptionHandler>());
    }

    /// <summary>
    /// 使用者实现替换默认实现后，容器解析到的是使用者实现。
    /// </summary>
    [Fact]
    public async Task UserImplementation_ReplacesDefault()
    {
        using var application = await AbpApplicationFactory.CreateAsync<ReplacingModule>();
        var provider = application.Services.BuildServiceProvider();

        var handler = provider.GetRequiredService<IWpfExceptionHandler>();

        Assert.IsType<RecordingExceptionHandler>(handler);
    }

    /// <summary>
    /// 替换后 <see cref="WpfThreadService"/> 注入的是使用者实现。
    /// </summary>
    [Fact]
    public async Task WpfThreadService_UsesReplacedHandler()
    {
        using var application = await AbpApplicationFactory.CreateAsync<ReplacingModule>();
        var provider = application.Services.BuildServiceProvider();

        var service = provider.GetRequiredService<WpfThreadService>();
        var handler = provider.GetRequiredService<IWpfExceptionHandler>();

        Assert.NotNull(service);
        Assert.IsType<RecordingExceptionHandler>(handler);
    }

    /// <summary>
    /// 使用者实现可以自行决定是否让进程继续运行。
    /// </summary>
    [Fact]
    public void UserImplementation_ControlsHandledFlag()
    {
        var handler = new RecordingExceptionHandler { Handled = true };

        Assert.True(handler.HandleDispatcherException(new InvalidOperationException("测试")));
        Assert.Single(handler.DispatcherExceptions);

        handler.Handled = false;

        Assert.False(handler.HandleDispatcherException(new InvalidOperationException("测试")));
        Assert.Equal(2, handler.DispatcherExceptions.Count);
    }

    /// <summary>
    /// 使用者实现的后台异常回调会被调用，异常参数原样传入。
    /// </summary>
    [Fact]
    public void UserImplementation_ReceivesUnobservedTaskException()
    {
        var handler = new RecordingExceptionHandler();
        var exception = new InvalidOperationException("后台任务异常");

        handler.HandleUnobservedTaskException(exception);

        Assert.Same(exception, Assert.Single(handler.UnobservedExceptions));
    }

    /// <summary>
    /// 只依赖 WPF 运行时、不额外注册生命周期服务的模块。
    /// </summary>
    [DependsOn(typeof(LenAbpWpfModule))]
    public sealed class DefaultModule : AbpModule
    {
        /// <summary>
        /// 补上 Generic Host 才会注册的生命周期服务，使容器能完整解析宿主服务。
        /// </summary>
        /// <param name="context">服务配置上下文。</param>
        public override void ConfigureServices(ServiceConfigurationContext context) =>
            context.Services.AddSingleton<IHostApplicationLifetime, FakeHostApplicationLifetime>();
    }

    /// <summary>
    /// 在默认模块基础上替换异常处理实现。
    /// </summary>
    [DependsOn(typeof(DefaultModule))]
    public sealed class ReplacingModule : AbpModule;

    /// <summary>
    /// 使用者自定义的异常处理实现，记录收到的异常供断言。
    /// </summary>
    /// <remarks>
    /// 必须同时实现 <see cref="ISingletonDependency"/>：ABP 的约定注册只处理带生命周期标记的类型，
    /// 仅靠 <c>[Dependency]</c> + <c>[ExposeServices]</c> 不会注册。
    /// </remarks>
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(IWpfExceptionHandler))]
    public sealed class RecordingExceptionHandler : IWpfExceptionHandler, ISingletonDependency
    {
        /// <summary>
        /// 获取或设置 UI 线程异常的返回值。
        /// </summary>
        public bool Handled { get; set; }

        /// <summary>
        /// 获取收到的 UI 线程异常。
        /// </summary>
        public List<Exception> DispatcherExceptions { get; } = [];

        /// <summary>
        /// 获取收到的后台任务异常。
        /// </summary>
        public List<Exception> UnobservedExceptions { get; } = [];

        /// <inheritdoc/>
        public bool HandleDispatcherException(Exception exception)
        {
            DispatcherExceptions.Add(exception);
            return Handled;
        }

        /// <inheritdoc/>
        public void HandleUnobservedTaskException(Exception exception) =>
            UnobservedExceptions.Add(exception);
    }

    private sealed class FakeHostApplicationLifetime : IHostApplicationLifetime
    {
        public CancellationToken ApplicationStarted => CancellationToken.None;

        public CancellationToken ApplicationStopping => CancellationToken.None;

        public CancellationToken ApplicationStopped => CancellationToken.None;

        public void StopApplication()
        {
        }
    }
}
