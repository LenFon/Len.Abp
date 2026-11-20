using Volo.Abp;

namespace Len.Abp.Core;

/// <summary>
/// 枚举解析器，提供枚举项与名称/值对的互查。
/// </summary>
/// <typeparam name="TEnum">枚举类型。</typeparam>
/// <remarks>
/// 约束是 <c>struct, Enum</c> 而不是仅 <c>Enum</c>：实现方需要用
/// <see cref="Enum.GetValues{TEnum}"/> 这类泛型重载枚举全部成员，
/// 而它们要求值类型约束。仅 <c>Enum</c> 约束时只能走返回 <see cref="object"/> 的非泛型重载，
/// 每次遍历都要装箱。
/// </remarks>
public interface IEnumResolver<TEnum> where TEnum : struct, Enum
{
    /// <summary>
    /// 获取指定枚举项的名称和值。
    /// </summary>
    /// <param name="value">枚举项。</param>
    /// <returns>枚举项的名称和值。</returns>
    NameValue<TEnum> GetNameValue(TEnum value);

    /// <summary>
    /// 获取枚举的名称和值。
    /// </summary>
    /// <returns>枚举项的名称和值集合。</returns>
    IEnumerable<NameValue<TEnum>> GetNameValues();
}
