using Len.Abp.Wpf;
using System.Windows;
using Xunit;

namespace Len.Abp.Wpf.Abstractions.Tests;

/// <summary>
/// 覆盖 <see cref="UriResourceDictionary"/> 的构造。
/// </summary>
/// <remarks>
/// <see cref="ResourceDictionary.Source"/> 会立即加载目标资源，pack URI 的解析又依赖
/// <see cref="Application"/> 上下文，因此成功路径在 <c>WpfThreadServiceTests</c> 中
/// 借助已启动的应用验证；这里只覆盖无需应用上下文的失败路径。
/// </remarks>
public class UriResourceDictionaryTests
{
    /// <summary>
    /// 指向不存在资源的 URI 会抛出异常。
    /// </summary>
    [Fact]
    public void Constructor_WithMissingResource_Throws()
    {
        Assert.ThrowsAny<Exception>(
            () => new UriResourceDictionary("pack://application:,,,/Len.Abp.Wpf.Abstractions.Tests;component/Missing.xaml")
        );
    }

    /// <summary>
    /// 相对 URI 无法被 WPF 解析，构造时抛出异常。
    /// </summary>
    [Fact]
    public void Constructor_WithRelativeUri_Throws()
    {
        Assert.ThrowsAny<Exception>(
            () => new UriResourceDictionary("Styles.xaml", UriKind.Relative)
        );
    }
}
