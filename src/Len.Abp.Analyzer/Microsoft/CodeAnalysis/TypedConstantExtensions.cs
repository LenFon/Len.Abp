using System.ComponentModel;
using System.Globalization;

namespace Microsoft.CodeAnalysis;

public static class TypedConstantExtensions
{
    public static T To<T>(this TypedConstant typedConstant, T defaultValue = default) where T : struct
    {
        if (typedConstant.Value == null) return defaultValue;

        if (typeof(T) == typeof(Guid))
        {
            return (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFromInvariantString(typedConstant.Value.ToString());
        }

        return (T)Convert.ChangeType(typedConstant.Value, typeof(T), CultureInfo.InvariantCulture);
    }

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