using Len.Abp.Analyzer;
using Xunit;

namespace Len.Abp.Analyzer.Wpf.Tests;

/// <summary>
/// 覆盖生成器数据解析的入参守卫。
/// </summary>
/// <remarks>
/// 这些解析方法在生成器管线里由增量管道调用，理论上不会收到 null；
/// 但它们是 internal 的共享工具，显式守卫能在误用时给出明确异常，
/// 而不是在后续解引用处抛出难以定位的 <see cref="NullReferenceException"/>。
/// </remarks>
public class GeneratorDataParseTests
{
    /// <summary>
    /// <see cref="MvvmData.Parse"/> 对 null 特性抛出异常。
    /// </summary>
    [Fact]
    public void MvvmData_ParseNull_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => MvvmData.Parse(null!));
    }

    /// <summary>
    /// <see cref="RoutedEventData.Parse"/> 对 null 特性抛出异常。
    /// </summary>
    [Fact]
    public void RoutedEventData_ParseNull_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => RoutedEventData.Parse(null!));
    }

    /// <summary>
    /// 没有特性时去重判定返回 false，而不是抛异常。
    /// </summary>
    [Fact]
    public void AttributeDeduplication_NoAttributes_ReturnsFalse()
    {
        var context = new ClassContext(null!, [], null!, null!);
        Assert.False(AttributeDeduplication.IsFirstOfType(context));
    }
}
