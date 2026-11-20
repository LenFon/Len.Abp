using Microsoft.Extensions.Localization;
using Volo.Abp;
using Volo.Abp.Localization;

namespace Len.Abp.Core;

/// <summary>
/// <see cref="IEnumResolver{TEnum}"/> 的默认实现，使用 ABP 的枚举本地化能力生成显示名称。
/// </summary>
/// <typeparam name="TEnum">枚举类型。</typeparam>
/// <remarks>
/// <para>
/// 名称来自 <see cref="IAbpEnumLocalizer"/>：它会依次尝试
/// <c>Enum:{类型名}.{成员名}</c>、<c>{类型名}.{成员名}</c> 与 <c>{成员名}</c> 等键，
/// 找不到本地化资源时回退到成员名本身，因此无需配置资源即可直接使用。
/// </para>
/// <para>
/// 约束是 <c>struct, Enum</c> 而不是仅 <c>Enum</c>：只有前者能使用
/// <see cref="Enum.GetValues{TEnum}"/> 这类泛型重载。仅 <c>Enum</c> 约束时只能调用
/// <see cref="Enum.GetValues(Type)"/>，返回 <see cref="Array"/> 与 <see cref="object"/>，
/// 遍历时每个元素都要装箱。
/// </para>
/// </remarks>
/// <param name="enumLocalizer">ABP 枚举本地化器。</param>
public class EnumResolver<TEnum>(IAbpEnumLocalizer enumLocalizer) : IEnumResolver<TEnum>
    where TEnum : struct, Enum
{
    private readonly IAbpEnumLocalizer _enumLocalizer = enumLocalizer;

    /// <inheritdoc/>
    public NameValue<TEnum> GetNameValue(TEnum value) =>
        new(GetLocalizedName(value), value);

    /// <inheritdoc/>
    public IEnumerable<NameValue<TEnum>> GetNameValues()
    {
        return
        [
            .. Enum.GetValues<TEnum>()
                .Select(value => new NameValue<TEnum>(GetLocalizedName(value), value)),
        ];
    }

    /// <summary>
    /// 获取枚举值的显示名称。
    /// </summary>
    /// <param name="value">枚举值。</param>
    /// <returns>本地化名称；没有对应资源时返回成员名。</returns>
    /// <remarks>
    /// <c>IAbpEnumLocalizer.GetString</c> 在值不是单一命名成员时
    /// （未定义的值、<see cref="FlagsAttribute"/> 组合值）会返回 null，
    /// 而 <see cref="NameValue{T}.Name"/> 声明为非空。这里回退到 <c>ToString()</c>，
    /// 未定义值得到数字字符串，组合值得到以逗号分隔的成员名。
    /// </remarks>
    private string GetLocalizedName(TEnum value) =>
        _enumLocalizer.GetString<TEnum>(value) ?? value.ToString();
}
