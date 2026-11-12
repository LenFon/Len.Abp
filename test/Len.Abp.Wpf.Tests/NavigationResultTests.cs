using Len.Abp.Wpf.Navigation;
using Xunit;

namespace Len.Abp.Wpf.Tests;

/// <summary>
/// 覆盖 <see cref="NavigationResult"/> 的成功与失败构造。
/// </summary>
public class NavigationResultTests
{
    /// <summary>
    /// 成功结果保存上下文，异常为 null。
    /// </summary>
    [Fact]
    public void SuccessConstructor_SetsResultTrueAndNoException()
    {
        var context = new NavigationContext(new Uri("demo", UriKind.Relative));

        var result = new NavigationResult(context, true);

        Assert.True(result.Result);
        Assert.Null(result.Exception);
        Assert.Same(context, result.Context);
    }

    /// <summary>
    /// 失败结果保存异常，Result 为 false。
    /// </summary>
    [Fact]
    public void FailureConstructor_SetsResultFalseAndException()
    {
        var context = new NavigationContext(new Uri("demo", UriKind.Relative));
        var exception = new InvalidOperationException("boom");

        var result = new NavigationResult(context, exception);

        Assert.False(result.Result);
        Assert.Same(exception, result.Exception);
        Assert.Same(context, result.Context);
    }
}
