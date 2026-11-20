using Len.Abp.Analyzer.TestSupport;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Len.Abp.Analyzer.Wpf.Tests;

/// <summary>
/// 覆盖：密封类上的只读依赖属性不能生成 <c>protected</c> 成员。
/// </summary>
/// <remarks>
/// <para>
/// C# 不允许密封类声明新的保护成员（CS0628）。生成器为只读依赖属性产出
/// <c>protected internal</c> 的键字段与 <c>protected</c> 的 setter，
/// 附加版本产出 <c>protected</c> 的键字段与 <c>Set</c> 方法。
/// 在密封宿主上这些成员都会触发 CS0628 警告——仓库未开
/// <c>TreatWarningsAsErrors</c> 时只是警告，但使用者开启后构建会被阻断。
/// </para>
/// <para>
/// 修复方式与静态类的既有处理一致：密封宿主上把 <c>protected</c> 降级。
/// 实例版降为 <c>public</c>（键字段与 setter 都要降），
/// 附加版降为 <c>private</c>（<c>Set</c> 只给同类型的静态方法用）。
/// </para>
/// </remarks>
public class SealedHostAccessibilityTests
{
    /// <summary>
    /// 密封类上的只读依赖属性不产生 CS0628。
    /// </summary>
    [Fact]
    public void SealedClass_ReadOnlyProperty_NoProtectedMembers()
    {
        var (sources, compileDiagnostics, _) = GeneratorTestHost.RunAllWithWarnings(
            [new DependencyPropertyGenerator()],
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

        AssertNoProtectedMemberDiagnostics(compileDiagnostics, generated.Text);
    }

    /// <summary>
    /// 密封类上的只读附加属性不产生 CS0628。
    /// </summary>
    [Fact]
    public void SealedClass_ReadOnlyAttachedProperty_NoProtectedMembers()
    {
        var (sources, compileDiagnostics, _) = GeneratorTestHost.RunAllWithWarnings(
            [new AttachedDependencyPropertyGenerator()],
            Stubs
                + """

                namespace Demo
                {
                    [Len.Abp.Wpf.AttachedDependencyProperty<string>("Title", IsReadOnly = true)]
                    public sealed partial class MyAttached { }
                }
                """
        );

        var generated = Assert.Single(sources);

        AssertNoProtectedMemberDiagnostics(compileDiagnostics, generated.Text);
    }

    /// <summary>
    /// 非密封类上的只读依赖属性仍生成 <c>protected</c> 成员。
    /// </summary>
    /// <remarks>
    /// 对照组：降级只针对密封宿主，不能把普通类的封装性一起削掉。
    /// 只读依赖属性的 <c>protected</c> setter 是 WPF 的惯例——
    /// 派生类可以改值，外部只能读。
    /// </remarks>
    [Fact]
    public void NonSealedClass_ReadOnlyProperty_KeepsProtectedSetter()
    {
        var (sources, compileDiagnostics, _) = GeneratorTestHost.RunAllWithWarnings(
            [new DependencyPropertyGenerator()],
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

        AssertNoProtectedMemberDiagnostics(compileDiagnostics, generated.Text);
        Assert.Contains("protected set", generated.Text);
    }

    /// <summary>
    /// 非密封类上的只读附加属性仍生成 <c>protected</c> 的 <c>Set</c> 方法。
    /// </summary>
    [Fact]
    public void NonSealedClass_ReadOnlyAttachedProperty_KeepsProtectedSet()
    {
        var (sources, compileDiagnostics, _) = GeneratorTestHost.RunAllWithWarnings(
            [new AttachedDependencyPropertyGenerator()],
            Stubs
                + """

                namespace Demo
                {
                    [Len.Abp.Wpf.AttachedDependencyProperty<string>("Title", IsReadOnly = true)]
                    public partial class MyAttached { }
                }
            """
        );

        var generated = Assert.Single(sources);

        AssertNoProtectedMemberDiagnostics(compileDiagnostics, generated.Text);
        Assert.Contains("protected static void SetTitle", generated.Text);
    }

    /// <summary>
    /// 静态类上的只读附加属性仍降级为 <c>private</c>（既有行为，不能被回归）。
    /// </summary>
    [Fact]
    public void StaticClass_ReadOnlyAttachedProperty_KeepsPrivateSet()
    {
        var (sources, compileDiagnostics, _) = GeneratorTestHost.RunAllWithWarnings(
            [new AttachedDependencyPropertyGenerator()],
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

        AssertNoProtectedMemberDiagnostics(compileDiagnostics, generated.Text);
        Assert.Contains("private static void SetTitle", generated.Text);
    }

    /// <summary>
    /// 密封类上的路由事件不生成 <c>protected</c> 的 Raise 方法。
    /// </summary>
    /// <remarks>
    /// 回归：生成器原本无条件产出 <c>protected void RaiseXxxEvent(...)</c>，
    /// 密封类不能声明新的保护成员，实测 CS0628。仓库没开
    /// <c>TreatWarningsAsErrors</c> 时只是警告，使用者一开就构建失败。
    /// 修法与依赖属性的 setter 降级一致：密封宿主上把 <c>protected</c> 降为 <c>public</c>。
    /// </remarks>
    [Fact]
    public void SealedClass_RoutedEvent_RaiseMethodIsPublic()
    {
        var (sources, compileDiagnostics, _) = GeneratorTestHost.RunAllWithWarnings(
            [new RoutedEventGenerator()],
            RoutedEventStubs
                + """

                namespace Demo
                {
                    [Len.Abp.Wpf.RoutedEvent("Ping", System.Windows.RoutingStrategy.Bubble)]
                    public sealed partial class MyControl : System.Windows.UIElement { }
                }
                """
        );

        var generated = Assert.Single(sources);

        AssertNoProtectedMemberDiagnostics(compileDiagnostics, generated.Text);
        Assert.Contains("public void RaisePingEvent", generated.Text);
    }

    /// <summary>
    /// 非密封类上的路由事件仍生成 <c>protected</c> 的 Raise 方法。
    /// </summary>
    /// <remarks>
    /// 对照组：降级只针对密封宿主。路由事件的 Raise 方法按 WPF 惯例是
    /// <c>protected</c>——派生类负责在合适的时机触发，外部不应直接调用。
    /// </remarks>
    [Fact]
    public void NonSealedClass_RoutedEvent_RaiseMethodStaysProtected()
    {
        var (sources, compileDiagnostics, _) = GeneratorTestHost.RunAllWithWarnings(
            [new RoutedEventGenerator()],
            RoutedEventStubs
                + """

                namespace Demo
                {
                    [Len.Abp.Wpf.RoutedEvent("Ping", System.Windows.RoutingStrategy.Bubble)]
                    public partial class MyControl : System.Windows.UIElement { }
                }
                """
        );

        var generated = Assert.Single(sources);

        AssertNoProtectedMemberDiagnostics(compileDiagnostics, generated.Text);
        Assert.Contains("protected void RaisePingEvent", generated.Text);
    }

    /// <summary>
    /// 断言没有 CS0628 / CS1057（保护成员相关诊断）。
    /// </summary>
    private static void AssertNoProtectedMemberDiagnostics(
        IReadOnlyList<Diagnostic> compileDiagnostics,
        string generatedText
    )
    {
        var relevant = compileDiagnostics
            .Where(d => d.Id is "CS0628" or "CS1057")
            .Select(d => d.ToString())
            .ToArray();

        Assert.True(
            relevant.Length == 0,
            "生成了密封类或静态类不允许的保护成员：" + Environment.NewLine
                + string.Join(Environment.NewLine, relevant)
                + Environment.NewLine + "---- 生成代码 ----" + Environment.NewLine + generatedText
        );
    }

    /// <summary>
    /// 路由事件生成器所需的可编译桩。
    /// </summary>
    /// <remarks>
    /// 与依赖属性桩分开：路由事件只需要 <c>System.Windows.UIElement</c> 与
    /// <c>System.Windows.EventManager</c>，混在一起会让依赖属性用例多编译无关类型。
    /// </remarks>
    private const string RoutedEventStubs = """
        namespace System.Windows
        {
            public enum RoutingStrategy { Tunnel = 0, Bubble = 1, Direct = 2 }

            public delegate void RoutedEventHandler(object sender, RoutedEventArgs e);

            public class RoutedEventArgs : System.EventArgs { public RoutedEvent? RoutedEvent { get; set; } }

            public class RoutedEvent { }

            public static class EventManager
            {
                public static RoutedEvent RegisterRoutedEvent(string name, RoutingStrategy strategy, System.Type handlerType, System.Type ownerType) => new();
            }

            public class UIElement
            {
                public void AddHandler(RoutedEvent routedEvent, System.Delegate handler) { }
                public void RemoveHandler(RoutedEvent routedEvent, System.Delegate handler) { }
                public void RaiseEvent(RoutedEventArgs e) { }
            }
        }

        namespace Len.Abp.Wpf
        {
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
    /// 生成器所需的可编译桩。
    /// </summary>
    private const string Stubs = """
        namespace System.Windows
        {
            public class DependencyObject
            {
                public object? GetValue(DependencyProperty dp) => null;
                public void SetValue(DependencyProperty dp, object? value) { }
                public void SetValue(DependencyPropertyKey key, object? value) { }
            }

            public class DependencyProperty
            {
                public static DependencyProperty Register(string name, System.Type propertyType, System.Type ownerType, PropertyMetadata typeMetadata, ValidateValueCallback? validateValueCallback) => new();
                public static DependencyPropertyKey RegisterReadOnly(string name, System.Type propertyType, System.Type ownerType, PropertyMetadata typeMetadata, ValidateValueCallback? validateValueCallback) => new();
                public static DependencyProperty RegisterAttached(string name, System.Type propertyType, System.Type ownerType, PropertyMetadata defaultMetadata, ValidateValueCallback? validateValueCallback) => new();
                public static DependencyPropertyKey RegisterAttachedReadOnly(string name, System.Type propertyType, System.Type ownerType, PropertyMetadata defaultMetadata, ValidateValueCallback? validateValueCallback) => new();
            }

            public class DependencyPropertyKey { public DependencyProperty DependencyProperty => new(); }

            public delegate bool ValidateValueCallback(object value);

            public class PropertyMetadata
            {
                public PropertyMetadata(object? defaultValue, PropertyChangedCallback? propertyChangedCallback, CoerceValueCallback? coerceValueCallback) { }
            }

            public class FrameworkPropertyMetadata : PropertyMetadata
            {
                public FrameworkPropertyMetadata(object? defaultValue, FrameworkPropertyMetadataOptions flags, PropertyChangedCallback? propertyChangedCallback, CoerceValueCallback? coerceValueCallback, bool isAnimationProhibited) : base(defaultValue, propertyChangedCallback, coerceValueCallback) { }
            }

            [System.Flags]
            public enum FrameworkPropertyMetadataOptions { None = 0, AffectsRender = 1 }

            public delegate void PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e);

            public delegate object CoerceValueCallback(DependencyObject d, object baseValue);

            public struct DependencyPropertyChangedEventArgs { public object? OldValue => null; public object? NewValue => null; }
        }

        namespace System.Windows.Data
        {
            public enum UpdateSourceTrigger { Default = 0, PropertyChanged = 1, LostFocus = 2, Explicit = 3 }
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
}
