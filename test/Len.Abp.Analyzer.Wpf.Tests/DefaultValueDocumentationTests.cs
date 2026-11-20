using Len.Abp.Analyzer.TestSupport;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Len.Abp.Analyzer.Wpf.Tests;

/// <summary>
/// 覆盖：默认值表达式必须转义后才能进 XML 文档注释。
/// </summary>
/// <remarks>
/// <para>
/// 回归：<c>DefaultValueExpression</c> 的源码原文被直接拼进 <c>&lt;summary&gt;</c>，
/// 使用者写 <c>new List&lt;int&gt;()</c> 这类含泛型尖括号的表达式时，
/// 生成的注释不是合法 XML，编译器报 CS1570。
/// </para>
/// <para>
/// 关键点一：CS1570 只在 <see cref="DocumentationMode.Diagnose"/> 下产生，
/// 默认的 <see cref="DocumentationMode.None"/> 根本不解析注释，测试会假绿。
/// </para>
/// <para>
/// 关键点二：CS1570 是**警告**，<see cref="GeneratorTestHost.RunAll"/> 只返回错误级诊断，
/// 用它断言同样假绿（集合永远为空）。必须走 <see cref="GeneratorTestHost.RunAllWithWarnings"/>。
/// </para>
/// <para>
/// 另一半同样重要：<c>DefaultValue</c>（元数据用的表达式）**不能**转义，
/// 那是真实代码，转义后 <c>new List&lt;int&gt;()</c> 会变成非法 C#。
/// 因此每个用例都同时断言元数据里保留原文。
/// </para>
/// </remarks>
public class DefaultValueDocumentationTests
{
    /// <summary>
    /// 含泛型尖括号的 <c>DefaultValueExpression</c> 生成合法 XML 文档。
    /// </summary>
    [Fact]
    public void GenericDefaultValueExpression_ProducesValidXmlDocumentation()
    {
        var (sources, compileDiagnostics) = Run(
            """
            namespace Demo
            {
                [Len.Abp.Wpf.DependencyProperty<object>("Items", DefaultValueExpression = "new System.Collections.Generic.List<int>()")]
                public partial class MyControl : System.Windows.DependencyObject { }
            }
            """
        );

        AssertValidDocumentation(compileDiagnostics);

        var generated = Assert.Single(sources);
        Assert.Contains("/// 默认值：new System.Collections.Generic.List&lt;int&gt;()", generated.Text);

        // 元数据不能转义：那是真实代码。表达式被包成与属性类型一致的强制转换，
        // 使 new()、集合表达式这类依赖目标类型的写法能正确推断。
        Assert.Contains("defaultValue: (object)(new System.Collections.Generic.List<int>())", generated.Text);
    }

    /// <summary>
    /// 附加属性版本同样转义。
    /// </summary>
    /// <remarks>
    /// 附加属性的文档由 <c>AttachedDependencyPropertyGenerator.GenerateDocumentation</c> 生成，
    /// 与普通属性是两条独立的代码路径，只修一条会漏。
    /// </remarks>
    [Fact]
    public void AttachedGenericDefaultValueExpression_ProducesValidXmlDocumentation()
    {
        var (sources, compileDiagnostics) = Run(
            """
            namespace Demo
            {
                [Len.Abp.Wpf.AttachedDependencyProperty<object>("Items", DefaultValueExpression = "new System.Collections.Generic.List<int>()")]
                public partial class MyControl : System.Windows.DependencyObject { }
            }
            """,
            new AttachedDependencyPropertyGenerator()
        );

        AssertValidDocumentation(compileDiagnostics);

        var generated = Assert.Single(sources);
        Assert.Contains("/// 默认值：new System.Collections.Generic.List&lt;int&gt;()", generated.Text);
        Assert.Contains("defaultValue: (object)(new System.Collections.Generic.List<int>())", generated.Text);
    }

    /// <summary>
    /// 未指定默认值时，回退文本里的泛型尖括号同样要转义。
    /// </summary>
    /// <remarks>
    /// 回退分支走 <c>default({Type})</c>，<c>Type</c> 是完全限定名，
    /// 属性类型是泛型时同样含尖括号。
    /// </remarks>
    [Fact]
    public void GenericPropertyTypeWithoutDefault_ProducesValidXmlDocumentation()
    {
        var (sources, compileDiagnostics) = Run(
            """
            namespace Demo
            {
                [Len.Abp.Wpf.DependencyProperty<System.Collections.Generic.List<int>>("Items")]
                public partial class MyControl : System.Windows.DependencyObject { }
            }
            """
        );

        AssertValidDocumentation(compileDiagnostics);

        var generated = Assert.Single(sources);
        Assert.Contains("&lt;", generated.Text);
        Assert.DoesNotContain("/// 默认值：default(global::System.Collections.Generic.List<int>)", generated.Text);
    }

    /// <summary>
    /// 不含尖括号的表达式行为不变。
    /// </summary>
    /// <remarks>
    /// 回归保护：转义只应影响 XML 特殊字符，普通表达式必须逐字保留。
    /// </remarks>
    [Fact]
    public void PlainDefaultValueExpression_IsUnchanged()
    {
        var (sources, compileDiagnostics) = Run(
            """
            namespace Demo
            {
                [Len.Abp.Wpf.DependencyProperty<int>("Count", DefaultValueExpression = "42")]
                public partial class MyControl : System.Windows.DependencyObject { }
            }
            """
        );

        AssertValidDocumentation(compileDiagnostics);

        var generated = Assert.Single(sources);
        Assert.Contains("/// 默认值：42", generated.Text);
        Assert.Contains("defaultValue: (int)(42)", generated.Text);
    }

    /// <summary>
    /// 含 <c>&amp;</c> 的表达式也要转义。
    /// </summary>
    /// <remarks>
    /// <c>&amp;</c> 是 XML 里最容易漏的字符：它不出现在泛型语法里，
    /// 但位运算表达式（<c>Flags.A | Flags.B</c> 之外的 <c>a &amp; b</c>）会带出来。
    /// </remarks>
    [Fact]
    public void AmpersandInDefaultValueExpression_IsEscaped()
    {
        var (sources, compileDiagnostics) = Run(
            """
            namespace Demo
            {
                [Len.Abp.Wpf.DependencyProperty<int>("Mask", DefaultValueExpression = "1 & 2")]
                public partial class MyControl : System.Windows.DependencyObject { }
            }
            """
        );

        AssertValidDocumentation(compileDiagnostics);

        var generated = Assert.Single(sources);
        Assert.Contains("/// 默认值：1 &amp; 2", generated.Text);
        Assert.Contains("defaultValue: (int)(1 & 2)", generated.Text);
    }

    /// <summary>
    /// 空 <c>DefaultValueExpression</c> 回退到 <c>default(T)</c>，生成的代码可编译。
    /// </summary>
    /// <remarks>
    /// 回归：原实现用 <c>IsNull</c> 判断是否提供表达式，空字符串不是 null，
    /// 于是生成 <c>defaultValue: ,</c>，编译报 CS0839。
    /// </remarks>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void BlankDefaultValueExpression_FallsBackToDefault(string expression)
    {
        var (sources, compileDiagnostics) = Run(
            $$"""
            namespace Demo
            {
                [Len.Abp.Wpf.DependencyProperty<string>("Title", DefaultValueExpression = "{{expression}}")]
                public partial class MyControl : System.Windows.DependencyObject { }
            }
            """
        );

        AssertValidDocumentation(compileDiagnostics);

        var generated = Assert.Single(sources);
        Assert.Contains("defaultValue: default(string)", generated.Text);
        Assert.DoesNotContain("defaultValue: ,", generated.Text);
    }

    /// <summary>
    /// 附加版本的依赖属性同样处理空表达式。
    /// </summary>
    [Fact]
    public void AttachedBlankDefaultValueExpression_FallsBackToDefault()
    {
        var (sources, compileDiagnostics) = Run(
            """
            namespace Demo
            {
                [Len.Abp.Wpf.AttachedDependencyProperty<string>("Title", DefaultValueExpression = "")]
                public static partial class MyAttached { }
            }
            """,
            new AttachedDependencyPropertyGenerator()
        );

        AssertValidDocumentation(compileDiagnostics);

        var generated = Assert.Single(sources);
        Assert.Contains("defaultValue: default(string)", generated.Text);
    }

    /// <summary>
    /// 非空表达式保持原样，不被回退逻辑吞掉。
    /// </summary>
    [Fact]
    public void NonBlankDefaultValueExpression_IsUnchanged()
    {
        var (sources, compileDiagnostics) = Run(
            """
            namespace Demo
            {
                [Len.Abp.Wpf.DependencyProperty<object>("Items", DefaultValueExpression = "new System.Collections.Generic.List<int>()")]
                public partial class MyControl : System.Windows.DependencyObject { }
            }
            """
        );

        AssertValidDocumentation(compileDiagnostics);

        var generated = Assert.Single(sources);
        Assert.Contains("defaultValue: (object)(new System.Collections.Generic.List<int>())", generated.Text);
    }

    /// <summary>
    /// 含尖括号的 <c>Description</c> 同样不破坏 XML。
    /// </summary>
    /// <remarks>
    /// 对照组：<c>Description</c> 一直走 <c>ToXmlCommentText</c>，从未出过问题。
    /// 若这条也失败，说明问题不在转义方法本身而在别处。
    /// </remarks>
    [Fact]
    public void GenericDescription_ProducesValidXmlDocumentation()    {
        var (sources, compileDiagnostics) = Run(
            """
            namespace Demo
            {
                [Len.Abp.Wpf.DependencyProperty<object>("Items", Description = "类型是 List<int>")]
                public partial class MyControl : System.Windows.DependencyObject { }
            }
            """
        );

        AssertValidDocumentation(compileDiagnostics);

        var generated = Assert.Single(sources);
        Assert.Contains("/// 类型是 List&lt;int&gt;", generated.Text);
    }

    private static (IReadOnlyList<GeneratedSource> Sources, IReadOnlyList<Diagnostic> CompileDiagnostics) Run(
        string snippet,
        IIncrementalGenerator? generator = null
    )
    {
        var (sources, compileDiagnostics, _) = GeneratorTestHost.RunAllWithWarnings(
            [generator ?? new DependencyPropertyGenerator()],
            Stubs + "\n" + snippet.TrimEnd() + "\n"
        );

        return (sources, compileDiagnostics);
    }

    /// <summary>
    /// 断言没有 CS1570（XML 文档非法），且没有错误级诊断。
    /// </summary>
    /// <remarks>
    /// 只断言 CS1570 不够：生成代码本身编译不过时同样要失败。
    /// 两者分开断言，失败信息才能直接指向原因。
    /// </remarks>
    private static void AssertValidDocumentation(IReadOnlyList<Diagnostic> compileDiagnostics)
    {
        var errors = compileDiagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(errors);

        var xmlErrors = compileDiagnostics.Where(d => d.Id == "CS1570").ToArray();
        Assert.Empty(xmlErrors);
    }

    /// <summary>
    /// 依赖属性生成器所需的最小桩。
    /// </summary>
    /// <remarks>
    /// 不复用 <see cref="GeneratorTestSources.Stubs"/>：那份桩里
    /// <c>System.Windows.DependencyProperty</c> 同时被声明为类和静态类（CS0101），
    /// 只适合不编译的文本断言。这里要编译并检查 CS1570，桩必须干净。
    /// </remarks>
    private const string Stubs = """
        namespace System.Windows
        {
            public class DependencyObject
            {
                public object GetValue(DependencyProperty dp) => new();
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
