using System.ComponentModel;
using System.Globalization;

namespace Len.Abp.Analyzer;

public static class ObjectExtensions
{
    public static T To<T>(this object obj) where T : struct
    {
        if (typeof(T) == typeof(Guid))
        {
            return (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFromInvariantString(obj.ToString());
        }

        return (T)Convert.ChangeType(obj, typeof(T), CultureInfo.InvariantCulture);
    }
}