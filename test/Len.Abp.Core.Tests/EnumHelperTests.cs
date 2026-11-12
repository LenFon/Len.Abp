using Len.Abp.Core;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Xunit;

namespace Len.Abp.Core.Tests;

/// <summary>
/// 覆盖 <see cref="EnumHelper.GetNameValues{TEnum}"/> 通过静态提供程序解析枚举。
/// </summary>
/// <remarks>
/// <see cref="EnumHelper"/> 只是静态入口，职责是从全局提供程序取出解析器并转发调用。
/// 解析器本身的行为由 <see cref="EnumResolverTests"/> 覆盖。
/// </remarks>
[Collection(GlobalServiceProviderCollection.Name)]
public class EnumHelperTests
{
    /// <summary>
    /// 从容器解析 <see cref="IEnumResolver{TEnum}"/> 并返回其名称值集合。
    /// </summary>
    [Fact]
    public void GetNameValues_ResolvesResolverFromStaticProvider()
    {
        var expected = new[]
        {
            new NameValue<SampleEnum>("First", SampleEnum.First),
            new NameValue<SampleEnum>("Second", SampleEnum.Second),
        };

        var services = new ServiceCollection();
        services.AddSingleton<IEnumResolver<SampleEnum>>(new SampleEnumResolver(expected));
        AbpServiceProvider.Initialize(services.BuildServiceProvider());

        var actual = EnumHelper.GetNameValues<SampleEnum>();

        Assert.Equal(expected, actual);
    }

    /// <summary>
    /// 容器中没有注册解析器时抛出异常。
    /// </summary>
    [Fact]
    public void GetNameValues_WithoutResolver_Throws()
    {
        // 用一个不包含 IEnumResolver 的容器替换全局提供程序。
        AbpServiceProvider.Initialize(new ServiceCollection().BuildServiceProvider());

        Assert.Throws<InvalidOperationException>(() => EnumHelper.GetNameValues<SampleEnum>());
    }

    /// <summary>
    /// 测试用枚举。
    /// </summary>
    public enum SampleEnum
    {
        First = 1,
        Second = 2,
    }

    private sealed class SampleEnumResolver(NameValue<SampleEnum>[] values) : IEnumResolver<SampleEnum>
    {
        public NameValue<SampleEnum> GetNameValue(SampleEnum value) =>
            values.First(v => v.Value.Equals(value));

        public IEnumerable<NameValue<SampleEnum>> GetNameValues() => values;
    }
}
