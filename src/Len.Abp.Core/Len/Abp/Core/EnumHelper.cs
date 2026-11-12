using Volo.Abp;

namespace Len.Abp.Core;

public static class EnumHelper
{
    /// <summary>
    /// 获取枚举项的名称和值。
    /// </summary>
    /// <returns>枚举项的名称和值集合。</returns>
    public static IEnumerable<NameValue<TEnum>> GetNameValues<TEnum>() where TEnum : Enum
    {
        var resolver = AbpServiceProvider.GetRequiredService<IEnumResolver<TEnum>>();

        return resolver.GetNameValues();
    }
}
