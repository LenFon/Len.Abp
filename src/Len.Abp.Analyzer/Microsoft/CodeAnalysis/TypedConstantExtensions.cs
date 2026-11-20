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
    /// <remarks>
    /// <see cref="Guid"/> 单独处理：它不实现 <see cref="IConvertible"/>，
    /// <see cref="Convert.ChangeType(object, Type, IFormatProvider)"/> 会抛
    /// <see cref="InvalidCastException"/>。这里用 <see cref="Guid.TryParse(string, out Guid)"/>
    /// 而不是 <c>TypeDescriptor.GetConverter</c>：后者是反射，在裁剪与 AOT 下不可靠，
    /// 而这里只需要处理一种类型。
    /// </remarks>
    public static T To<T>(this TypedConstant typedConstant, T defaultValue = default) where T : struct
    {
        if (typedConstant.Value == null) return defaultValue;

        if (typeof(T) == typeof(Guid))
        {
            return Guid.TryParse(typedConstant.Value.ToString(), out var guid)
                ? (T)(object)guid
                : defaultValue;
        }

        return (T)Convert.ChangeType(typedConstant.Value, typeof(T), CultureInfo.InvariantCulture);
    }
}
