using Len.Abp.Wpf.Mvvm;

namespace Len.Abp.Wpf.Navigation;

/// <summary>
/// 校验视图声明的导航必需参数。
/// </summary>
/// <remarks>
/// <para>
/// 校验发生在激活之前，因此失败时当前活动视图不会收到 <c>OnNavigatedFrom</c>。
/// 类型判定复用 <see cref="ParameterValueConverter"/>，与 <c>IParameters.TryGetValue{T}</c>
/// 使用同一套转换规则，避免"校验通过但读取失败"的矛盾。
/// </para>
/// <para>
/// 契约优先从视图模型读取（<see cref="INavigationContract"/>），读不到再回落到视图的
/// <see cref="IView.RequiredNavigationParameters"/>。双读是为了兼容手写实现：
/// 生成器已把成员迁到视图模型，但既有手写 <see cref="IView"/> 仍可能自己声明契约。
/// </para>
/// </remarks>
internal static class NavigationParameterValidator
{
    /// <summary>
    /// 校验视图声明的导航必需参数。
    /// </summary>
    /// <param name="view">目标视图。</param>
    /// <param name="context">导航上下文。</param>
    /// <exception cref="InvalidOperationException">缺少必需参数或类型不匹配时抛出。</exception>
    public static void Validate(IView view, INavigationContext context)
    {
        var required = GetRequiredParameters(view);
        if (required.Count == 0)
        {
            return;
        }

        var parameters = context.Parameters;
        List<string>? failures = null;

        foreach (var (name, type) in required)
        {
            // IParameters 的键比较与这里一致，都是大小写不敏感，
            // 因此"校验通过"意味着视图模型随后一定能读到该参数。
            // 早期读取是 Ordinal，两处口径不一致会导致校验通过、读取失败。
            if (!parameters.ContainsKey(name))
            {
                (failures ??= []).Add($"缺少参数 {name}");
                continue;
            }

            if (type is not null && !ParameterValueConverter.CanConvert(type, parameters[name]))
            {
                (failures ??= []).Add($"参数 {name} 的类型不是 {type.Name}");
            }
        }

        if (failures is null)
        {
            return;
        }

        var provided = string.Join(", ", parameters.Keys);

        throw new InvalidOperationException(
            $"视图 \"{view.ViewName}\" 的参数校验失败：{string.Join("；", failures)}。"
                + $"已提供：{(string.IsNullOrEmpty(provided) ? "（无）" : provided)}。"
        );
    }

    /// <summary>
    /// 读取视图的导航契约：优先视图模型，回落到视图。
    /// </summary>
    /// <param name="view">目标视图。</param>
    /// <returns>必需参数集合；两处都没有声明时返回空集合。</returns>
    /// <remarks>
    /// 视图模型优先：它是参数的消费者，契约与消费者放在一起。
    /// 视图侧的回退保留给手写 <see cref="IView"/> 实现。
    /// </remarks>
    private static IReadOnlyDictionary<string, Type?> GetRequiredParameters(IView view)
    {
        if (view.ViewModel is INavigationContract contract)
        {
            return contract.RequiredNavigationParameters;
        }

        return view.RequiredNavigationParameters;
    }
}
