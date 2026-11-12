using Len.Abp.Analyzer.TestSupport;
using Xunit;

namespace Len.Abp.Analyzer.Wpf.Tests;

/// <summary>
/// 锁定依赖属性生成器的生成结果。
/// 在重构生成器期间，这些断言必须逐字保持不变。
/// </summary>
public class DependencyPropertyGeneratorTests
{
    private const string Stubs = """
        namespace System.Windows
        {
            public class DependencyObject
            {
                public object GetValue(DependencyProperty dp) => null!;
                public void SetValue(DependencyProperty dp, object value) { }
            }

            public class DependencyProperty { }

            public class PropertyMetadata
            {
                public PropertyMetadata(object defaultValue, PropertyChangedCallback propertyChangedCallback, CoerceValueCallback coerceValueCallback) { }
            }

            public class FrameworkPropertyMetadata : PropertyMetadata
            {
                public FrameworkPropertyMetadata(object defaultValue, FrameworkPropertyMetadataOptions flags, PropertyChangedCallback propertyChangedCallback, CoerceValueCallback coerceValueCallback, bool isAnimationProhibited, System.Windows.Data.UpdateSourceTrigger defaultUpdateSourceTrigger)
                    : base(defaultValue, propertyChangedCallback, coerceValueCallback) { }
            }

            [System.Flags]
            public enum FrameworkPropertyMetadataOptions { None = 0, AffectsRender = 1, BindsTwoWayByDefault = 2 }

            public delegate void PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e);
            public delegate object CoerceValueCallback(DependencyObject d, object baseValue);
            public delegate void ValidateValueCallback(object value);
            public struct DependencyPropertyChangedEventArgs { public object OldValue; public object NewValue; }

            public static class DependencyProperty
            {
                public static DependencyProperty Register(string name, System.Type propertyType, System.Type ownerType, PropertyMetadata typeMetadata, ValidateValueCallback validateValueCallback) => new();
                public static DependencyPropertyKey RegisterReadOnly(string name, System.Type propertyType, System.Type ownerType, PropertyMetadata typeMetadata, ValidateValueCallback validateValueCallback) => new();
            }

            public class DependencyPropertyKey { public DependencyProperty DependencyProperty => new(); }
        }

        namespace System.Windows.Data
        {
            public enum UpdateSourceTrigger { Default = 0, PropertyChanged = 1, LostFocus = 2 }
        }

        namespace Len.Abp.Wpf
        {
            [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true)]
            public class DependencyPropertyAttribute : System.Attribute
            {
                public DependencyPropertyAttribute(string name, System.Type type) { }
                public string? Category { get; set; }
                public object? DefaultValue { get; set; }
                public string? DefaultValueExpression { get; set; }
                public string? Description { get; set; }
                public System.Windows.FrameworkPropertyMetadataOptions Flags { get; set; }
                public bool IsReadOnly { get; set; }
                public System.Windows.Data.UpdateSourceTrigger UpdateSourceTrigger { get; set; }
            }

            [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true)]
            public class DependencyPropertyAttribute<T> : DependencyPropertyAttribute
            {
                public DependencyPropertyAttribute(string name) : base(name, typeof(T)) { }
                public new T? DefaultValue { get; set; }
            }

            [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true)]
            public class AttachedDependencyPropertyAttribute : System.Attribute
            {
                public AttachedDependencyPropertyAttribute(string name, System.Type type) { }
                public string? Category { get; set; }
                public object? DefaultValue { get; set; }
                public string? DefaultValueExpression { get; set; }
                public string? Description { get; set; }
                public System.Windows.FrameworkPropertyMetadataOptions Flags { get; set; }
                public bool IsReadOnly { get; set; }
                public System.Windows.Data.UpdateSourceTrigger UpdateSourceTrigger { get; set; }
            }

            [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true)]
            public class AttachedDependencyPropertyAttribute<T> : AttachedDependencyPropertyAttribute
            {
                public AttachedDependencyPropertyAttribute(string name) : base(name, typeof(T)) { }
                public new T? DefaultValue { get; set; }
            }
        }
        """;

    /// <summary>
    /// 普通属性生成 Register 调用与 CLR 包装属性。
    /// </summary>
    [Fact]
    public void SimpleProperty_GeneratesRegisterAndClrWrapper()
    {
        var sources = GeneratorTestHost.Run(
            new DependencyPropertyGenerator(),
            Stubs
                + """

                namespace Demo
                {
                    [Len.Abp.Wpf.DependencyProperty<string>("Title")]
                    public partial class MyControl : System.Windows.DependencyObject { }
                }
                """
        );

        var generated = Assert.Single(sources);

        Assert.Contains("global::System.Windows.DependencyProperty.Register(", generated.Text);
        Assert.Contains("name: \"Title\"", generated.Text);
        Assert.Contains("public string Title", generated.Text);
        Assert.Contains("get { return (string)GetValue(TitleProperty); }", generated.Text);
        Assert.Contains("set { SetValue(TitleProperty, value); }", generated.Text);
    }

    /// <summary>
    /// 只读属性生成 RegisterReadOnly 与属性键。
    /// </summary>
    [Fact]
    public void ReadOnlyProperty_GeneratesRegisterReadOnlyAndKey()
    {
        var sources = GeneratorTestHost.Run(
            new DependencyPropertyGenerator(),
            Stubs
                + """

                namespace Demo
                {
                    [Len.Abp.Wpf.DependencyProperty<string>("Title", IsReadOnly = true)]
                    public partial class MyControl : System.Windows.DependencyObject { }
                }
                """
        );

        var generated = Assert.Single(sources);

        Assert.Contains("RegisterReadOnly(", generated.Text);
        Assert.Contains("TitlePropertyKey", generated.Text);
        Assert.Contains("protected set { SetValue(TitlePropertyKey, value); }", generated.Text);
    }

    /// <summary>
    /// 指定 Flags 时生成 FrameworkPropertyMetadata。
    /// </summary>
    [Fact]
    public void PropertyWithFlags_GeneratesFrameworkPropertyMetadata()
    {
        var sources = GeneratorTestHost.Run(
            new DependencyPropertyGenerator(),
            Stubs
                + """

                namespace Demo
                {
                    [Len.Abp.Wpf.DependencyProperty<string>("Title", Flags = System.Windows.FrameworkPropertyMetadataOptions.AffectsRender)]
                    public partial class MyControl : System.Windows.DependencyObject { }
                }
                """
        );

        var generated = Assert.Single(sources);

        Assert.Contains("global::System.Windows.FrameworkPropertyMetadata(", generated.Text);
        Assert.Contains("global::System.Windows.FrameworkPropertyMetadataOptions.AffectsRender", generated.Text);
    }

    /// <summary>
    /// 指定默认值时在元数据中使用该值。
    /// </summary>
    [Fact]
    public void PropertyWithDefaultValue_UsesItInMetadata()
    {
        var sources = GeneratorTestHost.Run(
            new DependencyPropertyGenerator(),
            Stubs
                + """

                namespace Demo
                {
                    [Len.Abp.Wpf.DependencyProperty<int>("Count", DefaultValue = 5)]
                    public partial class MyControl : System.Windows.DependencyObject { }
                }
                """
        );

        var generated = Assert.Single(sources);

        Assert.Contains("defaultValue: 5", generated.Text);
    }

    /// <summary>
    /// 属性始终生成三个分部钩子方法。
    /// </summary>
    [Fact]
    public void Property_AlwaysEmitsPartialHooks()
    {
        var sources = GeneratorTestHost.Run(
            new DependencyPropertyGenerator(),
            Stubs
                + """

                namespace Demo
                {
                    [Len.Abp.Wpf.DependencyProperty<string>("Title")]
                    public partial class MyControl : System.Windows.DependencyObject { }
                }
                """
        );

        var generated = Assert.Single(sources);

        Assert.Contains("static partial void CoerceTitle(string value, ref string result);", generated.Text);
        Assert.Contains("static partial void IsTitleValid(string value, ref bool result);", generated.Text);
        Assert.Contains("partial void OnTitleChanged(string oldValue, string newValue);", generated.Text);
    }

    /// <summary>
    /// 附加属性生成 Get/Set 方法与静态元数据。
    /// </summary>
    [Fact]
    public void AttachedProperty_GeneratesGetSetAndStaticMetadata()
    {
        var sources = GeneratorTestHost.Run(
            new AttachedDependencyPropertyGenerator(),
            Stubs
                + """

                namespace Demo
                {
                    [Len.Abp.Wpf.AttachedDependencyProperty<string>("Title")]
                    public static partial class MyAttached { }
                }
                """
        );

        var generated = Assert.Single(sources);

        Assert.Contains("public static string GetTitle(global::System.Windows.DependencyObject element)", generated.Text);
        Assert.Contains("public static void SetTitle(global::System.Windows.DependencyObject element, string value)", generated.Text);
        Assert.Contains("static partial void OnTitleChanged(global::System.Windows.DependencyObject element, string oldValue, string newValue);", generated.Text);
    }
}
