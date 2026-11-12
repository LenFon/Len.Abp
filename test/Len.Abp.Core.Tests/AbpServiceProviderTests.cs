using Len.Abp.Core;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Len.Abp.Core.Tests;

/// <summary>
/// 覆盖 <see cref="AbpServiceProvider"/> 的初始化与解析。
/// </summary>
[Collection(GlobalServiceProviderCollection.Name)]
public class AbpServiceProviderTests
{
    /// <summary>
    /// GetRequiredService 能从已初始化的提供程序解析服务。
    /// </summary>
    [Fact]
    public void GetRequiredService_ResolvesFromInitializedProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ISampleService, SampleService>();
        AbpServiceProvider.Initialize(services.BuildServiceProvider());

        var resolved = AbpServiceProvider.GetRequiredService<ISampleService>();

        Assert.IsType<SampleService>(resolved);
    }

    /// <summary>
    /// 再次初始化会替换之前的提供程序。
    /// </summary>
    [Fact]
    public void Initialize_ReplacesPreviousProvider()
    {
        var first = new ServiceCollection();
        first.AddSingleton<ISampleService>(new SampleService { Value = "first" });
        AbpServiceProvider.Initialize(first.BuildServiceProvider());

        var second = new ServiceCollection();
        second.AddSingleton<ISampleService>(new SampleService { Value = "second" });
        AbpServiceProvider.Initialize(second.BuildServiceProvider());

        Assert.Equal("second", AbpServiceProvider.GetRequiredService<ISampleService>().Value);
    }

    /// <summary>
    /// 传入 null 提供程序时抛出异常。
    /// </summary>
    [Fact]
    public void Initialize_NullProvider_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => AbpServiceProvider.Initialize(null!));
    }

    /// <summary>
    /// 测试用的示例服务接口。
    /// </summary>
    public interface ISampleService
    {
        /// <summary>
        /// 获取服务的标识值。
        /// </summary>
        string Value { get; }
    }

    /// <summary>
    /// <see cref="ISampleService"/> 的测试实现。
    /// </summary>
    public sealed class SampleService : ISampleService
    {
        /// <inheritdoc/>
        public string Value { get; init; } = "default";
    }
}
