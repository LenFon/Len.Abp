using Len.Abp.Wpf.Mvvm;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Len.Abp.Wpf.Abstractions.Tests;

/// <summary>
/// 覆盖 <see cref="ViewAttribute"/> 及其泛型版本。
/// </summary>
public class ViewAttributeTests
{
    /// <summary>
    /// 构造函数保存视图模型类型，默认不是 Shell。
    /// </summary>
    [Fact]
    public void Constructor_SetsViewModelAndDefaults()
    {
        var attribute = new ViewAttribute(typeof(SampleViewModel));

        Assert.Equal(typeof(SampleViewModel), attribute.ViewModel);
        Assert.False(attribute.IsShell);
        Assert.Null(attribute.ViewName);
    }

    /// <summary>
    /// 所有可选属性都可读写。
    /// </summary>
    [Fact]
    public void OptionalProperties_AreSettable()
    {
        var attribute = new ViewAttribute(typeof(SampleViewModel))
        {
            IsShell = true,
            ViewName = "main",
        };

        Assert.True(attribute.IsShell);
        Assert.Equal("main", attribute.ViewName);
    }

    /// <summary>
    /// 泛型版本从类型参数推导视图模型类型。
    /// </summary>
    [Fact]
    public void GenericConstructor_UsesTypeArgument()
    {
        var attribute = new ViewAttribute<SampleViewModel>();

        Assert.Equal(typeof(SampleViewModel), attribute.ViewModel);
    }

    /// <summary>
    /// 生命周期默认为瞬态。
    /// </summary>
    [Fact]
    public void Lifetime_DefaultsToTransient()
    {
        var attribute = new ViewAttribute(typeof(SampleViewModel));

        Assert.Equal(ServiceLifetime.Transient, attribute.Lifetime);
    }

    /// <summary>
    /// 生命周期可设置为 MS DI 的任意取值。
    /// </summary>
    [Theory]
    [InlineData(ServiceLifetime.Singleton)]
    [InlineData(ServiceLifetime.Scoped)]
    [InlineData(ServiceLifetime.Transient)]
    public void Lifetime_IsSettable(ServiceLifetime lifetime)
    {
        var attribute = new ViewAttribute(typeof(SampleViewModel)) { Lifetime = lifetime };

        Assert.Equal(lifetime, attribute.Lifetime);
    }

    /// <summary>
    /// 未显式指定视图模型生命周期时，标记位为 false，视图模型跟随视图的生命周期。
    /// </summary>
    [Fact]
    public void ViewModelLifetime_DefaultsToTransientWithoutExplicitFlag()
    {
        var attribute = new ViewAttribute(typeof(SampleViewModel));

        Assert.Equal(ServiceLifetime.Transient, attribute.ViewModelLifetime);
        Assert.False(attribute.HasViewModelLifetime);
    }

    /// <summary>
    /// 显式指定视图模型生命周期时，标记位与取值一起生效。
    /// </summary>
    [Fact]
    public void ViewModelLifetime_ExplicitValue_SetsFlag()
    {
        var attribute = new ViewAttribute(typeof(SampleViewModel))
        {
            ViewModelLifetime = ServiceLifetime.Singleton,
            HasViewModelLifetime = true,
        };

        Assert.Equal(ServiceLifetime.Singleton, attribute.ViewModelLifetime);
        Assert.True(attribute.HasViewModelLifetime);
    }

    /// <summary>
    /// 必需参数声明可读写。
    /// </summary>
    [Fact]
    public void RequiredParameters_IsSettable()
    {
        var attribute = new ViewAttribute(typeof(SampleViewModel))
        {
            RequiredParameters = ["Id:int", "Source"],
        };

        Assert.Equal(["Id:int", "Source"], attribute.RequiredParameters);
    }

    /// <summary>
    /// 测试用视图模型。
    /// </summary>
    public sealed class SampleViewModel;
}
