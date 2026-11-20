using Len.Abp.Analyzer.TestSupport;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Len.Abp.Analyzer.Wpf.Tests;

/// <summary>
/// 覆盖：数值默认值的表达式类型必须与属性类型精确匹配。
/// </summary>
/// <remarks>
/// <para>
/// <c>TypedConstant.ToCSharpString()</c> 只保证"文本能表示这个值"，不保证
/// "文本的类型就是目标类型"：<c>double</c> 的 <c>1.0</c> 输出成 <c>1</c>，
/// <c>uint</c> 的 <c>1</c> 不带后缀，两者都是 <c>int</c> 字面量。
/// WPF 在 <c>DependencyProperty.Register</c> 时校验默认值类型，
/// 不匹配就抛 <c>ArgumentException: 默认值类型与属性类型不匹配</c>——
/// 异常发生在静态字段初始化阶段，表现为 <c>TypeInitializationException</c>，
/// 堆栈里看不到生成器。
/// </para>
/// <para>
/// 既有测试只断言生成文本包含某个片段，<c>defaultValue: 1</c> 这种
/// "文本合法、类型错误"的问题在文本层面完全看不出来。因此这里断言的是
/// **表达式本身带正确的类型标记**，并由 <c>DefaultValueTypeContractTests</c>
/// 用真实 WPF 注册做端到端验证。
/// </para>
/// </remarks>
public class NumericDefaultValueTests
{
    /// <summary>
    /// 数值类型的默认值表达式必须带正确的后缀或强制转换。
    /// </summary>
    /// <param name="type">属性类型。</param>
    /// <param name="literal">特性里写的默认值。</param>
    /// <param name="expected">期望的表达式片段。</param>
    /// <remarks>
    /// 窄整型与 <c>float</c> 的期望值是 <c>(byte)(1)</c> 这种简写形式：
    /// <see cref="SymbolDisplayFormat.FullyQualifiedFormat"/> 对 C# 关键字类型
    /// 仍输出关键字，只有非关键字类型才带 <c>global::</c> 前缀。
    /// </remarks>
    [Theory]
    [InlineData("double", "1.0", "1D")]
    [InlineData("double", "0.5", "0.5D")]
    [InlineData("uint", "1", "1U")]
    [InlineData("uint", "42", "42U")]
    [InlineData("long", "1", "1L")]
    [InlineData("ulong", "1", "1UL")]
    [InlineData("float", "1.0f", "(float)(1)")]
    [InlineData("short", "1", "(short)(1)")]
    [InlineData("byte", "1", "(byte)(1)")]
    public void NumericDefaultValue_HasMatchingType(string type, string literal, string expected)
    {
        var (sources, errors, _) = GeneratorTestHost.RunAll(
            [new DependencyPropertyGenerator()],
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

        Assert.Empty(errors);

        var line = generated.Text
            .Split('\n')
            .FirstOrDefault(candidate => candidate.Contains("defaultValue:"))
            ?.Trim();

        Assert.True(
            line is not null && line.Contains(expected),
            $"属性类型 {type} 的默认值表达式应包含 \"{expected}\"，实际是：{line}"
        );
    }

    /// <summary>
    /// <c>int</c> 的字面量本身就是正确类型，不加多余后缀。
    /// </summary>
    /// <remarks>
    /// 对照组：过度处理会让生成文本变脏，也会掩盖真正的类型问题。
    /// <c>nint</c> 与 <c>decimal</c> 不能作特性参数（CS0655），无法在此覆盖。
    /// </remarks>
    [Fact]
    public void IntDefaultValue_HasNoRedundantSuffix()
    {
        var (sources, errors, _) = GeneratorTestHost.RunAll(
            [new DependencyPropertyGenerator()],
            Stubs
                + """

                namespace Demo
                {
                    [Len.Abp.Wpf.DependencyProperty<int>("Value", DefaultValue = 5)]
                    public partial class MyControl : System.Windows.DependencyObject { }
                }
                """
        );

        var generated = Assert.Single(sources);

        Assert.Empty(errors);
        Assert.Contains("defaultValue: 5,", generated.Text);
    }

    /// <summary>
    /// 附加版本的依赖属性同样生成带类型的默认值表达式。
    /// </summary>
    [Theory]
    [InlineData("double", "1.0", "1D")]
    [InlineData("uint", "1", "1U")]
    public void AttachedNumericDefaultValue_HasMatchingType(string type, string literal, string expected)
    {
        var (sources, errors, _) = GeneratorTestHost.RunAll(
            [new AttachedDependencyPropertyGenerator()],
            Stubs
                + $$"""

                namespace Demo
                {
                    [Len.Abp.Wpf.AttachedDependencyProperty<{{type}}>("Value", DefaultValue = {{literal}})]
                    public static partial class MyAttached { }
                }
                """
        );

        var generated = Assert.Single(sources);

        Assert.Empty(errors);
        Assert.Contains($"defaultValue: {expected}", generated.Text);
    }

    /// <summary>
    /// 非有限浮点默认值生成静态成员访问，而不是 <c>NaND</c> 这种不存在的标识符。
    /// </summary>
    /// <param name="type">属性类型。</param>
    /// <param name="literal">特性里写的默认值。</param>
    /// <param name="expected">期望的表达式。</param>
    /// <remarks>
    /// <para>
    /// 回归：<c>TypedConstant.ToCSharpString()</c> 对 <c>double.NaN</c> 返回 <c>"NaN"</c>、
    /// 对 <c>double.PositiveInfinity</c> 返回 <c>"Infinity"</c>、
    /// 对 <c>double.NegativeInfinity</c> 返回 <c>"-Infinity"</c>。
    /// 按数值规则加后缀会得到 <c>NaND</c>、<c>InfinityD</c>、<c>-InfinityD</c>，
    /// 编译报 CS0103。
    /// </para>
    /// <para>
    /// 正确做法是改写成 <c>double.NaN</c> 这类静态成员访问，类型由属性类型决定。
    /// </para>
    /// </remarks>
    [Theory]
    [InlineData("double", "double.NaN", "double.NaN")]
    [InlineData("double", "double.PositiveInfinity", "double.PositiveInfinity")]
    [InlineData("double", "double.NegativeInfinity", "double.NegativeInfinity")]
    [InlineData("float", "float.NaN", "float.NaN")]
    [InlineData("float", "float.PositiveInfinity", "float.PositiveInfinity")]
    public void NonFiniteDefaultValue_UsesStaticMember(string type, string literal, string expected)
    {
        var (sources, errors, _) = GeneratorTestHost.RunAll(
            [new DependencyPropertyGenerator()],
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

        Assert.Empty(errors);
        Assert.Contains($"defaultValue: {expected}", generated.Text);
    }

    /// <summary>
    /// 有限值仍按类型加后缀，不受非有限值分支影响。
    /// </summary>
    /// <remarks>
    /// 对照组：非有限值走静态成员，有限值必须保持原有的后缀/强制转换规则。
    /// </remarks>
    [Fact]
    public void FiniteDoubleDefaultValue_KeepsSuffix()
    {
        var (sources, errors, _) = GeneratorTestHost.RunAll(
            [new DependencyPropertyGenerator()],
            Stubs
                + """

                namespace Demo
                {
                    [Len.Abp.Wpf.DependencyProperty<double>("Value", DefaultValue = double.MaxValue)]
                    public partial class MyControl : System.Windows.DependencyObject { }
                }
                """
        );

        var generated = Assert.Single(sources);

        Assert.Empty(errors);
        Assert.Contains("defaultValue: 1.7976931348623157E+308D", generated.Text);
    }
    /// <remarks>
    /// 不复用 <see cref="GeneratorTestSources.Stubs"/>：那份桩里
    /// <c>System.Windows.DependencyProperty</c> 同时被声明为类和静态类（CS0101），
    /// 只适合不编译的文本断言。这里要编译，桩必须干净。
    /// </remarks>
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
