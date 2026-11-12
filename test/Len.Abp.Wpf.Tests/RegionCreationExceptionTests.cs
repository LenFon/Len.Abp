using Len.Abp.Wpf.Navigation.Regions;
using Xunit;

namespace Len.Abp.Wpf.Tests;

/// <summary>
/// 覆盖 <see cref="RegionCreationException"/> 的消息与内部异常。
/// </summary>
public class RegionCreationExceptionTests
{
    /// <summary>
    /// 异常消息包含区域名称，内部异常被保留。
    /// </summary>
    [Fact]
    public void Constructor_IncludesRegionNameAndInnerException()
    {
        var inner = new InvalidOperationException("boom");

        var ex = new RegionCreationException("MainRegion", inner);

        Assert.Contains("MainRegion", ex.Message);
        Assert.Same(inner, ex.InnerException);
    }
}
