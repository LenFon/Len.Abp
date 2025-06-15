using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;

namespace Len.Abp.Core;

public static class EnumHelper
{
    internal static IServiceProvider ServiceProvider { get; set; } = default!;

    /// <summary>
    /// Get the name and value of the enum
    /// </summary>
    /// <returns></returns>
    public static IEnumerable<NameValue<TEnum>> GetNameValues<TEnum>() where TEnum : Enum
    {
        var resolver = ServiceProvider.GetRequiredService<IEnumResolver<TEnum>>();

        return resolver.GetNameValues();
    }
}