namespace Len.Abp.Wpf.Mvvm;

/// <summary>
/// 可被区域承载的视图，提供视图模型与视图名称。
/// </summary>
public interface IView
{
    /// <summary>
    /// 获取视图模型。
    /// </summary>
    object ViewModel { get; }

    /// <summary>
    /// 获取视图名称，用于导航与键控服务解析。
    /// </summary>
    string ViewName { get; }

    /// <summary>
    /// 获取导航到本视图时必须提供的参数：键为参数名，值为参数类型；
    /// 值为 <see langword="null"/> 表示只校验存在性。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 生成器已把契约成员生成到**视图模型**上（<see cref="Navigation.INavigationContract"/>），
    /// 校验器优先读那里。本成员保留给手写 <see cref="IView"/> 实现：
    /// 视图模型不实现 <see cref="Navigation.INavigationContract"/> 时回落到这里。
    /// </para>
    /// <para>
    /// 提供默认实现是为了不破坏手写 <see cref="IView"/> 实现。
    /// </para>
    /// </remarks>
    IReadOnlyDictionary<string, Type?> RequiredNavigationParameters => EmptyParameters;

    /// <summary>
    /// 空参数集合，供默认实现复用。
    /// </summary>
    private static readonly IReadOnlyDictionary<string, Type?> EmptyParameters =
        new Dictionary<string, Type?>(StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// 强类型视图，视图模型以具体类型暴露。
/// </summary>
/// <typeparam name="TViewModel">视图模型类型。</typeparam>
public interface IView<out TViewModel> : IView
    where TViewModel : class
{
    /// <summary>
    /// 获取强类型视图模型。
    /// </summary>
    new TViewModel ViewModel { get; }
}
