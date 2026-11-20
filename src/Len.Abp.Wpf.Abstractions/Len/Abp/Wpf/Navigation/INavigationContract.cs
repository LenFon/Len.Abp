namespace Len.Abp.Wpf.Navigation;

/// <summary>
/// 导航契约：声明视图模型对导航参数的输入要求。
/// </summary>
/// <remarks>
/// <para>
/// 契约放在视图模型上而不是视图上：视图模型才是参数的消费者
/// （它调用 <c>context.Parameters.GetRequired&lt;T&gt;("id")</c>），
/// 视图只是被动外壳。框架的其它导航契约（<see cref="INavigationAware"/>）
/// 也都在视图模型上，放在一起心智负担最小。
/// </para>
/// <para>
/// 另一个好处是可测试性：视图模型是普通类，测试里直接 <c>new</c> 就能断言契约；
/// 视图是 WPF 控件，实例化需要 STA 线程与 <c>InitializeComponent</c>，
/// 测试通常只能用替身，真实产出从未被验证过。
/// </para>
/// <para>
/// 提供默认实现是为了不破坏手写视图模型：未声明必需参数时无需实现任何成员。
/// </para>
/// </remarks>
public interface INavigationContract
{
    /// <summary>
    /// 获取导航到本视图时必须提供的参数：键为参数名，值为参数类型；
    /// 值为 <see langword="null"/> 表示只校验存在性。
    /// </summary>
    /// <remarks>
    /// 由生成器根据 <c>ViewAttribute.RequiredParameters</c> 产出。
    /// 字典内容在编译期确定，生成器把它放在静态字段里由所有实例共享。
    /// </remarks>
    IReadOnlyDictionary<string, Type?> RequiredNavigationParameters => EmptyParameters;

    /// <summary>
    /// 空参数集合，供默认实现复用。
    /// </summary>
    private static readonly IReadOnlyDictionary<string, Type?> EmptyParameters =
        new Dictionary<string, Type?>(StringComparer.OrdinalIgnoreCase);
}
