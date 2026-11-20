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

    /// <summary>
    /// 空相对 URI 归一化为根路径，视图名为空字符串。
    /// </summary>
    /// <remarks>
    /// 导航服务在入口处已拒绝空视图名，这里锁定的是归一化本身的边界行为：
    /// 不抛异常，而是给出确定结果。
    /// </remarks>
    [Fact]
    public void EnsureAbsolute_EmptyRelativeUri_BecomesRoot()
    {
        var result = NavigationUri.EnsureAbsolute(new Uri(string.Empty, UriKind.Relative));

        Assert.Equal("http://localhost/", result.AbsoluteUri);
        Assert.Equal(string.Empty, NavigationUri.GetViewName(new Uri(string.Empty, UriKind.Relative)));
    }

    /// <summary>
    /// 只有斜杠的相对 URI 归一化为根路径。
    /// </summary>
    [Fact]
    public void GetViewName_RootOnlyUri_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, NavigationUri.GetViewName(new Uri("/", UriKind.Relative)));
    }

    /// <summary>
    /// 多个前导斜杠全部被去掉。
    /// </summary>
    [Fact]
    public void GetViewName_MultipleLeadingSlashes_AreTrimmed()
    {
        Assert.Equal("demo", NavigationUri.GetViewName(new Uri("///demo", UriKind.Relative)));
    }

    /// <summary>
    /// 只有查询字符串、没有路径时视图名为空，查询仍被保留。
    /// </summary>
    [Fact]
    public void GetViewName_QueryOnlyUri_ReturnsEmptyPath()
    {
        var uri = new Uri("?x=1", UriKind.Relative);

        Assert.Equal(string.Empty, NavigationUri.GetViewName(uri));
        Assert.Equal("?x=1", NavigationUri.GetQuery(uri));
    }

    /// <summary>
    /// 查询字符串为空（只有一个问号）时返回 "?"，而不是空字符串。
    /// </summary>
    /// <remarks>
    /// <see cref="Uri.Query"/> 的既有行为：只要原始字符串带 <c>?</c> 就保留它。
    /// 下游 <c>IParameters.Create</c> 会把它解析成空集合，因此不影响导航。
    /// </remarks>
    [Fact]
    public void GetQuery_EmptyQuery_ReturnsQuestionMark()
    {
        Assert.Equal("?", NavigationUri.GetQuery(new Uri("demo?", UriKind.Relative)));
    }

    /// <summary>
    /// 绝对 URI 的视图名同样去掉前导斜杠。
    /// </summary>
    [Fact]
    public void GetViewName_AbsoluteUri_ReturnsPathWithoutLeadingSlash()
    {
        Assert.Equal("a/b", NavigationUri.GetViewName(new Uri("https://example.com/a/b?x=1")));
    }

    /// <summary>
    /// 超长视图名不截断。
    /// </summary>
    [Fact]
    public void GetViewName_VeryLongPath_IsNotTruncated()
    {
        var name = new string('v', 5_000);

        Assert.Equal(name, NavigationUri.GetViewName(new Uri(name, UriKind.Relative)));
    }

    /// <summary>
    /// 非 ASCII 视图名被还原为原始文本。
    /// </summary>
    /// <remarks>
    /// 回归：<see cref="Uri.AbsolutePath"/> 返回转义后的形式
    /// （中文变成 <c>%E9%A1%B5%E9%9D%A2</c>），与注册时写入的视图名永远不相等，
    /// 导航必然失败。这里锁定"还原转义"这条契约。
    /// </remarks>
    [Theory]
    [InlineData("页面", "页面")]
    [InlineData("用户列表", "用户列表")]
    [InlineData("/页面", "页面")]
    [InlineData("页面?x=1", "页面")]
    public void GetViewName_NonAsciiName_IsUnescaped(string input, string expected)
    {
        Assert.Equal(expected, NavigationUri.GetViewName(new Uri(input, UriKind.Relative)));
    }

    /// <summary>
    /// 含空格的视图名被还原为原始文本。
    /// </summary>
    [Theory]
    [InlineData("my view", "my view")]
    [InlineData("a b c", "a b c")]
    [InlineData("my view?x=1", "my view")]
    public void GetViewName_NameWithSpaces_IsUnescaped(string input, string expected)
    {
        Assert.Equal(expected, NavigationUri.GetViewName(new Uri(input, UriKind.Relative)));
    }

    /// <summary>
    /// 字面百分号能正确往返。
    /// </summary>
    /// <remarks>
    /// <see cref="Uri"/> 把字面 <c>%</c> 转义成 <c>%25</c>，还原后必须回到单个 <c>%</c>。
    /// 这条用例同时覆盖"没有 <c>%</c> 时走快路径"与"有 <c>%</c> 时走还原"两条分支。
    /// </remarks>
    [Theory]
    [InlineData("100%", "100%")]
    [InlineData("50%off", "50%off")]
    public void GetViewName_LiteralPercent_RoundTrips(string input, string expected)
    {
        Assert.Equal(expected, NavigationUri.GetViewName(new Uri(input, UriKind.Relative)));
    }

    /// <summary>
    /// 畸形转义序列不抛异常，退化为原文。
    /// </summary>
    /// <remarks>
    /// 使用者可能把 <c>%ZZ</c> 写进视图名。<see cref="Uri"/> 会把它转义成 <c>%25ZZ</c>，
    /// 还原后回到 <c>%ZZ</c>——导航失败时看到的是自己写的名称，而不是异常堆栈。
    /// </remarks>
    [Fact]
    public void GetViewName_MalformedEscape_DoesNotThrow()
    {
        Assert.Equal("demo%ZZ", NavigationUri.GetViewName(new Uri("demo%ZZ", UriKind.Relative)));
    }

    /// <summary>
    /// 纯 ASCII 视图名不受还原影响。
    /// </summary>
    /// <remarks>
    /// 还原对没有 <c>%</c> 的字符串是恒等操作，这里锁定既有行为不回归。
    /// </remarks>
    [Theory]
    [InlineData("demo", "demo")]
    [InlineData("lifetime-scoped", "lifetime-scoped")]
    [InlineData("a/b", "a/b")]
    [InlineData("tab-page-1", "tab-page-1")]
    public void GetViewName_AsciiName_IsUnchanged(string input, string expected)
    {
        Assert.Equal(expected, NavigationUri.GetViewName(new Uri(input, UriKind.Relative)));
    }

    /// <summary>
    /// 绝对 URI 中的非 ASCII 路径同样被还原。
    /// </summary>
    [Fact]
    public void GetViewName_AbsoluteUriWithNonAsciiPath_IsUnescaped()
    {
        Assert.Equal("页面", NavigationUri.GetViewName(new Uri("https://example.com/页面?x=1")));
    }
}
