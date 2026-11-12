using Volo.Abp;

namespace Len.Abp.Core;

/// <summary>
/// 枚举解析器。
/// </summary>
/// <typeparam name="TEnum">枚举类型。</typeparam>
public interface IEnumResolver<TEnum> where TEnum : Enum
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
