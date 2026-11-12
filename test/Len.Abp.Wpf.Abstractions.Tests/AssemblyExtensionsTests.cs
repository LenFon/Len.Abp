using Len.Abp.Wpf;
using System.Reflection;
using Xunit;

namespace Len.Abp.Wpf.Abstractions.Tests;

/// <summary>
/// 覆盖 Assembly 扩展方法的资源读取与 WPF 程序集判定。
/// </summary>
public class AssemblyExtensionsTests
{
    /// <summary>
    /// 测试程序集没有 .g.resources 时返回空数组。
    /// </summary>
    [Fact]
    public void GetResourceNames_WithoutWpfResources_ReturnsEmpty()
    {
        var names = typeof(AssemblyExtensionsTests).Assembly.GetResourceNames();

        Assert.NotNull(names);
    }

    /// <summary>
    /// 不存在的资源名返回 false。
    /// </summary>
    [Fact]
    public void ExistsResource_MissingResource_ReturnsFalse()
    {
        Assert.False(typeof(AssemblyExtensionsTests).Assembly.ExistsResource("definitely-not-a-resource"));
    }

    /// <summary>
    /// 传入 null 程序集时抛出异常。
    /// </summary>
    [Fact]
    public void ExistsResource_NullAssembly_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => ((Assembly)null!).ExistsResource("x"));
    }

    /// <summary>
    /// 传入 null 资源名时抛出异常。
    /// </summary>
    [Fact]
    public void ExistsResource_NullName_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => typeof(AssemblyExtensionsTests).Assembly.ExistsResource(null!));
    }

    /// <summary>
    /// 传入 null 程序集时 GetResourceNames 抛出异常。
    /// </summary>
    [Fact]
    public void GetResourceNames_NullAssembly_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => ((Assembly)null!).GetResourceNames());
    }

    /// <summary>
    /// 测试程序集引用了 WPF 核心程序集，因此被判定为 WPF 程序集。
    /// </summary>
    [Fact]
    public void IsWpfAssembly_ForWpfTestAssembly_ReturnsTrue()
    {
        Assert.True(typeof(AssemblyExtensionsTests).Assembly.IsWpfAssembly());
    }

    /// <summary>
    /// 纯 .NET 程序集不被判定为 WPF 程序集。
    /// </summary>
    [Fact]
    public void IsWpfAssembly_ForNonWpfAssembly_ReturnsFalse()
    {
        Assert.False(typeof(object).Assembly.IsWpfAssembly());
    }

    /// <summary>
    /// 传入 null 程序集时抛出异常。
    /// </summary>
    [Fact]
    public void IsWpfAssembly_NullAssembly_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => ((Assembly)null!).IsWpfAssembly());
    }
}
