using Len.Abp.Wpf.Navigation;
using Xunit;

namespace Len.Abp.Wpf.Tests;

/// <summary>
/// 覆盖 <see cref="NavigationContext"/> 的查询字符串解析与参数合并。
/// </summary>
public class NavigationContextTests
{
    /// <summary>
    /// 从 URI 的查询字符串解析出参数。
    /// </summary>
    [Fact]
    public void Constructor_ParsesQueryString()
    {
        var context = new NavigationContext(new Uri("demo?id=42&name=abc", UriKind.Relative));

        Assert.Equal(2, context.Parameters.Count);
        Assert.Equal(42, context.Parameters.GetValue<int>("id"));
        Assert.Equal("abc", context.Parameters.GetValue<string>("name"));
    }

    /// <summary>
    /// URI 没有查询字符串时参数集合为空。
    /// </summary>
    [Fact]
    public void Constructor_WithoutQuery_HasEmptyParameters()
    {
        var context = new NavigationContext(new Uri("demo", UriKind.Relative));

        Assert.Empty(context.Parameters.Keys);
    }

    /// <summary>
    /// 附加参数会合并到查询字符串解析结果中。
    /// </summary>
    [Fact]
    public void Constructor_MergesAdditionalParameters()
    {
        var extra = IParameters.Create().Add("token", "xyz");

        var context = new NavigationContext(new Uri("demo?id=42", UriKind.Relative), extra);

        Assert.Equal(42, context.Parameters.GetValue<int>("id"));
        Assert.Equal("xyz", context.Parameters.GetValue<string>("token"));
    }

    /// <summary>
    /// 附加参数与查询字符串同名时两者都保留，按添加顺序查找。
    /// </summary>
    [Fact]
    public void Constructor_DuplicateKey_KeepsQueryValueFirst()
    {
        var extra = IParameters.Create().Add("id", 99);

        var context = new NavigationContext(new Uri("demo?id=42", UriKind.Relative), extra);

        Assert.Equal(42, context.Parameters.GetValue<int>("id"));
        Assert.Equal([42, 99], context.Parameters.GetValues<int>("id"));
    }

    /// <summary>
    /// Uri 属性保存传入的原始 URI。
    /// </summary>
    [Fact]
    public void Constructor_KeepsUri()
    {
        var uri = new Uri("demo?id=42", UriKind.Relative);

        var context = new NavigationContext(uri);

        Assert.Same(uri, context.Uri);
    }

    /// <summary>
    /// 传入 null 参数时不抛异常。
    /// </summary>
    [Fact]
    public void Constructor_NullParameters_IsTolerated()
    {
        var context = new NavigationContext(new Uri("demo?id=42", UriKind.Relative), null);

        Assert.Equal(42, context.Parameters.GetValue<int>("id"));
    }
}
