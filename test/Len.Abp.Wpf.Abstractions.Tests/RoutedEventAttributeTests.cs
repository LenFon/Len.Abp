using Len.Abp.Wpf.Mvvm;
using System.Windows;
using Xunit;

namespace Len.Abp.Wpf.Abstractions.Tests;

/// <summary>
/// 覆盖 <see cref="RoutedEventAttribute"/> 及其派生类型。
/// </summary>
public class RoutedEventAttributeTests
{
    /// <summary>
    /// 构造函数保存名称与路由策略，默认处理器类型为 RoutedEventHandler。
    /// </summary>
    [Fact]
    public void Constructor_SetsNameStrategyAndDefaultHandler()
    {
        var attribute = new RoutedEventAttribute("Clicked", RoutingStrategy.Bubble);

        Assert.Equal("Clicked", attribute.Name);
        Assert.Equal(RoutingStrategy.Bubble, attribute.Strategy);
        Assert.Equal(typeof(RoutedEventHandler), attribute.Type);
    }

    /// <summary>
    /// 名称为 null 时抛出异常。
    /// </summary>
    [Fact]
    public void Constructor_NullName_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new RoutedEventAttribute(null!, RoutingStrategy.Bubble));
    }

    /// <summary>
    /// 显式指定处理器类型时使用该类型。
    /// </summary>
    [Fact]
    public void Constructor_WithHandlerType_UsesIt()
    {
        var attribute = new RoutedEventAttribute("Clicked", RoutingStrategy.Tunnel, typeof(RoutedEventHandler));

        Assert.Equal(typeof(RoutedEventHandler), attribute.Type);
    }

    /// <summary>
    /// 分类与描述可读写。
    /// </summary>
    [Fact]
    public void OptionalProperties_AreSettable()
    {
        var attribute = new RoutedEventAttribute("Clicked", RoutingStrategy.Direct)
        {
            Category = "Input",
            Description = "点击事件",
        };

        Assert.Equal("Input", attribute.Category);
        Assert.Equal("点击事件", attribute.Description);
    }

    /// <summary>
    /// 泛型版本从类型参数推导处理器类型。
    /// </summary>
    [Fact]
    public void GenericConstructor_UsesTypeArgument()
    {
        var attribute = new RoutedEventAttribute<RoutedEventHandler>("Clicked", RoutingStrategy.Bubble);

        Assert.Equal(typeof(RoutedEventHandler), attribute.Type);
    }

    /// <summary>
    /// 附加路由事件继承名称、策略与处理器类型。
    /// </summary>
    [Fact]
    public void AttachedConstructor_InheritsBaseBehavior()
    {
        var attribute = new AttachedRoutedEventAttribute("Clicked", RoutingStrategy.Bubble);

        Assert.Equal("Clicked", attribute.Name);
        Assert.Equal(RoutingStrategy.Bubble, attribute.Strategy);
        Assert.Equal(typeof(RoutedEventHandler), attribute.Type);
    }

    /// <summary>
    /// 附加路由事件的泛型版本从类型参数推导处理器类型。
    /// </summary>
    [Fact]
    public void AttachedGenericConstructor_UsesTypeArgument()
    {
        var attribute = new AttachedRoutedEventAttribute<RoutedEventHandler>("Clicked", RoutingStrategy.Bubble);

        Assert.Equal(typeof(RoutedEventHandler), attribute.Type);
    }
}
