using Len.Abp.Wpf;
using Volo.Abp.Modularity;

namespace MinimalApp.Host;

/// <summary>
/// 宿主模块：引入 WPF 运行时。
/// </summary>
/// <remarks>
/// <see cref="LenAbpWpfModule"/> 负责启动 UI 线程后台工作器并收集区域行为键，
/// Shell 窗口由该工作器创建，因此宿主只需声明依赖，不需要手写任何启动代码。
/// 不依赖 Autofac：框架只使用 Microsoft.Extensions.DependencyInjection 的 keyed services，
/// 默认容器即可满足。
/// </remarks>
[DependsOn(typeof(LenAbpWpfModule))]
[DependsOn(typeof(MinimalAppModule))]
public class MinimalAppHostModule : AbpModule
{
    /// <summary>
    /// 配置 WPF 运行选项。
    /// </summary>
    /// <param name="context">服务配置上下文。</param>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<WpfOptions>(options =>
        {
            // 打开后 UI 线程的未处理异常会被记录到日志，进程继续运行。
            // 默认关闭是为了保持 WPF 原有的崩溃语义，示例里显式打开以便观察异常处理效果。
            options.HandleUnhandledExceptions = true;
        });
    }
}
