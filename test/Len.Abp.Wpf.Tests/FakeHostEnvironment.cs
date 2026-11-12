using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Len.Abp.Wpf.Tests;

/// <summary>
/// 测试用的宿主环境替身。
/// </summary>
internal sealed class FakeHostEnvironment(string applicationName) : IHostEnvironment
{
    /// <inheritdoc/>
    public string EnvironmentName { get; set; } = "Test";

    /// <inheritdoc/>
    public string ApplicationName { get; set; } = applicationName;

    /// <inheritdoc/>
    public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

    /// <inheritdoc/>
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
