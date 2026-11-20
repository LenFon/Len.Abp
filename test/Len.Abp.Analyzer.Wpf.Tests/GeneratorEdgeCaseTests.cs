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
            public class DependencyObject
            {
                public object GetValue(DependencyProperty dp) => null!;
                public void SetValue(DependencyProperty dp, object value) { }
                public void SetValue(DependencyPropertyKey key, object value) { }
            }
            public class DependencyProperty
            {
                public static DependencyProperty Register(string name, System.Type propertyType, System.Type ownerType, PropertyMetadata typeMetadata, ValidateValueCallback validateValueCallback) => new();
                public static DependencyPropertyKey RegisterReadOnly(string name, System.Type propertyType, System.Type ownerType, PropertyMetadata typeMetadata, ValidateValueCallback validateValueCallback) => new();
                public static DependencyProperty RegisterAttached(string name, System.Type propertyType, System.Type ownerType, PropertyMetadata defaultMetadata, ValidateValueCallback? validateValueCallback) => new();
                public static DependencyPropertyKey RegisterAttachedReadOnly(string name, System.Type propertyType, System.Type ownerType, PropertyMetadata defaultMetadata, ValidateValueCallback? validateValueCallback) => new();
            }
            public class PropertyMetadata { public PropertyMetadata(object defaultValue, PropertyChangedCallback propertyChangedCallback, CoerceValueCallback coerceValueCallback) { } }
            public class FrameworkPropertyMetadata : PropertyMetadata
            {
                public FrameworkPropertyMetadata(object defaultValue, FrameworkPropertyMetadataOptions flags, PropertyChangedCallback propertyChangedCallback, CoerceValueCallback coerceValueCallback, bool isAnimationProhibited, System.Windows.Data.UpdateSourceTrigger defaultUpdateSourceTrigger)
                    : base(defaultValue, propertyChangedCallback, coerceValueCallback) { }

                public FrameworkPropertyMetadata(object defaultValue, FrameworkPropertyMetadataOptions flags, PropertyChangedCallback propertyChangedCallback, CoerceValueCallback coerceValueCallback, bool isAnimationProhibited)
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
    /// 只指定 Flags 时不能写出 <c>defaultUpdateSourceTrigger: Default</c>。
    /// </summary>
    /// <remarks>
    /// WPF 会抛「不能将 FrameworkPropertyMetadata.DefaultUpdateSourceTrigger 设置为
    /// UpdateSourceTrigger.Default；这样会创建循环定义」。该异常发生在类型初始化阶段，
    /// 编译期看不出来，只有真正运行到注册依赖属性时才暴露。
    /// </remarks>
    [Fact]
    public void FlagsWithoutUpdateSourceTrigger_OmitsDefaultTrigger()
    {
        var (sources, errors) = GeneratorTestHost.RunAndCompile(
            new DependencyPropertyGenerator(),
            Stubs
                + """

                namespace Demo
                {
                    [Len.Abp.Wpf.DependencyProperty<string>(
                        "Title",
                        Flags = System.Windows.FrameworkPropertyMetadataOptions.AffectsRender)]
                    public partial class MyControl : System.Windows.DependencyObject { }
                }
                """
        );

        var generated = Assert.Single(sources);
        Assert.Contains("new global::System.Windows.FrameworkPropertyMetadata(", generated.Text);
        Assert.DoesNotContain("defaultUpdateSourceTrigger:", generated.Text);
        Assert.DoesNotContain("isAnimationProhibited: false,", generated.Text);
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
    /// <remarks>
    /// 不覆盖 <c>decimal</c>：它不是合法的特性参数类型，编译器不允许
    /// <c>DefaultValue = 0.5m</c>，对应分支在真实代码里不可达。
    /// </remarks>
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

    /// <summary>
    /// 引用类型属性的强制值回调不产生可空警告。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 回归：生成器原本无条件产出 <c>{Type} result = default;</c>。生成文件开了
    /// <c>#nullable enable</c>，对引用类型会报 CS8600（把 null 转成不可空类型）
    /// 与 CS8601（可能的 null 赋值）。仓库没开 <c>TreatWarningsAsErrors</c> 时只是警告，
    /// 使用者一开就构建失败。
    /// </para>
    /// <para>
    /// 修法是引用类型用 <c>default!</c>。不能改成 <c>{Type}? result</c>：
    /// 分部方法的参数是 <c>ref {Type}</c>，可空标注不匹配会报 CS8620。
    /// </para>
    /// </remarks>
    [Fact]
    public void ReferenceTypeCoerceHook_DoesNotProduceNullableWarnings()
    {
        var (sources, compileDiagnostics, _) = GeneratorTestHost.RunAllWithWarnings(
            [new DependencyPropertyGenerator()],
            Stubs
                + """

                namespace Demo
                {
                    [Len.Abp.Wpf.DependencyProperty<System.Collections.Generic.List<string>>("Tags")]
                    public partial class MyControl : System.Windows.DependencyObject
                    {
                        static partial void CoerceTags(System.Collections.Generic.List<string> value, ref System.Collections.Generic.List<string> result) { }
                    }
                }
                """
        );

        var generated = Assert.Single(sources);

        var nullableWarnings = compileDiagnostics
            .Where(d => d.Id is "CS8600" or "CS8601" or "CS8620")
            .Select(d => d.ToString())
            .ToArray();

        Assert.True(
            nullableWarnings.Length == 0,
            "引用类型强制值回调产生了可空警告：" + Environment.NewLine
                + string.Join(Environment.NewLine, nullableWarnings)
                + Environment.NewLine + "---- 生成代码 ----" + Environment.NewLine + generated.Text
        );

        Assert.Contains("result = default!;", generated.Text);
    }

    /// <summary>
    /// 值类型属性的强制值回调仍用普通 <c>default</c>。
    /// </summary>
    /// <remarks>
    /// 对照组：<c>default!</c> 只对引用类型有意义。值类型上加 <c>!</c> 虽然能编译，
    /// 但会让生成代码出现无意义的抑制符，掩盖真正的可空问题。
    /// </remarks>
    [Fact]
    public void ValueTypeCoerceHook_UsesPlainDefault()
    {
        var (sources, _, _) = GeneratorTestHost.RunAllWithWarnings(
            [new DependencyPropertyGenerator()],
            Stubs
                + """

                namespace Demo
                {
                    [Len.Abp.Wpf.DependencyProperty<int>("Count")]
                    public partial class MyControl : System.Windows.DependencyObject
                    {
                        static partial void CoerceCount(int value, ref int result) { }
                    }
                }
                """
        );

        var generated = Assert.Single(sources);

        Assert.Contains("result = default;", generated.Text);
        Assert.DoesNotContain("result = default!;", generated.Text);
    }

    /// <summary>
    /// 静态类宿主生成 <c>static partial class</c>，修饰符顺序必须是 <c>static partial</c>。
    /// </summary>
    /// <remarks>
    /// 写成 <c>partial static class</c> 会报 CS0267；漏掉 <c>static</c> 则与用户声明的静态类冲突。
    /// 注意：生成的注册调用是 <c>DependencyProperty.Register</c>，它在运行时要求
    /// <c>ownerType</c> 派生自 <c>DependencyObject</c>，因此静态类只适合承载附加路由事件，
    /// 附加依赖属性应放在派生自 <c>DependencyObject</c> 的类上。
    /// </remarks>
    [Fact]
    public void StaticHost_GeneratesStaticPartialDeclaration()
    {
        var (sources, errors) = GeneratorTestHost.RunAndCompile(
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
        Assert.Contains("static partial class MyAttached", generated.Text);
        Assert.DoesNotContain("partial static class", generated.Text);
        Assert.Empty(errors);
    }

    /// <summary>
    /// 静态类上的只读附加属性不能生成 <c>protected</c> 成员（CS1057）。
    /// </summary>
    /// <remarks>
    /// 只验证生成代码可编译。注册走 <c>RegisterAttached</c>，静态类宿主在运行期也合法。
    /// </remarks>
    [Fact]
    public void StaticHost_ReadOnlyAttachedProperty_UsesAccessibleModifier()
    {
        var (sources, errors) = GeneratorTestHost.RunAndCompile(
            new AttachedDependencyPropertyGenerator(),
            Stubs
                + """

                namespace Demo
                {
                    [Len.Abp.Wpf.AttachedDependencyProperty<string>("Title", IsReadOnly = true)]
                    public static partial class MyAttached { }
                }
                """
        );

        var generated = Assert.Single(sources);
        Assert.Contains("private static void SetTitle(", generated.Text);
        Assert.DoesNotContain("protected static void SetTitle(", generated.Text);
        Assert.Empty(errors);
    }

    /// <summary>
    /// 附加依赖属性用 <c>RegisterAttached</c> 注册，静态类宿主在运行期也合法。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 回归：原实现用 <c>DependencyProperty.Register</c>，它要求 <c>ownerType</c>
    /// 派生自 <c>DependencyObject</c>，静态类宿主会在类型初始化时抛
    /// 「类型必须派生自 DependencyObject」。
    /// </para>
    /// <para>
    /// <c>RegisterAttached</c> 没有该限制，且 XAML 才能把属性识别为附加属性。
    /// 注意它的元数据参数名是 <c>defaultMetadata</c>，与 <c>Register</c> 的
    /// <c>typeMetadata</c> 不同，写错会报 CS1739。
    /// </para>
    /// </remarks>
    [Fact]
    public void AttachedProperty_UsesRegisterAttached()
    {
        var (sources, errors) = GeneratorTestHost.RunAndCompile(
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

        Assert.Empty(errors);
        Assert.Contains("DependencyProperty.RegisterAttached(", generated.Text);
        Assert.Contains("defaultMetadata:", generated.Text);
        Assert.DoesNotContain("DependencyProperty.Register(", generated.Text);
    }

    /// <summary>
    /// 只读附加属性用 <c>RegisterAttachedReadOnly</c> 注册。
    /// </summary>
    [Fact]
    public void ReadOnlyAttachedProperty_UsesRegisterAttachedReadOnly()
    {
        var (sources, errors) = GeneratorTestHost.RunAndCompile(
            new AttachedDependencyPropertyGenerator(),
            Stubs
                + """

                namespace Demo
                {
                    [Len.Abp.Wpf.AttachedDependencyProperty<string>("Title", IsReadOnly = true)]
                    public static partial class MyAttached { }
                }
                """
        );

        var generated = Assert.Single(sources);

        Assert.Empty(errors);
        Assert.Contains("DependencyProperty.RegisterAttachedReadOnly(", generated.Text);
        Assert.Contains("defaultMetadata:", generated.Text);
    }

    /// <summary>
    /// 非附加依赖属性仍用 <c>Register</c>，元数据参数名是 <c>typeMetadata</c>。
    /// </summary>
    /// <remarks>
    /// 对照组：两组 API 的参数名不同，改附加版本时不能把实例版本一起改错。
    /// </remarks>
    [Fact]
    public void InstanceProperty_StillUsesRegisterWithTypeMetadata()
    {
        var (sources, errors) = GeneratorTestHost.RunAndCompile(
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

        Assert.Empty(errors);
        Assert.Contains("DependencyProperty.Register(", generated.Text);
        Assert.Contains("typeMetadata:", generated.Text);
        Assert.DoesNotContain("RegisterAttached", generated.Text);
    }

    /// <summary>
    /// 静态外层类型同样保留 <c>static</c>，嵌套类型逐层展开时不能丢失。
    /// </summary>
    [Fact]
    public void StaticContainingType_PreservesStaticModifier()
    {
        var (sources, errors) = GeneratorTestHost.RunAndCompile(
            new AttachedDependencyPropertyGenerator(),
            Stubs
                + """

                namespace Demo
                {
                    public static partial class Outer
                    {
                        [Len.Abp.Wpf.AttachedDependencyProperty<string>("Title")]
                        public static partial class Inner { }
                    }
                }
                """
        );

        var generated = Assert.Single(sources);
        Assert.Contains("static partial class Outer", generated.Text);
        Assert.Empty(errors);
    }

    /// <summary>
    /// 密封类上的只读依赖属性不产生 CS0273。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 回归：原实现把密封宿主的 setter 降级为 <c>public</c>，而属性本身已是 <c>public</c>，
    /// 访问器可访问性必须比属性更严格，编译器报 CS0273。
    /// </para>
    /// <para>
    /// 正确做法是去掉访问器修饰符（等价于继承属性本身的可访问性），
    /// 而不是降级为 <c>public</c>。
    /// </para>
    /// </remarks>
    [Fact]
    public void SealedClass_ReadOnlyProperty_SetterHasNoModifier()
    {
        var (sources, errors) = GeneratorTestHost.RunAndCompile(
            new DependencyPropertyGenerator(),
            Stubs
                + """

                namespace Demo
                {
                    [Len.Abp.Wpf.DependencyProperty<string>("Title", IsReadOnly = true)]
                    public sealed partial class MyControl : System.Windows.DependencyObject { }
                }
                """
        );

        var generated = Assert.Single(sources);

        Assert.Empty(errors);
        Assert.Contains("set { SetValue(TitlePropertyKey, value); }", generated.Text);
        Assert.DoesNotContain("public set", generated.Text);
    }

    /// <summary>
    /// 非密封类上的只读依赖属性仍生成 <c>protected</c> setter。
    /// </summary>
    /// <remarks>
    /// 对照组：降级只针对密封宿主，不能把普通类的封装性一起削掉。
    /// </remarks>
    [Fact]
    public void NonSealedClass_ReadOnlyProperty_SetterStaysProtected()
    {
        var (sources, errors) = GeneratorTestHost.RunAndCompile(
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

        Assert.Empty(errors);
        Assert.Contains("protected set { SetValue(TitlePropertyKey, value); }", generated.Text);
    }

    /// <summary>
    /// 显式写 <c>UpdateSourceTrigger.Default</c> 时不写出该参数。
    /// </summary>
    /// <remarks>
    /// 回归：WPF 的 <c>FrameworkPropertyMetadata</c> 构造函数拒绝 <c>Default</c>——
    /// 「不能将 DefaultUpdateSourceTrigger 设置为 UpdateSourceTrigger.Default；这样会创建循环定义」。
    /// 异常发生在静态字段初始化阶段，表现为 <c>TypeInitializationException</c>。
    /// </remarks>
    [Fact]
    public void ExplicitDefaultUpdateSourceTrigger_IsOmitted()
    {
        var (sources, errors) = GeneratorTestHost.RunAndCompile(
            new DependencyPropertyGenerator(),
            Stubs
                + """

                namespace Demo
                {
                    [Len.Abp.Wpf.DependencyProperty<string>("Title", UpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.Default)]
                    public partial class MyControl : System.Windows.DependencyObject { }
                }
                """
        );

        var generated = Assert.Single(sources);

        Assert.Empty(errors);
        Assert.DoesNotContain("defaultUpdateSourceTrigger:", generated.Text);
    }

    /// <summary>
    /// 非 Default 的 <c>UpdateSourceTrigger</c> 仍然写出。
    /// </summary>
    /// <remarks>
    /// 对照组：省略只针对 <c>Default</c>，其余取值必须保留，否则设置会被静默丢弃。
    /// </remarks>
    [Fact]
    public void NonDefaultUpdateSourceTrigger_IsEmitted()
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

        Assert.Empty(errors);
        Assert.Contains(
            "defaultUpdateSourceTrigger: global::System.Windows.Data.UpdateSourceTrigger.PropertyChanged",
            generated.Text
        );
    }

    /// <summary>
    /// 未定义的枚举值生成强制转换，而不是 <c>global::8</c> 这种非法代码。
    /// </summary>
    /// <remarks>
    /// <c>TypedConstant.ToCSharpString()</c> 对未定义值给出纯数字，
    /// 直接拼 <c>global::</c> 会生成 <c>global::8</c>，编译报 CS1001。
    /// </remarks>
    [Fact]
    public void UndefinedFlagsValue_GeneratesCast()
    {
        var (sources, errors) = GeneratorTestHost.RunAndCompile(
            new DependencyPropertyGenerator(),
            Stubs
                + """

                namespace Demo
                {
                    [Len.Abp.Wpf.DependencyProperty<string>("Title", Flags = (System.Windows.FrameworkPropertyMetadataOptions)8)]
                    public partial class MyControl : System.Windows.DependencyObject { }
                }
                """
        );

        var generated = Assert.Single(sources);

        Assert.Empty(errors);
        Assert.Contains("flags: (global::System.Windows.FrameworkPropertyMetadataOptions)(8)", generated.Text);
    }

    /// <summary>
    /// 未定义的 <c>UpdateSourceTrigger</c> 值生成强制转换。
    /// </summary>
    [Fact]
    public void UndefinedUpdateSourceTrigger_GeneratesCast()
    {
        var (sources, errors) = GeneratorTestHost.RunAndCompile(
            new DependencyPropertyGenerator(),
            Stubs
                + """

                namespace Demo
                {
                    [Len.Abp.Wpf.DependencyProperty<string>("Title", UpdateSourceTrigger = (System.Windows.Data.UpdateSourceTrigger)9)]
                    public partial class MyControl : System.Windows.DependencyObject { }
                }
                """
        );

        var generated = Assert.Single(sources);

        Assert.Empty(errors);
        Assert.Contains(
            "defaultUpdateSourceTrigger: (global::System.Windows.Data.UpdateSourceTrigger)(9)",
            generated.Text
        );
    }

    /// <summary>
    /// <c>DefaultValueExpression = "new()"</c> 被包成强制转换，目标类型由属性类型决定。
    /// </summary>
    /// <remarks>
    /// 回归：特性命名参数的类型是 <see cref="object"/>，<c>new()</c> 会被推断成
    /// <see cref="object"/> 而不是属性类型，WPF 注册时抛「默认值类型与属性类型不匹配」。
    /// 包上转换后目标类型由属性类型决定，<c>new()</c> 与集合表达式都能正确推断。
    /// </remarks>
    [Fact]
    public void TargetTypedDefaultValueExpression_IsCastToPropertyType()
    {
        var (sources, errors) = GeneratorTestHost.RunAndCompile(
            new DependencyPropertyGenerator(),
            Stubs
                + """

                namespace Demo
                {
                    [Len.Abp.Wpf.DependencyProperty<System.Collections.Generic.List<int>>("Items", DefaultValueExpression = "new()")]
                    public partial class MyControl : System.Windows.DependencyObject { }
                }
                """
        );

        var generated = Assert.Single(sources);

        Assert.Empty(errors);
        Assert.Contains(
            "defaultValue: (global::System.Collections.Generic.List<int>)(new())",
            generated.Text
        );
    }
}
