# Len.Abp.Wpf.Abstractions

WPF 导航与 MVVM 的契约层：视图特性、导航/区域接口与导航参数解析。

本包只包含契约与少量不依赖 UI 线程的实现（参数解析），
不含 UI 线程宿主与区域引擎——那些在
[`Len.Abp.Wpf`](https://www.nuget.org/packages/Len.Abp.Wpf) 中。
单独引用本包适用于"自定义控件库只需特性与契约"的场景。

## 安装

```bash
dotnet add package Len.Abp.Wpf.Abstractions
```

依赖 `Len.Abp.Core`。

## 生成器特性

本包定义 5 个由源生成器消费的特性。生成器随 `Len.Abp.Wpf` 包提供，
**只引本包不会得到生成器**；需要生成代码请引 `Len.Abp.Wpf`。

| 特性 | 用途 |
| --- | --- |
| `DependencyPropertyAttribute` / `<T>` | 生成依赖属性 |
| `AttachedDependencyPropertyAttribute` / `<T>` | 生成附加依赖属性 |
| `RoutedEventAttribute` / `<T>` | 生成路由事件 |
| `AttachedRoutedEventAttribute` / `<T>` | 生成附加路由事件 |
| `ViewAttribute` / `<T>` | 生成 MVVM 视图契约与视图模型注册 |

## 视图契约

`IView` 是区域可承载的最小契约：

```csharp
public interface IView
{
    object ViewModel { get; }
    string ViewName { get; }
    IReadOnlyDictionary<string, Type?> RequiredNavigationParameters { get; }
}
```

`IView<TViewModel>` 提供强类型 `ViewModel`。`RequiredNavigationParameters` 有默认实现，
手写 `IView` 实现不会因新增成员而破坏。

`[View<TViewModel>]` 让生成器产出上述成员与接收视图模型的构造函数：

```csharp
[View<DemoViewModel>(ViewName = "demo")]
public partial class DemoView : UserControl
{
    public DemoView() => InitializeComponent();
}
```

生成器同时会为视图模型追加生命周期接口（前提是它声明为 `partial` 且构造函数可注入），
生命周期跟随视图的 `Lifetime`，可用 `ViewModelLifetime` 单独指定。

`ViewName` 是导航键；`IsShell = true` 注册为单例主窗口；`Lifetime` 控制注册生命周期
（`Transient` / `Scoped` / `Singleton`）；`RequiredParameters` 声明导航输入契约。

`Lifetime = Scoped` 的视图每次导航由独立作用域创建新实例，其 Scoped 依赖来自同一作用域，
视图被拆除时作用域随之释放。需要跨导航复用请用 `Singleton`，需要每次新建请用 `Transient`。

`RequiredParameters` 支持 `"Id"`（只校验存在性）与 `"Id:int"`（同时校验类型）两种写法，
生成器把它翻译成视图模型上的 `INavigationContract.RequiredNavigationParameters`，
由 `Len.Abp.Wpf` 的导航服务在激活前校验。视图模型必须声明为 `partial`；
声明了必需参数却不是 `partial` 时报 `LENABP009`（错误级）。
手写 `IView` 实现仍可通过 `IView.RequiredNavigationParameters` 声明契约。

## 导航契约

| 类型 | 说明 |
| --- | --- |
| `INavigationService` | 按区域名与视图名导航，支持取消与按区域回退 |
| `INavigationContext` | 本次导航的 URI 与参数 |
| `INavigationResult` | 成功标志与失败异常 |
| `INavigationAware` | 视图模型的导航生命周期回调 |
| `IParameters` | 异构参数包，支持查询字符串解析与类型转换 |

`INavigationAware` 有三个回调：`IsNavigationTargetAsync`（是否复用现有实例）、
`OnNavigatedFromAsync`、`OnNavigatedToAsync`。

`GoBackAsync` 在接口上有默认实现，抛 `NotSupportedException`：手写 `INavigationService`
实现不会因新增成员而编译失败，"不支持回退"与"回退失败"也不会混为一谈。
导航历史的实现与 `NavigationOptions.HistoryDepth` 在 `Len.Abp.Wpf` 中。

`IParameters` 的读取有两套语义：

```csharp
var id = context.Parameters.GetValue<int>("id");        // 缺失时返回 default
var required = context.Parameters.GetRequired<int>("id"); // 缺失时抛异常，消息含可用键
```

`GetRequired<T>` 用于"键名拼错"不应被当成"值为默认值"的场景。

## 区域契约

| 类型 | 说明 |
| --- | --- |
| `IRegionBuilder` | 为控件创建区域 |
| `IRegionMemberLifetime` | 视图保活声明（`KeepAlive`） |
| `RegionHost` | XAML 附加属性：`Name` 声明区域，`TabHeader` 声明选项卡标题 |

```xml
<ContentControl len:RegionHost.Name="MainRegion" />
```

`TabHeader` 供 `TabControl` 区域适配器使用，未设置时回退到视图名。

## 宿主与资源

- `IShell`：标记主窗口视图，固定注册为单例。
- `AbpWpfModule`：ABP 模块基类，重写 `ConfigureResources` 可向应用合并资源字典。
- `UriResourceDictionary`：用 URI 字符串构造的资源字典，便于在模块里声明样式来源。

```csharp
[DependsOn(typeof(LenAbpWpfAbstractionsModule))]
public class ControlsModule : AbpWpfModule
{
    public override void ConfigureResources(ICollection<ResourceDictionary> resources) =>
        resources.Add(new UriResourceDictionary(
            "pack://application:,,,/MyControls;component/Themes/Default.xaml"));
}
```

## 模块

`LenAbpWpfAbstractionsModule` 依赖 `LenAbpCoreModule`，本身不注册服务，
只用于把静态 `AbpServiceProvider` 的初始化纳入模块图。

## 许可证

MIT
