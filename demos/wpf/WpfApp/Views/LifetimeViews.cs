using Len.Abp.Wpf.Mvvm;
using Microsoft.Extensions.DependencyInjection;
using WpfApp.ViewModels;

namespace WpfApp.Views;

/// <summary>
/// 单例生命周期视图，反复导航时复用同一个视图与视图模型实例。
/// </summary>
[View<LifetimeSingletonViewModel>(ViewName = "lifetime-singleton", Lifetime = ServiceLifetime.Singleton)]
public partial class LifetimeSingletonView : LifetimeDemoView
{
    /// <summary>
    /// 初始化视图。
    /// </summary>
    public LifetimeSingletonView() =>
        Configure("单例视图（Lifetime = Singleton）", "生成器把 Lifetime 翻译成 ISingletonDependency，反复导航始终复用同一实例。");
}

/// <summary>
/// 瞬态生命周期视图，每次导航都创建新的视图与视图模型实例。
/// </summary>
[View<LifetimeTransientViewModel>(ViewName = "lifetime-transient")]
public partial class LifetimeTransientView : LifetimeDemoView
{
    /// <summary>
    /// 初始化视图。
    /// </summary>
    public LifetimeTransientView() =>
        Configure("瞬态视图（Lifetime = Transient，默认）", "生成器默认翻译成 ITransientDependency，每次导航都会得到新的实例编号。");
}

/// <summary>
/// 作用域生命周期视图，演示 <see cref="ServiceLifetime.Scoped"/> 的翻译结果。
/// </summary>
[View<LifetimeScopedViewModel>(ViewName = "lifetime-scoped", Lifetime = ServiceLifetime.Scoped)]
public partial class LifetimeScopedView : LifetimeDemoView
{
    /// <summary>
    /// 初始化视图。
    /// </summary>
    public LifetimeScopedView() =>
        Configure("作用域视图（Lifetime = Scoped）", "生成器把 Lifetime 翻译成 IScopedDependency，作用域内复用同一实例。");
}
