using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace Len.Abp.Wpf.Tests;

/// <summary>
/// 测试用的 ABP 应用替身，模块列表为空。
/// </summary>
internal sealed class FakeAbpApplication : IAbpApplication
{
    /// <inheritdoc/>
    public Type StartupModuleType => typeof(FakeAbpApplication);

    /// <inheritdoc/>
    public IServiceCollection Services { get; } = new ServiceCollection();

    /// <inheritdoc/>
    public IServiceProvider ServiceProvider { get; } = new ServiceCollection().BuildServiceProvider();

    /// <inheritdoc/>
    public IReadOnlyList<IAbpModuleDescriptor> Modules { get; set; } = [];

    /// <inheritdoc/>
    public string ApplicationName => "Test";

    /// <inheritdoc/>
    public string InstanceId => Guid.NewGuid().ToString("N");

    /// <inheritdoc/>
    public Task ConfigureServicesAsync() => Task.CompletedTask;

    /// <inheritdoc/>
    public Task ShutdownAsync()
    {
        Shutdown();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void Shutdown() { }

    /// <inheritdoc/>
    public void Dispose() { }
}
