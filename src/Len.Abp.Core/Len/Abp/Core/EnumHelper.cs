using Volo.Abp;

namespace Len.Abp.Core;

/// <summary>
/// 枚举名称解析的静态入口。
/// </summary>
/// <remarks>
/// 通过静态 <see cref="AbpServiceProvider"/> 取解析器，因此调用前必须已加载
/// <see cref="LenAbpCoreModule"/>（或依赖它的模块），否则抛 <see cref="InvalidOperationException"/>。
/// 需要注入的场景直接用 <see cref="IEnumResolver{TEnum}"/>，不必经过本类。
/// </remarks>
public static class EnumHelper
{
    /// <summary>
    /// 获取枚举项的名称和值。
    /// </summary>
    /// <typeparam name="TEnum">枚举类型。</typeparam>
    /// <returns>枚举项的名称和值集合。</returns>
    public static IEnumerable<NameValue<TEnum>> GetNameValues<TEnum>() where TEnum : struct, Enum
    {
        var resolver = AbpServiceProvider.GetRequiredService<IEnumResolver<TEnum>>();

        return resolver.GetNameValues();
    }
}
