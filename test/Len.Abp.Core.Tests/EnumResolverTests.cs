using Len.Abp.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Volo.Abp;
using Volo.Abp.Localization;
using Volo.Abp.Modularity;
using Xunit;

namespace Len.Abp.Core.Tests;

/// <summary>
/// 覆盖 <see cref="EnumResolver{TEnum}"/> 基于 ABP 枚举本地化的默认实现。
/// </summary>
/// <remarks>
/// 这些用例通过完整的 ABP 应用启动，验证模块注册、本地化回退与覆盖行为，
/// 而不是直接构造解析器，因此能同时覆盖依赖注入接线。
/// </remarks>
[Collection(GlobalServiceProviderCollection.Name)]
public class EnumResolverTests
{
    /// <summary>
    /// 模块注册了开放泛型的默认实现，可直接从容器解析。
    /// </summary>
    [Fact]
    public async Task Module_RegistersDefaultResolver()
    {
        using var application = await AbpApplicationFactory.CreateAsync<ResolverModule>();
        await application.InitializeAsync();

        var resolver = application.ServiceProvider.GetRequiredService<IEnumResolver<SampleEnum>>();

        Assert.IsType<EnumResolver<SampleEnum>>(resolver);
    }

    /// <summary>
    /// 没有本地化资源时回退到枚举成员名。
    /// </summary>
    [Fact]
    public async Task GetNameValues_WithoutLocalization_FallsBackToMemberNames()
    {
        using var application = await AbpApplicationFactory.CreateAsync<ResolverModule>();
        await application.InitializeAsync();

        var resolver = application.ServiceProvider.GetRequiredService<IEnumResolver<SampleEnum>>();
        var values = resolver.GetNameValues().ToArray();

        Assert.Equal([nameof(SampleEnum.First), nameof(SampleEnum.Second)], values.Select(v => v.Name));
        Assert.Equal([SampleEnum.First, SampleEnum.Second], values.Select(v => v.Value));
    }

    /// <summary>
    /// 名称值集合按枚举声明顺序返回。
    /// </summary>
    [Fact]
    public async Task GetNameValues_PreservesDeclarationOrder()
    {
        using var application = await AbpApplicationFactory.CreateAsync<ResolverModule>();
        await application.InitializeAsync();

        var resolver = application.ServiceProvider.GetRequiredService<IEnumResolver<SampleEnum>>();
        var values = resolver.GetNameValues().Select(v => v.Value).ToArray();

        Assert.Equal([SampleEnum.First, SampleEnum.Second], values);
    }

    /// <summary>
    /// 本地化资源提供翻译时，名称取翻译文本；未提供翻译的成员回退到成员名。
    /// </summary>
    [Fact]
    public async Task GetNameValues_WithLocalization_UsesTranslatedNames()
    {
        using var application = await AbpApplicationFactory.CreateAsync<LocalizedResolverModule>();
        await application.InitializeAsync();

        var resolver = application.ServiceProvider.GetRequiredService<IEnumResolver<SampleEnum>>();
        var values = resolver.GetNameValues().ToArray();

        Assert.Equal(["第一项", nameof(SampleEnum.Second)], values.Select(v => v.Name));
        Assert.Equal([SampleEnum.First, SampleEnum.Second], values.Select(v => v.Value));
    }

    /// <summary>
    /// 单个取值同样使用本地化名称。
    /// </summary>
    [Fact]
    public async Task GetNameValue_WithLocalization_UsesTranslatedName()
    {
        using var application = await AbpApplicationFactory.CreateAsync<LocalizedResolverModule>();
        await application.InitializeAsync();

        var resolver = application.ServiceProvider.GetRequiredService<IEnumResolver<SampleEnum>>();
        var value = resolver.GetNameValue(SampleEnum.First);

        Assert.Equal("第一项", value.Name);
        Assert.Equal(SampleEnum.First, value.Value);
    }

    /// <summary>
    /// 未定义的值没有本地化名称，回退到数值文本，而不是返回 null。
    /// </summary>
    /// <remarks>
    /// <see cref="NameValue{T}.Name"/> 声明为非空，返回 null 会让调用方在绑定显示时拿到空字符串。
    /// </remarks>
    [Fact]
    public async Task GetNameValue_UndefinedValue_FallsBackToNumericText()
    {
        using var application = await AbpApplicationFactory.CreateAsync<ResolverModule>();
        await application.InitializeAsync();

        var resolver = application.ServiceProvider.GetRequiredService<IEnumResolver<SampleEnum>>();
        var value = resolver.GetNameValue((SampleEnum)99);

        Assert.Equal("99", value.Name);
        Assert.Equal((SampleEnum)99, value.Value);
    }

    /// <summary>
    /// <see cref="FlagsAttribute"/> 组合值没有单一成员名，回退到以逗号分隔的成员名。
    /// </summary>
    [Fact]
    public async Task GetNameValue_FlagsCombination_FallsBackToMemberNames()
    {
        using var application = await AbpApplicationFactory.CreateAsync<ResolverModule>();
        await application.InitializeAsync();

        var resolver = application.ServiceProvider.GetRequiredService<IEnumResolver<FlagsEnum>>();
        var value = resolver.GetNameValue(FlagsEnum.A | FlagsEnum.B);

        Assert.Equal("A, B", value.Name);
    }

    /// <summary>
    /// 空枚举返回空集合，而不是抛异常。
    /// </summary>
    [Fact]
    public async Task GetNameValues_EmptyEnum_ReturnsEmptyCollection()
    {
        using var application = await AbpApplicationFactory.CreateAsync<ResolverModule>();
        await application.InitializeAsync();

        var resolver = application.ServiceProvider.GetRequiredService<IEnumResolver<EmptyEnum>>();

        Assert.Empty(resolver.GetNameValues());
    }

    /// <summary>
    /// 业务代码注册的实现可以覆盖默认实现。
    /// </summary>
    [Fact]
    public async Task CustomResolver_OverridesDefault()
    {
        using var application = await AbpApplicationFactory.CreateAsync<OverriddenResolverModule>();
        await application.InitializeAsync();

        var resolver = application.ServiceProvider.GetRequiredService<IEnumResolver<SampleEnum>>();

        Assert.IsType<CustomResolver>(resolver);
    }

    /// <summary>
    /// 测试用枚举。
    /// </summary>
    public enum SampleEnum
    {
        First = 1,
        Second = 2,
    }

    /// <summary>
    /// 用于验证组合值回退行为的标志枚举。
    /// </summary>
    [Flags]
    public enum FlagsEnum
    {
        None = 0,
        A = 1,
        B = 2,
    }

    /// <summary>
    /// 没有任何成员的枚举。
    /// </summary>
    public enum EmptyEnum;

    [DependsOn(typeof(LenAbpCoreModule))]
    public sealed class ResolverModule : AbpModule;

    /// <summary>
    /// 配置默认本地化资源，并为其提供一条翻译。
    /// </summary>
    [DependsOn(typeof(LenAbpCoreModule))]
    public sealed class LocalizedResolverModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<AbpLocalizationOptions>(options =>
            {
                options.Resources
                    .Add<ResolverResource>("en")
                    .Contributors.Add(new FixedContributor());

                options.DefaultResourceType = typeof(ResolverResource);
            });
        }
    }

    /// <summary>
    /// 注册自定义实现以覆盖默认行为。
    /// </summary>
    [DependsOn(typeof(LenAbpCoreModule))]
    public sealed class OverriddenResolverModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddTransient<IEnumResolver<SampleEnum>, CustomResolver>();
        }
    }

    [LocalizationResourceName("ResolverTest")]
    public sealed class ResolverResource;

    private sealed class CustomResolver : IEnumResolver<SampleEnum>
    {
        public NameValue<SampleEnum> GetNameValue(SampleEnum value) => new("custom", value);

        public IEnumerable<NameValue<SampleEnum>> GetNameValues() => [new("custom", SampleEnum.First)];
    }

    /// <summary>
    /// 只为 <see cref="SampleEnum.First"/> 提供翻译，其余键返回 null 以走回退逻辑。
    /// </summary>
    private sealed class FixedContributor : ILocalizationResourceContributor
    {
        public bool IsDynamic => false;

        public void Initialize(LocalizationResourceInitializationContext context) { }

        public LocalizedString? GetOrNull(string cultureName, string name) =>
            name == "Enum:SampleEnum.First" ? new LocalizedString(name, "第一项") : null;

        public void Fill(string cultureName, Dictionary<string, LocalizedString> dictionary) { }

        public Task FillAsync(string cultureName, Dictionary<string, LocalizedString> dictionary) =>
            Task.CompletedTask;

        public Task<IEnumerable<string>> GetSupportedCulturesAsync() =>
            Task.FromResult<IEnumerable<string>>(["en"]);
    }
}
