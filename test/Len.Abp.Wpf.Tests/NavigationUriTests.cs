using Len.Abp.Wpf.Navigation;
using Xunit;

namespace Len.Abp.Wpf.Tests;

public class NavigationUriTests
{
    /// <summary>
    /// 绝对 URI 原样返回。
    /// </summary>
    [Fact]
    public void EnsureAbsolute_AbsoluteUri_IsReturnedUnchanged()
    {
        var uri = new Uri("https://example.com/path?x=1");

        Assert.Same(uri, NavigationUri.EnsureAbsolute(uri));
    }

    /// <summary>
    /// 相对 URI 会以 http://localhost 为根补全为绝对 URI。
    /// </summary>
    [Theory]
    [InlineData("demo", "http://localhost/demo")]
    [InlineData("/demo", "http://localhost/demo")]
    [InlineData("demo?x=1", "http://localhost/demo?x=1")]
    [InlineData("/demo?x=1", "http://localhost/demo?x=1")]
    public void EnsureAbsolute_RelativeUri_IsRootedAtLocalhost(string input, string expected)
    {
        var result = NavigationUri.EnsureAbsolute(new Uri(input, UriKind.Relative));

        Assert.Equal(expected, result.AbsoluteUri);
    }

    /// <summary>
    /// GetViewName 返回去掉前导斜杠的路径。
    /// </summary>
    [Theory]
    [InlineData("demo", "demo")]
    [InlineData("/demo", "demo")]
    [InlineData("demo?x=1", "demo")]
    [InlineData("/demo/sub?x=1", "demo/sub")]
    public void GetViewName_ReturnsPathWithoutLeadingSlash(string input, string expected)
    {
        Assert.Equal(expected, NavigationUri.GetViewName(new Uri(input, UriKind.Relative)));
    }

    /// <summary>
    /// GetQuery 返回包含前导问号的查询字符串。
    /// </summary>
    [Theory]
    [InlineData("demo?x=1", "?x=1")]
    [InlineData("/demo?x=1&y=2", "?x=1&y=2")]
    [InlineData("demo", "")]
    public void GetQuery_ReturnsQueryString(string input, string expected)
    {
        Assert.Equal(expected, NavigationUri.GetQuery(new Uri(input, UriKind.Relative)));
    }

    /// <summary>
    /// 传入 null URI 时抛出异常。
    /// </summary>
    [Fact]
    public void EnsureAbsolute_NullUri_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => NavigationUri.EnsureAbsolute(null!));
    }
}
