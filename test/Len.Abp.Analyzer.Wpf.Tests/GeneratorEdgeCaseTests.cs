using Len.Abp.Analyzer.TestSupport;
using Xunit;

namespace Len.Abp.Analyzer.Wpf.Tests;

/// <summary>
/// 覆盖生成器在边界类型声明上的行为：全局命名空间、记录、嵌套类型与泛型类型。
/// 这些用例都会把生成结果编译一次，避免“文本看起来对、实际无法编译”的缺陷。
/// </summary>
public class GeneratorEdgeCaseTests
{
    private const string Stubs = """
        namespace System.Windows
        {
            public class DependencyObject { public object GetValue(DependencyProperty dp) => null!; public void SetValue(DependencyProperty dp, object value) { } }
            public class DependencyProperty
            {
                public static DependencyProperty Register(string name, System.Type propertyType, System.Type ownerType, PropertyMetadata typeMetadata, ValidateValueCallback validateValueCallback) => new();
                public static DependencyPropertyKey RegisterReadOnly(string name, System.Type propertyType, System.Type ownerType, PropertyMetadata typeMetadata, ValidateValueCallback validateValueCallback) => new();
            }
            public class PropertyMetadata { public PropertyMetadata(object defaultValue, PropertyChangedCallback propertyChangedCallback, CoerceValueCallback coerceValueCallback) { } }
            public class FrameworkPropertyMetadata : PropertyMetadata
            {
                public FrameworkPropertyMetadata(object defaultValue, FrameworkPropertyMetadataOptions flags, PropertyChangedCallback propertyChangedCallback, CoerceValueCallback coerceValueCallback, bool isAnimationProhibited, System.Windows.Data.UpdateSourceTrigger defaultUpdateSourceTrigger)
                    : base(defaultValue, propertyChangedCallback, coerceValueCallback) { }
            }
            [System.Flags] public enum FrameworkPropertyMetadataOptions { None = 0, AffectsRender = 1, BindsTwoWayByDefault = 2 }
            public delegate void PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e);
            public delegate object CoerceValueCallback(DependencyObject d, object baseValue);
            public delegate bool ValidateValueCallback(object value);
            public struct DependencyPropertyChangedEventArgs { public object OldValue; public object NewValue; }
            public class DependencyPropertyKey { public DependencyProperty DependencyProperty => new(); }

            public enum RoutingStrategy { Tunnel = 0, Bubble = 1, Direct = 2 }
            public delegate void RoutedEventHandler(object sender, RoutedEventArgs e);
            public class RoutedEventArgs { public RoutedEvent? RoutedEvent { get; set; } }
            public class RoutedEvent { }
            public class UIElement : DependencyObject
            {
                public void AddHandler(RoutedEvent routedEvent, System.Delegate handler) { }
                public void RemoveHandler(RoutedEvent routedEvent, System.Delegate handler) { }
                public void RaiseEvent(RoutedEventArgs e) { }
            }
            public static class EventManager
            {
                public static RoutedEvent RegisterRoutedEvent(string name, RoutingStrategy routingStrategy, System.Type handlerType, System.Type ownerType) => new();
            }
        }

        namespace System.Windows.Data { public enum UpdateSourceTrigger { Default = 0, PropertyChanged = 1 } }

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
            public class RoutedEventAttribute : System.Attribute
            {
                public RoutedEventAttribute(string name, System.Windows.RoutingStrategy strategy, System.Type? handlerType = null) { }
                public string? Category { get; set; }
                public string? Description { get; set; }
            }

            [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true)]
            public class RoutedEventAttribute<T> : RoutedEventAttribute
            {
                public RoutedEventAttribute(string name, System.Windows.RoutingStrategy strategy) : base(name, strategy, typeof(T)) { }
            }
        }
        """;

    /// <summary>
    /// 全局命名空间中的类型不再让生成器抛异常。
    /// </summary>
    [Fact]
    public void GlobalNamespace_GeneratesCompilableCode()
    {
        var (sources, errors) = GeneratorTestHost.RunAndCompile(
            new RoutedEventGenerator(),
            Stubs
                + """

                [Len.Abp.Wpf.RoutedEvent("Clicked", System.Windows.RoutingStrategy.Bubble)]
                public partial class GlobalControl : System.Windows.UIElement { }
                """
        );

        var generated = Assert.Single(sources);
        Assert.DoesNotContain("namespace", generated.Text);
        Assert.Empty(errors);
    }

    /// <summary>
    /// 记录声明不再因强制类型转换而让生成管线崩溃，且生成 <c>partial record</c> 而非 <c>partial class</c>。
    /// </summary>
    [Fact]
    public void RecordDeclaration_GeneratesCompilableCode()
    {
        // 记录不能继承 WPF 控件，因此这里由记录自身提供生成代码所需的成员。
        var (sources, errors) = GeneratorTestHost.RunAndCompile(
            new RoutedEventGenerator(),
            Stubs
                + """

                namespace Demo
                {
                    [Len.Abp.Wpf.RoutedEvent("Clicked", System.Windows.RoutingStrategy.Bubble)]
                    public partial record MyRecord
                    {
                        public void AddHandler(System.Windows.RoutedEvent routedEvent, System.Delegate handler) { }
                        public void RemoveHandler(System.Windows.RoutedEvent routedEvent, System.Delegate handler) { }
                        public void RaiseEvent(System.Windows.RoutedEventArgs e) { }
                    }
                }
                """
        );

        var generated = Assert.Single(sources);
        Assert.Contains("partial record MyRecord", generated.Text);
        Assert.DoesNotContain("partial class MyRecord", generated.Text);
        Assert.Empty(errors);
    }

    /// <summary>
    /// 嵌套类型生成逐层展开的 partial 声明，而不是把外层类型名并入命名空间。
    /// </summary>
    [Fact]
    public void NestedType_GeneratesCompilableCode()
    {
        var (sources, errors) = GeneratorTestHost.RunAndCompile(
            new RoutedEventGenerator(),
            Stubs
                + """

                namespace Demo
                {
                    public partial class Outer
                    {
                        [Len.Abp.Wpf.RoutedEvent("Clicked", System.Windows.RoutingStrategy.Bubble)]
                        public partial class Inner : System.Windows.UIElement { }
                    }
                }
                """
        );

        var generated = Assert.Single(sources);
        Assert.Contains("namespace Demo;", generated.Text);
        Assert.Contains("partial class Outer", generated.Text);
        Assert.Contains("partial class Inner", generated.Text);
        Assert.Empty(errors);
    }

    /// <summary>
    /// 外层类型是记录时，生成的 partial 声明也必须使用 <c>record</c> 关键字。
    /// </summary>
    [Fact]
    public void NestedInRecord_PreservesRecordKeyword()
    {
        var (sources, errors) = GeneratorTestHost.RunAndCompile(
            new RoutedEventGenerator(),
            Stubs
                + """

                namespace Demo
                {
                    public partial record Outer
                    {
                        [Len.Abp.Wpf.RoutedEvent("Clicked", System.Windows.RoutingStrategy.Bubble)]
                        public partial class Inner : System.Windows.UIElement { }
                    }
                }
                """
        );

        var generated = Assert.Single(sources);
        Assert.Contains("partial record Outer", generated.Text);
        Assert.DoesNotContain("partial class Outer", generated.Text);
        Assert.Empty(errors);
    }

    /// <summary>
    /// 泛型类型的 hint name 不含尖括号，生成器不再静默跳过。
    /// </summary>
    [Fact]
    public void GenericType_ProducesValidHintName()
    {
        var (sources, errors) = GeneratorTestHost.RunAndCompile(
            new RoutedEventGenerator(),
            Stubs
                + """

                namespace Demo
                {
                    [Len.Abp.Wpf.RoutedEvent("Clicked", System.Windows.RoutingStrategy.Bubble)]
                    public partial class MyControl<T> : System.Windows.UIElement { }
                }
                """
        );

        var generated = Assert.Single(sources);
        Assert.DoesNotContain("<", generated.HintName);
        Assert.Empty(errors);
    }

    /// <summary>
    /// 显式写出 Flags = None 时回退到普通 PropertyMetadata，而不是生成空 flags 参数。
    /// </summary>
    [Fact]
    public void ExplicitFlagsNone_GeneratesCompilableCode()
    {
        var (sources, errors) = GeneratorTestHost.RunAndCompile(
            new DependencyPropertyGenerator(),
            Stubs
                + """

                namespace Demo
                {
                    [Len.Abp.Wpf.DependencyProperty<string>("Title", Flags = System.Windows.FrameworkPropertyMetadataOptions.None)]
                    public partial class MyControl : System.Windows.DependencyObject { }
                }
                """
        );

        var generated = Assert.Single(sources);
        Assert.Contains("new global::System.Windows.PropertyMetadata(", generated.Text);
        Assert.Empty(errors);
    }

    /// <summary>
    /// 单独指定 UpdateSourceTrigger 时也必须走 FrameworkPropertyMetadata，否则设置会被丢弃。
    /// </summary>
    [Fact]
    public void UpdateSourceTriggerWithoutFlags_UsesFrameworkMetadata()
    {
        var (sources, errors) = GeneratorTestHost.RunAndCompile(
            new DependencyPropertyGenerator(),
            Stubs
                + """

                namespace Demo
                {
                    [Len.Abp.Wpf.DependencyProperty<string>("Title", UpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.PropertyChanged)]
                    public partial class MyControl : System.Windows.DependencyObject { }
                }
                """
        );

        var generated = Assert.Single(sources);
        Assert.Contains("new global::System.Windows.FrameworkPropertyMetadata(", generated.Text);
        Assert.Contains("defaultUpdateSourceTrigger: global::System.Windows.Data.UpdateSourceTrigger.PropertyChanged", generated.Text);
        Assert.Empty(errors);
    }

    /// <summary>
    /// 分类与描述中的引号和尖括号会被转义，生成结果仍可编译。
    /// </summary>
    [Fact]
    public void CategoryWithQuotes_IsEscaped()
    {
        var (sources, errors) = GeneratorTestHost.RunAndCompile(
            new DependencyPropertyGenerator(),
            Stubs
                + """

                namespace Demo
                {
                    [Len.Abp.Wpf.DependencyProperty<string>("Title", Category = "a\"b", Description = "<see cref=\"x\"/>")]
                    public partial class MyControl : System.Windows.DependencyObject { }
                }
                """
        );

        var generated = Assert.Single(sources);
        Assert.Contains("Category(\"a\\\"b\")", generated.Text);
        Assert.Empty(errors);
    }

    /// <summary>
    /// 枚举默认值必须保留枚举类型，不能退化成数字。
    /// 生成 <c>defaultValue: 1</c> 时 WPF 注册依赖属性会抛“默认值类型与属性类型不匹配”。
    /// </summary>
    [Fact]
    public void EnumDefaultValue_PreservesEnumType()
    {
        var (sources, errors) = GeneratorTestHost.RunAndCompile(
            new DependencyPropertyGenerator(),
            Stubs
                + """

                namespace Demo
                {
                    public enum Mode { A = 0, B = 1 }

                    [Len.Abp.Wpf.DependencyProperty<Mode>("Current", DefaultValue = Mode.B)]
                    public partial class MyControl : System.Windows.DependencyObject { }
                }
                """
        );

        var generated = Assert.Single(sources);
        Assert.Contains("global::Demo.Mode", generated.Text);
        Assert.DoesNotContain("defaultValue: 1,", generated.Text);
        Assert.Empty(errors);
    }

    /// <summary>
    /// long/ulong 默认值需要显式后缀，否则字面量会被推断为 int。
    /// </summary>
    [Theory]
    [InlineData("long", "5L", "5L")]
    [InlineData("ulong", "5ul", "5UL")]
    public void IntegerSuffixDefaultValue_KeepsType(string type, string literal, string expected)
    {
        var (sources, errors) = GeneratorTestHost.RunAndCompile(
            new DependencyPropertyGenerator(),
            Stubs
                + $$"""

                namespace Demo
                {
                    [Len.Abp.Wpf.DependencyProperty<{{type}}>("Count", DefaultValue = {{literal}})]
                    public partial class MyControl : System.Windows.DependencyObject { }
                }
                """
        );

        var generated = Assert.Single(sources);
        Assert.Contains($"defaultValue: {expected},", generated.Text);
        Assert.Empty(errors);
    }

    /// <summary>
    /// 窄整型与 float 默认值需要显式转换，保证元数据中的值类型与属性类型一致。
    /// </summary>
    [Theory]
    [InlineData("float", "0.5f", "(float)(0.5)")]
    [InlineData("byte", "(byte)5", "(byte)(5)")]
    [InlineData("short", "(short)5", "(short)(5)")]
    public void NarrowNumericDefaultValue_UsesExplicitCast(string type, string literal, string expected)
    {
        var (sources, errors) = GeneratorTestHost.RunAndCompile(
            new DependencyPropertyGenerator(),
            Stubs
                + $$"""

                namespace Demo
                {
                    [Len.Abp.Wpf.DependencyProperty<{{type}}>("Value", DefaultValue = {{literal}})]
                    public partial class MyControl : System.Windows.DependencyObject { }
                }
                """
        );

        var generated = Assert.Single(sources);
        Assert.Contains($"defaultValue: {expected},", generated.Text);
        Assert.Empty(errors);
    }

    /// <summary>
    /// 属性名互为前缀时，分部钩子必须按精确方法名匹配。
    /// 用 <c>Contains</c> 判断会让 <c>CoerceTitle</c> 误命中 <c>CoerceTitle2</c>，生成对不存在方法的调用。
    /// </summary>
    [Fact]
    public void HookDetection_DoesNotMatchLongerMethodName()
    {
        var (sources, _) = GeneratorTestHost.RunAndCompile(
            new DependencyPropertyGenerator(),
            Stubs
                + """

                namespace Demo
                {
                    [Len.Abp.Wpf.DependencyProperty<string>("Title")]
                    public partial class MyControl : System.Windows.DependencyObject
                    {
                        static partial void CoerceTitle2(string value, ref string result) { }
                    }
                }
                """
        );

        var generated = Assert.Single(sources);

        // 只有 CoerceTitle2 存在，CoerceTitle 的强制值回调应为 null。
        Assert.Contains("coerceValueCallback: null", generated.Text);

        // 生成器会为 Title 声明 CoerceTitle 分部方法，但源码里没有实现；
        // 未实现的分部方法本身不产生错误，因此这里只断言没有生成对 CoerceTitle 的调用。
        Assert.DoesNotContain("CoerceTitle((global::System.String)value", generated.Text);
    }

    /// <summary>
    /// 精确匹配时，同名钩子仍会被正确识别。
    /// </summary>
    [Fact]
    public void HookDetection_MatchesExactMethodName()
    {
        var (sources, errors) = GeneratorTestHost.RunAndCompile(
            new DependencyPropertyGenerator(),
            Stubs
                + """

                namespace Demo
                {
                    [Len.Abp.Wpf.DependencyProperty<string>("Title")]
                    public partial class MyControl : System.Windows.DependencyObject
                    {
                        static partial void CoerceTitle(string value, ref string result) { }
                    }
                }
                """
        );

        var generated = Assert.Single(sources);
        Assert.Contains("CoerceTitle((string)value, ref result)", generated.Text);
        Assert.Empty(errors);
    }
}
