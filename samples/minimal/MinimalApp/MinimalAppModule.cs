using Len.Abp.Wpf;
using Volo.Abp.Modularity;

namespace MinimalApp;

/// <summary>
/// 最小示例的 WPF 模块。
/// </summary>
/// <remarks>
/// 视图与视图模型由生成器自动注册到容器，本模块无需写任何注册代码。
/// 需要合并自定义资源字典时重写 <see cref="AbpWpfModule.ConfigureResources"/>。
/// </remarks>
[DependsOn(typeof(LenAbpWpfAbstractionsModule))]
public class MinimalAppModule : AbpWpfModule;
