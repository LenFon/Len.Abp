using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Volo.Abp;
using Volo.Abp.Localization;
using Volo.Abp.Modularity;

namespace Len.Abp.Core;

[DependsOn(typeof(AbpLocalizationModule))]
public class LenAbpCoreModule : AbpModule
{
    /// <summary>
    /// 注册枚举解析器的默认实现。
    /// </summary>
    /// <param name="context">服务配置上下文。</param>
    /// <remarks>
    /// ABP 的约定注册会跳过泛型类型，因此开放泛型的 <see cref="EnumResolver{TEnum}"/>
    /// 必须在这里显式注册。使用 <c>TryAdd</c> 语义，允许业务代码注册自己的实现覆盖默认行为。
    /// </remarks>
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.TryAddTransient(typeof(IEnumResolver<>), typeof(EnumResolver<>));
    }

    /// <summary>
    /// 在应用初始化前设置静态辅助方法使用的服务提供程序。
    /// </summary>
    /// <param name="context">应用初始化上下文。</param>
    public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
    {
        AbpServiceProvider.Initialize(context.ServiceProvider.GetRequiredService<IAbpApplication>().ServiceProvider);
    }
}
