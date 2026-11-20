namespace Len.Abp.Wpf;

/// <summary>
/// 非泛型的参数值类型兼容判定，供导航参数校验复用。
/// </summary>
/// <remarks>
/// <para>
/// 直接复用 <see cref="ParameterConversion.CanConvert(object?, Type)"/> 的转换逻辑，
/// 不另写一套判定：两处规则一旦分叉，就会出现"导航校验通过但读取失败"的矛盾。
/// </para>
/// <para>
/// 目标类型在运行时才确定，但判定本身已经是非泛型的，因此这里不需要反射构造泛型委托，
/// 也不需要在静态字典里缓存委托。判定结果只取决于值与类型，没有可缓存的状态。
/// </para>
/// </remarks>
public static class ParameterValueConverter
{
    /// <summary>
    /// 判断原始值能否转换为目标类型。
    /// </summary>
    /// <param name="targetType">目标类型，可为可空值类型。</param>
    /// <param name="raw">原始值。</param>
    /// <returns>可转换时返回 <see langword="true"/>。</returns>
    /// <exception cref="ArgumentNullException"><paramref name="targetType"/> 为 <see langword="null"/> 时抛出。</exception>
    public static bool CanConvert(Type targetType, object? raw)
    {
        ArgumentNullException.ThrowIfNull(targetType);

        return ParameterConversion.CanConvert(raw, targetType);
    }
}
