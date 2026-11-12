using System.ComponentModel;
using System.Globalization;

namespace Microsoft.CodeAnalysis;

/// <summary>
/// <see cref="TypedConstant"/> 的扩展方法。
/// </summary>
public static class TypedConstantExtensions
{
    /// <summary>
    /// 将特性常量值转换为指定类型。
    /// </summary>
    /// <typeparam name="T">目标类型。</typeparam>
    /// <param name="typedConstant">特性常量。</param>
    /// <param name="defaultValue">常量值为 null 时返回的默认值。</param>
    /// <returns>转换后的值。</returns>
    public static T To<T>(this TypedConstant typedConstant, T defaultValue = default) where T : struct
    {
        if (typedConstant.Value == null) return defaultValue;

        if (typeof(T) == typeof(Guid))
        {
            return (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFromInvariantString(typedConstant.Value.ToString());
        }

        return (T)Convert.ChangeType(typedConstant.Value, typeof(T), CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// 将特性常量值转换为可空类型。
    /// </summary>
    /// <typeparam name="T">目标值类型。</typeparam>
    /// <param name="typedConstant">特性常量。</param>
    /// <returns>转换后的值；常量值为 null 时返回 null。</returns>
    public static T? ToNullable<T>(this TypedConstant typedConstant) where T : struct
    {
        if (typedConstant.Value == null) return null;

        if (typeof(T) == typeof(Guid))
        {
            return (T?)TypeDescriptor.GetConverter(typeof(T)).ConvertFromInvariantString(typedConstant.Value.ToString());
        }

        return (T?)Convert.ChangeType(typedConstant.Value, typeof(T), CultureInfo.InvariantCulture);
    }
}
