using Len.Abp.Wpf;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Windows;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Modularity;
using Xunit;

namespace Len.Abp.Wpf.Tests;

/// <summary>
/// 覆盖 <see cref="WpfThreadService"/> 的启动与退出流程。
/// </summary>
/// <remarks>
/// 一个 AppDomain 只能创建一个 <see cref="Application"/>，因此所有断言合并在同一个测试里，
/// 依次验证启动、资源合并、Shell 显示、调度器转发与退出联动。
/// </remarks>
[Collection(GlobalServiceProviderCollection.Name)]
public class WpfThreadServiceTests
{
    /// <summary>
    /// 启动后创建应用、合并模块资源、显示 Shell，并在应用退出时停止宿主。
    /// </summary>
    [Fact]
    public async Task StartAsync_RunsFullLifecycle()
    {
        var module = new RecordingWpfModule();
        var context = new WpfContext { IsLifetimeLinked = true };
        var lifetime = new FakeHostLifetime();
        var shellHolder = new ShellHolder();

        var (worker, service) = CreateWorker(context, lifetime, module, shellHolder);

        await worker.StartAsync();

        Assert.NotNull(context.WpfApplication);
        Assert.True(context.IsRunning);
        Assert.Same(context.WpfApplication.Dispatcher, context.Dispatcher);

        // StartUiThread 在 StartAsync 返回后才在 UI 线程上执行，这里等待它完成。
        await WaitUntilAsync(
            () => module.ConfigureResourcesCalled && shellHolder.Shell?.IsVisible == true,
            TimeSpan.FromSeconds(5)
        );

        Assert.True(module.ConfigureResourcesCalled);
        Assert.NotNull(shellHolder.Shell);
        Assert.True(shellHolder.Shell!.IsVisible);
        Assert.False(lifetime.StopRequested);

        // pack URI 的解析依赖 Application 上下文，这里在应用已启动的前提下验证。
        var dictionary = new UriResourceDictionary(
            "pack://application:,,,/Len.Abp.Wpf.Tests;component/TestResource.xaml"
        );
        Assert.True(dictionary.Source.IsAbsoluteUri);

        // 通过后台工作器停止，覆盖 StopAsync 的关闭路径。
        await worker.StopAsync();

        await WaitUntilAsync(() => !context.IsRunning, TimeSpan.FromSeconds(5));

        Assert.False(context.IsRunning);
        Assert.True(lifetime.StopRequested);
    }

    private static (WpfBackgroundWorker Worker, WpfThreadService Service) CreateWorker(
        WpfContext context,
        FakeHostLifetime lifetime,
        RecordingWpfModule module,
        ShellHolder shellHolder
    )
    {
        var application = new FakeAbpApplication
        {
            Modules = [new FakeModuleDescriptor(module)],
        };

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IAbpApplication>(application);
        services.AddSingleton(context);
        services.AddSingleton<IHostApplicationLifetime>(lifetime);
        services.Configure<WpfOptions>(_ => { });

        // Window 必须在创建它的线程上使用，因此由容器在 WPF 应用线程上延迟构造。
        services.AddSingleton<IShell>(_ =>
        {
            var shell = new RecordingShell();
            shellHolder.Shell = shell;
            return shell;
        });

        var provider = services.BuildServiceProvider();
        var lazyProvider = new AbpLazyServiceProvider(provider);

        services.AddSingleton(new WpfThreadService(lazyProvider, context, lifetime));
        provider = services.BuildServiceProvider();
        lazyProvider = new AbpLazyServiceProvider(provider);

        var service = new WpfThreadService(lazyProvider, context, lifetime);
        var worker = new WpfBackgroundWorker { LazyServiceProvider = lazyProvider };

        return (worker, service);
    }

    private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(20);
        }
    }

    /// <summary>
    /// 保存容器在 WPF 应用线程上创建的 Shell，供测试断言。
    /// </summary>
    private sealed class ShellHolder
    {
        public RecordingShell? Shell { get; set; }
    }

    private sealed class RecordingWpfModule : AbpWpfModule
    {
        public bool ConfigureResourcesCalled { get; private set; }

        public override void ConfigureResources(ICollection<ResourceDictionary> resources)
        {
            ConfigureResourcesCalled = true;
            resources.Add([]);
        }
    }

    private sealed class FakeModuleDescriptor(IAbpModule module) : IAbpModuleDescriptor
    {
        public Type Type => module.GetType();

        public System.Reflection.Assembly Assembly => module.GetType().Assembly;

        public System.Reflection.Assembly[] AllAssemblies => [Assembly];

        public IAbpModule Instance => module;

        public bool IsLoadedAsPlugIn => false;

        public IReadOnlyList<IAbpModuleDescriptor> Dependencies => [];
    }

    private sealed class RecordingShell : Window, IShell;
}
