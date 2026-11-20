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
    public LifetimeSingletonView() =>
        Configure("单例视图（Lifetime = Singleton）", "生成器把 Lifetime 翻译成 ISingletonDependency，反复导航始终复用同一实例。");
}

/// <summary>
/// 瞬态生命周期视图，每次导航都创建新的视图与视图模型实例。
/// </summary>
[View<LifetimeTransientViewModel>(ViewName = "lifetime-transient")]
public partial class LifetimeTransientView : LifetimeDemoView
{
    public LifetimeTransientView() =>
        Configure("瞬态视图（Lifetime = Transient，默认）", "生成器默认翻译成 ITransientDependency，每次导航都会得到新的实例编号。");
}

/// <summary>
/// 作用域生命周期视图，演示 <see cref="ServiceLifetime.Scoped"/> 的翻译结果。
/// </summary>
/// <remarks>
/// 每次导航为视图创建独立作用域，因此作用域视图每次导航都得到新实例，
/// 实例编号会变化，与瞬态视图的表现一致。
/// </remarks>
[View<LifetimeScopedViewModel>(ViewName = "lifetime-scoped", Lifetime = ServiceLifetime.Scoped)]
public partial class LifetimeScopedView : LifetimeDemoView
{
    public LifetimeScopedView() =>
        Configure("作用域视图（Lifetime = Scoped）", "生成器把 Lifetime 翻译成 IScopedDependency。每次导航创建独立作用域，因此每次都是新实例。");
}

/// <summary>
/// 视图瞬态、视图模型单例的演示，展示 <c>ViewModelLifetime</c> 单独指定。
/// </summary>
/// <remarks>
/// 视图本身每次导航都新建（默认瞬态），但它拿到的视图模型始终是同一个实例。
/// 适用于「界面要重建、状态要保留」的场景。
/// </remarks>
[View<LifetimeSharedStateViewModel>(
    ViewName = "lifetime-shared-state",
    ViewModelLifetime = ServiceLifetime.Singleton,
    HasViewModelLifetime = true
)]
public partial class LifetimeSharedStateView : LifetimeDemoView
{
    public LifetimeSharedStateView() =>
        Configure(
            "视图瞬态 + 视图模型单例（ViewModelLifetime = Singleton）",
            "视图每次导航都重建，但视图模型始终是同一个实例：实例编号不变，进入次数持续累加。"
        );
}
