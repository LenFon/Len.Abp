using Len.Abp.Wpf.Extensions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Xunit;

namespace Len.Abp.Wpf.Tests;

/// <summary>
/// 覆盖 <see cref="DependencyObjectExtensions"/> 的绑定检测与父级查找。
/// </summary>
public class DependencyObjectExtensionsTests
{
    /// <summary>
    /// 未设置绑定时返回 false。
    /// </summary>
    [Fact]
    public void HasBinding_WithoutBinding_ReturnsFalse()
    {
        Sta.Run(() =>
        {
            var control = new TextBox();

            Assert.False(control.HasBinding(TextBox.TextProperty));
        });
    }

    /// <summary>
    /// 设置绑定后返回 true。
    /// </summary>
    [Fact]
    public void HasBinding_WithBinding_ReturnsTrue()
    {
        Sta.Run(() =>
        {
            var control = new TextBox();
            BindingOperations.SetBinding(control, TextBox.TextProperty, new Binding("Name"));

            Assert.True(control.HasBinding(TextBox.TextProperty));
        });
    }

    /// <summary>
    /// 多值绑定同样算作已设置绑定。
    /// </summary>
    [Fact]
    public void HasBinding_WithMultiBinding_ReturnsTrue()
    {
        Sta.Run(() =>
        {
            var control = new TextBox();
            var multiBinding = new MultiBinding { Converter = new ConcatConverter() };
            multiBinding.Bindings.Add(new Binding("A"));
            multiBinding.Bindings.Add(new Binding("B"));
            BindingOperations.SetBinding(control, TextBox.TextProperty, multiBinding);

            Assert.True(control.HasBinding(TextBox.TextProperty));
        });
    }

    /// <summary>
    /// 沿逻辑树向上找到指定类型的父级。
    /// </summary>
    [Fact]
    public void GetParent_FindsAncestorOfType()
    {
        Sta.Run(() =>
        {
            var outer = new ContentControl();
            var inner = new TextBox();
            outer.Content = inner;

            Assert.Same(outer, inner.GetParent<ContentControl>());
        });
    }

    /// <summary>
    /// 没有父级时返回 null。
    /// </summary>
    [Fact]
    public void GetParent_WithoutParent_ReturnsNull()
    {
        Sta.Run(() =>
        {
            var control = new TextBox();

            Assert.Null(control.GetParent<ContentControl>());
        });
    }

    /// <summary>
    /// 非 Visual 的 <c>ContentElement</c> 没有视觉父级时返回 null，而不是抛异常。
    /// </summary>
    [Fact]
    public void GetParent_OnContentElement_ReturnsNull()
    {
        Sta.Run(() =>
        {
            var run = new System.Windows.Documents.Run("text");

            Assert.Null(run.GetParent<ContentControl>());
        });
    }

    /// <summary>
    /// 父级类型不匹配时继续向上查找，直到找到或耗尽。
    /// </summary>
    [Fact]
    public void GetParent_SkipsNonMatchingAncestors()
    {
        Sta.Run(() =>
        {
            var outer = new ContentControl();
            var middle = new Border();
            var inner = new TextBox();
            outer.Content = middle;
            middle.Child = inner;

            Assert.Same(outer, inner.GetParent<ContentControl>());
        });
    }

    private sealed class ConcatConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture) =>
            string.Join("|", values);

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
