using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;

namespace Len.Abp.Core;

public static class EnumHelper
{
    private static IServiceScope? _scope;

    /// <summary>
    /// Get the name and value of the enum
    /// </summary>
    /// <returns></returns>
    public static IEnumerable<NameValue<TEnum>> GetNameValues<TEnum>() where TEnum : Enum
    {
        if (_scope == null)
        {
            throw new InvalidOperationException("Service provider has not been set. Please call SetServiceProvider first.");
        }

        var resolver = _scope.ServiceProvider.GetRequiredService<IEnumResolver<TEnum>>();

        return resolver.GetNameValues();
    }

    internal static void CloseServiceProvider()
    {
        _scope?.Dispose();
        _scope = null;
    }

    internal static void SetServiceProvider(IServiceProvider serviceProvider)
    {
        _scope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope();
    }
}