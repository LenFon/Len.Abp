using System.Windows;
using Xunit;

namespace Len.Abp.Wpf.Tests;

/// <summary>
/// 端到端契约：生成的默认值表达式必须能被真实 WPF 接受。
/// </summary>
/// <remarks>
/// <para>
/// 生成器测试只能断言"表达式带正确的类型标记"，无法证明 WPF 真的接受它。
/// WPF 在 <c>DependencyProperty.Register</c> 时校验默认值类型，
/// 不匹配就抛 <see cref="ArgumentException"/>——而该异常发生在静态字段初始化阶段，
/// 使用者看到的是 <see cref="TypeInitializationException"/>，堆栈里看不到生成器。
/// </para>
/// <para>
/// 这里用与生成器**完全相同的表达式形式**注册依赖属性，锁定"表达式 → WPF 接受"
/// 这条契约。若生成器的表达式形式变了而这里没同步，测试会失败。
/// </para>
/// </remarks>
public class DefaultValueTypeContractTests
{
    /// <summary>
    /// 生成器为各数值类型产出的表达式形式都能被 WPF 接受。
    /// </summary>
    /// <param name="propertyType">依赖属性的类型。</param>
    /// <param name="defaultValue">生成器产出的默认值表达式求值结果。</param>
    /// <remarks>
    /// 用 <see cref="MemberDataAttribute"/> 而不是 <see cref="InlineDataAttribute"/>：
    /// <c>decimal</c> 常量不能作特性实参（CS0182）。
    /// </remarks>
    [Theory]
    [MemberData(nameof(NumericCases))]
    public void NumericDefaultValue_IsAcceptedByWpf(Type propertyType, object defaultValue)
    {
        var exception = Record.Exception(() =>
            DependencyProperty.Register(
                $"Contract{propertyType.Name}",
                propertyType,
                typeof(ContractOwner),
                new PropertyMetadata(defaultValue)
            )
        );

        Assert.Null(exception);
    }

    /// <summary>
    /// 各数值类型的测试数据。
    /// </summary>
    public static TheoryData<Type, object> NumericCases => new()
    {
        { typeof(double), 1D },
        { typeof(uint), 1U },
        { typeof(long), 1L },
        { typeof(ulong), 1UL },
        { typeof(float), 1f },
        { typeof(short), (short)1 },
        { typeof(byte), (byte)1 },
        { typeof(decimal), 1m },
        { typeof(int), 1 },
    };

    /// <summary>
    /// 对照组：类型不匹配时 WPF 确实会拒绝。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 若这条也通过（即 WPF 不报错），说明上面的用例证明不了任何东西——
    /// 校验根本没发生。这条锁定"WPF 确实校验默认值类型"这个前提。
    /// </para>
    /// <para>
    /// 断言属性名而不是异常文案：文案随操作系统语言变化
    /// （中文「默认值类型与属性“X”类型不匹配。」、英文「Default value type does not match type of property 'X'.」），
    /// 属性名两边都有。写死中文会让这条测试只在中文系统上通过。
    /// </para>
    /// </remarks>
    [Theory]
    [InlineData(typeof(double), 1)]
    [InlineData(typeof(uint), 1)]
    public void MismatchedDefaultValue_IsRejectedByWpf(Type propertyType, object defaultValue)
    {
        var propertyName = $"Mismatch{propertyType.Name}";

        var exception = Record.Exception(() =>
            DependencyProperty.Register(
                propertyName,
                propertyType,
                typeof(ContractOwner),
                new PropertyMetadata(defaultValue)
            )
        );

        var argumentException = Assert.IsType<ArgumentException>(exception);
        Assert.Contains(propertyName, argumentException.Message);
    }

    /// <summary>
    /// 依赖属性的宿主类型。
    /// </summary>
    private sealed class ContractOwner : DependencyObject;
}
