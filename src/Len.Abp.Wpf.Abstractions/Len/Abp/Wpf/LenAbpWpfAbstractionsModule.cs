using Len.Abp.Core;
using Volo.Abp.Modularity;

namespace Len.Abp.Wpf;

/// <summary>
/// WPF 抽象层模块。
/// </summary>
/// <remarks>
/// 静态服务提供程序由 <see cref="LenAbpCoreModule"/> 在
/// <c>OnPreApplicationInitialization</c> 中初始化。本模块依赖该模块，
/// 因此它必然参与模块图，无需重复初始化。
/// </remarks>
[DependsOn(typeof(LenAbpCoreModule))]
public class LenAbpWpfAbstractionsModule : AbpModule
{
}
