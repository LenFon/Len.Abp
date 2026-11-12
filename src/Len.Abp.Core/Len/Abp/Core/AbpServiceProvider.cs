using Microsoft.Extensions.DependencyInjection;

namespace Len.Abp.Core;

/// <summary>
/// 为静态入口（附加属性回调、静态辅助方法）提供访问根服务提供程序的能力，
/// 因为这些代码路径没有依赖注入上下文。
/// </summary>
/// <remarks>
/// 该提供程序是进程级全局的。再次调用 <see cref="Initialize"/> 会替换它，
/// 从而支持多个容器和测试宿主；最后初始化的提供程序生效。
/// </remarks>
public static class AbpServiceProvider
{
    private static IServiceProvider? _provider;

    /// <summary>
    /// 设置静态辅助方法使用的根服务提供程序。
    /// </summary>
    public static void Initialize(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        Volatile.Write(ref _provider, serviceProvider);
    }

    /// <summary>
    /// 获取根服务提供程序。
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// 尚未初始化任何提供程序时抛出，通常意味着调用 <see cref="Initialize"/> 的 ABP 模块未被加载。
    /// </exception>
    public static IServiceProvider Current =>
        Volatile.Read(ref _provider)
        ?? throw new InvalidOperationException(
            $"{nameof(AbpServiceProvider)} 尚未初始化。请确认负责初始化它的 ABP 模块已被加载。"
        );

    /// <summary>
    /// 从根服务提供程序解析必需的服务。
    /// </summary>
    public static T GetRequiredService<T>() where T : notnull => Current.GetRequiredService<T>();
}
