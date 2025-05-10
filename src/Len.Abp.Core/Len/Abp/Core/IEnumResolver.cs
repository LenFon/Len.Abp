using Volo.Abp;

namespace Len.Abp.Core;

/// <summary>
/// Enum Resolver
/// </summary>
/// <typeparam name="TEnum"></typeparam>
public interface IEnumResolver<TEnum> where TEnum : Enum
{
    /// <summary>
    /// Get the name and value of the enum item
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    NameValue<TEnum> GetNameValue(TEnum value);

    /// <summary>
    /// Get the name and value of the enum
    /// </summary>
    /// <returns></returns>
    IEnumerable<NameValue<TEnum>> GetNameValues();
}