using Len.Abp.Wpf.Mvvm;
using System.Windows;
using System.Windows.Data;
using Xunit;

namespace Len.Abp.Wpf.Abstractions.Tests;

/// <summary>
/// 覆盖 <see cref="DependencyPropertyAttribute"/> 及其泛型版本的属性读写。
/// </summary>
public class DependencyPropertyAttributeTests
{
    /// <summary>
    /// 构造函数保存名称与类型。
    /// </summary>
    [Fact]
    public void Constructor_SetsNameAndType()
    {
        var attribute = new DependencyPropertyAttribute("Title", typeof(string));

        Assert.Equal("Title", attribute.Name);
        Assert.Equal(typeof(string), attribute.Type);
    }

    /// <summary>
    /// 名称为 null 时抛出异常。
    /// </summary>
    [Fact]
    public void Constructor_NullName_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new DependencyPropertyAttribute(null!, typeof(string)));
    }

    /// <summary>
    /// 类型为 null 时抛出异常。
    /// </summary>
    [Fact]
    public void Constructor_NullType_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new DependencyPropertyAttribute("Title", null!));
    }

    /// <summary>
    /// 所有可选属性都可读写。
    /// </summary>
    [Fact]
    public void OptionalProperties_AreSettable()
    {
        var attribute = new DependencyPropertyAttribute("Title", typeof(string))
        {
            Category = "Appearance",
            DefaultValue = "Untitled",
            DefaultValueExpression = "new()",
            Description = "标题",
            Flags = FrameworkPropertyMetadataOptions.AffectsRender,
            IsReadOnly = true,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
        };

        Assert.Equal("Appearance", attribute.Category);
        Assert.Equal("Untitled", attribute.DefaultValue);
        Assert.Equal("new()", attribute.DefaultValueExpression);
        Assert.Equal("标题", attribute.Description);
        Assert.Equal(FrameworkPropertyMetadataOptions.AffectsRender, attribute.Flags);
        Assert.True(attribute.IsReadOnly);
        Assert.Equal(UpdateSourceTrigger.PropertyChanged, attribute.UpdateSourceTrigger);
    }

    /// <summary>
    /// 默认值符合预期。
    /// </summary>
    [Fact]
    public void Defaults_AreExpected()
    {
        var attribute = new DependencyPropertyAttribute("Title", typeof(string));

        Assert.Null(attribute.Category);
        Assert.Null(attribute.DefaultValue);
        Assert.Null(attribute.DefaultValueExpression);
        Assert.Null(attribute.Description);
        Assert.Equal(FrameworkPropertyMetadataOptions.None, attribute.Flags);
        Assert.False(attribute.IsReadOnly);
        Assert.Equal(UpdateSourceTrigger.Default, attribute.UpdateSourceTrigger);
    }

    /// <summary>
    /// 泛型版本从类型参数推导属性类型。
    /// </summary>
    [Fact]
    public void GenericConstructor_UsesTypeArgument()
    {
        var attribute = new DependencyPropertyAttribute<int>("Count");

        Assert.Equal("Count", attribute.Name);
        Assert.Equal(typeof(int), attribute.Type);
    }

    /// <summary>
    /// 泛型版本的默认值使用类型参数。
    /// </summary>
    [Fact]
    public void GenericDefaultValue_UsesTypeArgument()
    {
        var attribute = new DependencyPropertyAttribute<int>("Count") { DefaultValue = 5 };

        Assert.Equal(5, attribute.DefaultValue);
    }
}
