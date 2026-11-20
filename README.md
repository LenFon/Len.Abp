# Len.Abp

[![codecov](https://codecov.io/gh/LenFon/Len.Abp/branch/main/graph/badge.svg)](https://codecov.io/gh/LenFon/Len.Abp)

基于 [ABP Framework](https://abp.io) 的 WPF 桌面应用开发框架。用 Roslyn 源生成器消除 WPF 样板代码，用 ABP 的模块化与依赖注入体系承载 WPF 应用，并提供一套 Prism 风格的区域导航。

## 特性

- **源生成器消除样板代码**：依赖属性、附加属性、路由事件、附加路由事件、MVVM 视图绑定，全部由特性驱动生成，并配有专用诊断（`LENABP001`–`LENABP016`）。
- **ABP 依赖注入打通 MVVM**：视图按名称键控注册，导航时从每次导航独立的作用域解析，无需 ViewModelLocator 或 DataTemplate 映射。
- **Prism 风格的区域导航**：在 XAML 中声明区域，通过 `INavigationService` 切换视图，支持导航生命周期回调、视图保活、按区域回退、并发串行化与参数契约校验。
- **WPF 与 ABP 宿主生命周期融合**：UI 线程由 ABP 后台工作器托管，支持单实例运行、资源字典合并与关闭模式配置。

## 项目结构

| 项目 | 目标框架 | 说明 |
| --- | --- | --- |
| `Len.Abp.Core` | net10.0 | 枚举解析器与集合扩展方法 |
| `Len.Abp.Wpf.Abstractions` | net10.0-windows | 契约层：特性、导航/区域接口、参数解析 |
| `Len.Abp.Wpf` | net10.0-windows | 运行时：UI 线程宿主、导航服务、区域引擎；包内含生成器 |
| `Len.Abp.Analyzer` | netstandard2.0 | Roslyn 增量生成器的通用基础设施（不单独发布） |
| `Len.Abp.Analyzer.Wpf` | netstandard2.0 | 5 个 WPF 专用源生成器（不单独发布，产物并入 `Len.Abp.Wpf`） |
| `samples/minimal/MinimalApp` | net10.0-windows | 最小示例：一个 Shell、一个区域、三个页面视图（上手入口） |
| `samples/minimal/MinimalApp.Host` | net10.0-windows | 最小示例的宿主（Generic Host + Serilog，默认 DI 容器） |
| `samples/advanced/WpfApp.Controls` | net10.0-windows | 完整示例的自定义控件库（独立程序集，通过 `XmlnsDefinition` 暴露 XAML 命名空间） |
| `samples/advanced/WpfApp` | net10.0-windows | 完整示例界面（MahApps + MaterialDesign） |
| `samples/advanced/WpfApp.Host` | net10.0-windows | 完整示例宿主（Generic Host + Serilog + Autofac） |

**发布到 NuGet 的只有 3 个包**：`Len.Abp.Core`、`Len.Abp.Wpf.Abstractions`、`Len.Abp.Wpf`。
使用者只需引用 `Len.Abp.Wpf`，运行时与生成器一并获得。

测试项目与类库一一对应：

| 测试项目 | 覆盖的类库 |
| --- | --- |
| `Len.Abp.Core.Tests` | `Len.Abp.Core` |
| `Len.Abp.Wpf.Abstractions.Tests` | `Len.Abp.Wpf.Abstractions` |
| `Len.Abp.Wpf.Tests` | `Len.Abp.Wpf` |
| `Len.Abp.Analyzer.Tests` | `Len.Abp.Analyzer` |
| `Len.Abp.Analyzer.Wpf.Tests` | `Len.Abp.Analyzer.Wpf` |
| `Len.Abp.Analyzer.TestSupport` | 生成器测试共用的编译宿主（非测试项目） |

## 快速开始

### 环境要求

- .NET SDK 10.0 或更高
- Windows（WPF 项目仅支持 Windows）

### 构建与测试

```bash
dotnet build Len.Abp.slnx
dotnet test Len.Abp.slnx
```

> 若构建时报 `CS2015: 是二进制文件而非文本文件`，说明有文件加密/DLP 代理拦截了 `obj` 下的生成文件。清理 `obj`/`bin` 后重试，或使用 `-m:1` 串行构建。

### 运行示例

最小示例只有两个项目、一个区域、四个视图，适合第一次上手。
它演示最基本的导航、回退与必需参数校验，没有自定义控件和嵌套区域：

```bash
dotnet run --project samples/minimal/MinimalApp.Host
```

完整示例覆盖全部能力（27 个视图、8 层嵌套区域、四种生命周期组合、三种内置 + 一种自定义区域适配器、自定义控件库）：

```bash
dotnet run --project samples/advanced/WpfApp.Host
```

## 使用方式

### 1. 声明视图

给 View 加上 `[View<TViewModel>]`，生成器会让它实现 `IView<T>`，并生成一个接收 ViewModel 的构造函数把实例写入 `DataContext`：

```csharp
[View<DemoViewModel>(ViewName = "demo")]
public partial class DemoView : UserControl
{
    public DemoView() => InitializeComponent();
}
```

生成的构造函数形如 `public DemoView(DemoViewModel viewModel) : this() => DataContext = viewModel;`。
视图由容器解析，构造函数注入让依赖显式可见，不依赖 Autofac 的属性注入，默认 DI 容器下同样生效。
因此视图必须保留无参构造函数（`InitializeComponent` 在其中执行）。

视图与视图模型放在各自的目录里，不要写在同一个文件：

```
MyApp/
  Views/           视图（XAML + 代码后置）
  ViewModels/      视图模型
```

生成器会检查这条约定：视图不在 `Views` 下报 `LENABP015`，视图模型不在 `ViewModels` 下报
`LENABP016`，均为警告级。检查只要求路径里出现对应的目录段，按功能分目录
（`Features/Orders/Views/OrderView.cs`）同样合规。

生成器把两者产出到**两个源文件**：视图成员在 `<视图名>.MvvmGenerator.g.cs`，
视图模型的注册与契约成员在 `<视图模型名>.MvvmGenerator.ViewModel.g.cs`。
多个视图共用同一个视图模型时，视图模型那份只生成一次。

视图模型默认由生成器自动注册：只要它是 `partial` 类、构造函数参数都能从容器解析，
生成器就会追加生命周期接口，生命周期跟随视图的 `Lifetime`（可用 `ViewModelLifetime` 单独指定）。
不满足条件时生成 `LENABP006` / `LENABP007` 警告，此时需要自己注册或改用工厂注册。

`ViewName` 会作为键控服务的键，导航时用它定位视图。`IsShell = true` 的视图会被注册为单例主窗口。

视图的注册生命周期由 `Lifetime` 控制，取值来自 MS DI 的 `ServiceLifetime`，默认 `Transient`：

```csharp
[View<MainViewModel>(ViewName = "main", Lifetime = ServiceLifetime.Singleton)]
public partial class MainView : UserControl
{
    public MainView() => InitializeComponent();
}
```

生成器会把生命周期翻译成对应的 ABP 标记接口（`ITransientDependency` / `IScopedDependency` / `ISingletonDependency`）。
Shell 视图固定为单例（`IShell` 本身继承 `ISingletonDependency`），`Lifetime` 设置会被忽略。

> **每次导航为视图创建一个独立作用域**，因此 `Lifetime = Scoped` 的视图每次导航都得到新实例，
> 其 Scoped 依赖也来自同一个导航作用域，视图被拆除时作用域随之释放。
> 需要跨导航复用请用 `Singleton`，需要每次新建请用 `Transient`。

### 2. 声明区域

在 XAML 中用附加属性标记区域宿主：

```xml
<ContentControl len:RegionHost.Name="MainRegion" />
```

区域可以无限层嵌套：视图内部再声明区域，该区域就归属于这个视图实例。

```xml
<!-- DemoView.xaml：DemoView 被导航进 MainRegion 后，DemoRegion 属于这个 DemoView 实例 -->
<UserControl ...>
    <ContentControl len:RegionHost.Name="DemoRegion" />
</UserControl>
```

```csharp
// 在 DemoViewModel 里导航到自己的子区域
await NavigationService.NavigateAsync("DemoRegion", "demo2", IParameters.Create());
```

嵌套区域的语义：

- **所有权按视图实例记录**，不是按视图名。同一个视图存在多个实例时，各自的子区域互不干扰。
- **同名区域优先命中可见的那个**。多个实例拥有同名子区域时，导航命中活动链上的实例，也就是用户当前看到的那个。
- **拆除是递归的**。父视图失活且不保活时，其内部任意深度的区域子树一并摘除，瞬态嵌套视图按同一规则释放。
- **容器管理的嵌套视图保留自己的区域**。区域只在视图首次加载时创建，摘掉后不会重建，所以单例视图的区域必须留着，供下次导航复用。

区域宿主不限于 `ContentControl`，内置三种适配器：

| 宿主控件 | 区域类型 | 行为 |
| --- | --- | --- |
| `ContentControl` | 单活动视图 | 活动视图同步到 `Content` |
| `TabControl` | 单活动视图 | 每个视图一个选项卡，`SelectedItem` 与激活双向同步 |
| `ItemsControl` | 多活动视图 | 所有视图同步到 `ItemsSource`，与激活状态无关 |

`TabControl` 继承自 `ItemsControl`，查找器从具体类型沿继承链向上匹配，因此优先命中自己的适配器。
选项卡标题默认取视图名，需要可读标题时在视图上设置 `RegionHost.TabHeader`：

```xml
<UserControl len:RegionHost.TabHeader="登录">
```

自定义适配器继承 `BasicRegionAdapter<T>` 并用 `[ExposeKeyedService<IRegionAdapter>(typeof(TControl))]` 注册。
基类通过构造函数接收 `IAbpLazyServiceProvider`，派生类把容器注入的实例透传给 `base`：

```csharp
[ExposeKeyedService<IRegionAdapter>(typeof(MyControl))]
public class MyControlRegionAdapter(IAbpLazyServiceProvider abpLazyServiceProvider)
    : BasicRegionAdapter<MyControl>(abpLazyServiceProvider), ISingletonDependency
{
    protected override void Adapt(IRegion region, MyControl regionTarget) { /* 同步视图 */ }

    protected override IRegion CreateRegion(string regionName, IView ownerView) =>
        new SingleActiveRegion(regionName, ownerView.ViewName, ownerView);
}
```

完整可运行的范例见 `samples/advanced/WpfApp/Regions/ListBoxRegionAdapter.cs`：
它把 `ListBox` 适配为单活动视图区域，列表项显示全部视图，选中项与激活双向同步。

#### 自定义区域行为

区域行为用于扩展区域语义（注册、保活、日志等）。以键控方式注册 `IRegionBehavior` 后，
`LenAbpWpfModule` 会在 `PostConfigureServices` 扫描服务描述符并写入 `RegionOptions.BehaviorKeys`，
之后每个新建区域都会自动附加它，不需要在 XAML 或代码里逐个声明：

```csharp
[ExposeKeyedService<IRegionBehavior>(nameof(MyRegionBehavior))]
public class MyRegionBehavior(IMyLog log) : IRegionBehavior, ITransientDependency
{
    public IRegion Region { get; set; } = default!;

    public void Attach() => Region.ActiveViews.CollectionChanged += OnActiveViewsChanged;
}
```

`Attach` 在区域创建时调用，此时 `Region` 已赋值。示例见
`samples/advanced/WpfApp/Regions/RegionActivityBehavior.cs`，它把每次激活与失活写进操作日志。

### 3. 导航

```csharp
await NavigationService.NavigateAsync("MainRegion", "demo2", IParameters.Create("?id=42"));
```

不需要参数时可以省略：

```csharp
await NavigationService.NavigateAsync("MainRegion", "demo2");
```

导航支持取消。取消是唯一不会被包装成失败结果的异常，调用方可以据此区分"被取消"与"导航失败"：

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

try
{
    await NavigationService.NavigateAsync("MainRegion", "demo2", IParameters.Create(), cts.Token);
}
catch (OperationCanceledException)
{
    // 导航在激活之前被取消，区域状态保持不变
}
```

取消只在激活之前生效：一旦开始通知 `OnNavigatedFrom`，界面状态已开始切换，此时不再响应取消。
视图模型自己抛出的 `OperationCanceledException`（例如它内部的超时）仍按导航失败处理，不会穿透。

导航参数支持从查询字符串解析，并自动转换为目标类型：

```csharp
public Task OnNavigatedToAsync(INavigationContext context)
{
    var id = context.Parameters.GetValue<int>("id");
    return Task.CompletedTask;
}
```

`GetValue<T>` 在键缺失时返回 `default`，键名拼错会被当成"值为默认值"。
需要区分这两种情况时用 `GetRequired<T>`，缺失或类型不匹配会抛异常并列出可用键：

```csharp
var id = context.Parameters.GetRequired<int>("id");
```

#### 声明必传参数

视图可以在 `[View]` 上声明输入契约，导航服务在激活视图之前校验，缺参数直接失败而不是让视图带着空值启动：

```csharp
// 只声明名称：只校验存在性
[View<DetailViewModel>(ViewName = "detail", RequiredParameters = ["Id", "Source"])]

// 名称 + 类型：同时校验存在性与类型
[View<DetailViewModel>(ViewName = "detail", RequiredParameters = ["Id:int"])]
```

生成器把声明翻译成**视图模型**上的 `INavigationContract.RequiredNavigationParameters`，
校验失败时 `NavigateAsync` 返回失败结果，异常消息列出缺失项与已提供的参数。
校验发生在激活之前，界面状态不变。

契约放在视图模型上，因为视图模型才是参数的消费者，测试里直接 `new` 就能断言。
视图模型必须声明为 `partial`；声明了必需参数却不是 `partial` 时报 `LENABP009`（错误级），
因为契约生成不了会让参数校验静默失效。手写 `IView` 实现仍可通过
`IView.RequiredNavigationParameters` 声明契约，校验器会回落到那里。

#### 回退

导航历史按区域独立记录，`GoBackAsync` 回退到该区域的上一个视图：

```csharp
var result = await NavigationService.GoBackAsync("MainRegion");

if (!result.Result)
{
    // 没有更早的历史，或回退目标已无法解析
}
```

历史只记录视图名与参数，不记录实例，因此回退走与普通导航完全相同的解析路径：
视图还在就复用，已被释放就新建。回退本身不产生新的历史条目，避免在两个视图之间来回震荡。
每个区域默认保留 10 条，超出丢弃最旧，可通过 `NavigationOptions.HistoryDepth` 调整：

```csharp
Configure<NavigationOptions>(options => options.HistoryDepth = 20);
```

跨区域回退不受支持：区域名是导航的定位单位，父区域回退后子区域的视图可能已被拆除，
跨区域跳转的语义不可预期。需要时由调用方自行记录路径。

#### 并发

导航按调用顺序串行执行，同一时刻只有一次导航在跑。回调内部再次导航（例如切换子区域）
不会死锁——重入直接执行，不排队。

#### 失败诊断

区域不存在、视图解析失败、导航回调异常都会写日志，并带上视图名与区域名。
区域不存在时 `INavigationResult.Exception` 为 `null`（与成功相同），需结合 `Result` 判断；
日志里会列出当前已注册的区域，便于区分"区域名拼错"与"区域根本没创建"。

#### 视图保活

视图失活时是否留在区域中复用，按以下优先级判断：

| 条件 | 行为 |
| --- | --- |
| 视图模型声明 `KeepAlive = true` | 视图与视图模型都保留，下次导航复用同一实例 |
| 视图或视图模型是单例 | 按 IoC 规则保留，下次导航复用容器中的实例 |
| 其余情况（含未实现 `IRegionMemberLifetime`、作用域视图） | 不保留，移出区域并释放 |

```csharp
public class GalleryViewModel : ObservableObject, IRegionMemberLifetime
{
    // 需要保留状态（如日志、输入内容）时显式保活。
    public bool KeepAlive => true;
}
```

未实现 `IRegionMemberLifetime` 等同于 `KeepAlive = false`，避免忘记声明契约导致视图在区域里无限累积。

单例视图（`ISingletonDependency`）即使 `KeepAlive = false` 也会保留：
它由 IoC 容器持有，移除区域不会释放它，反而会丢掉视图内部的嵌套区域，下次导航还得重建整棵视图子树。

作用域视图（`IScopedDependency`）**不自动保活**：每次导航由独立作用域创建新实例，
保留旧实例只会让作用域无法释放。需要保活时显式声明 `KeepAlive = true`。

释放规则：有导航作用域的视图释放作用域（由作用域统一释放它创建的实例）；
其余只释放非单例实例，单例由容器负责销毁。

拆除父视图时会递归处理整棵区域子树，但单例嵌套视图是例外：它们的区域会保留，
否则下次复用该实例时区域已经不存在，导航会失败。

### 4. 生成依赖属性

```csharp
[DependencyProperty<string>("Title", DefaultValue = "Untitled")]
public partial class MyControl : Control
{
    // 生成器提供三个可选的分部钩子
    partial void OnTitleChanged(string oldValue, string newValue) { }
}
```

生成器会产出 `TitleProperty`、CLR 包装属性，以及 `CoerceTitle`、`IsTitleValid`、`OnTitleChanged` 三个分部方法。

特性支持 `Category`、`Description`、`DefaultValueExpression`、`Flags`、`UpdateSourceTrigger` 与 `IsReadOnly`：

```csharp
[DependencyProperty<int>(
    "Rating",
    DefaultValue = 0,
    Category = "行为",
    Description = "当前评分。",
    Flags = FrameworkPropertyMetadataOptions.BindsTwoWayByDefault)]
[DependencyProperty<string>(
    "DisplayText",
    DefaultValue = "未评分",
    IsReadOnly = true)]
public partial class RatingControl : Control { }
```

`IsReadOnly = true` 时生成器改用 `RegisterReadOnly`，产出 `DisplayTextPropertyKey`（`protected internal`）
与 `public static readonly DisplayTextProperty`，CLR 属性的 setter 为 `protected`。
控件内部通过 `SetValue(DisplayTextPropertyKey, value)` 写入，外部调用 `SetValue(DisplayTextProperty, ...)`
会抛 `InvalidOperationException`，只读约束由 WPF 依赖属性系统保证。

### 5. 生成附加依赖属性

```csharp
[AttachedDependencyProperty<int>("MaxTagLength", DefaultValue = 12)]
public partial class TagInputAssist : DependencyObject
{
    static partial void IsMaxTagLengthValid(int value, ref bool result) => result = value is >= 1 and <= 64;
}
```

生成器会产出 `MaxTagLengthProperty`、`GetMaxTagLength` / `SetMaxTagLength`，以及三个静态分部钩子。
宿主必须派生自 `DependencyObject`：生成器用 `DependencyProperty.Register` 注册（不是
`RegisterAttached`），而 `Register` 要求 `ownerType` 派生自 `DependencyObject`，
静态类会在类型初始化时抛 `ArgumentException`。

`IsReadOnly = true` 时改用 `RegisterReadOnly`，`Set` 访问器为 `protected`（静态宿主上退化为 `private`，避免 CS1057），
外部只能通过 `GetXxx` 读取：

```csharp
[AttachedDependencyProperty<int>("TagCount", DefaultValue = 0, IsReadOnly = true)]
public partial class TagInputAssist : DependencyObject { }
```

只读附加属性在 `ControlTemplate` 里绑定时要注意：`StringFormat` 与 `Mode=OneWay` 组合会让绑定取不到值，
应直接写 `{Binding Path=(wc:TagInputAssist.TagCount), RelativeSource={RelativeSource TemplatedParent}}`。

### 6. 生成路由事件

```csharp
[RoutedEvent("Clicked", RoutingStrategy.Bubble)]
public partial class MyControl : Control { }
```

生成器会产出 `ClickedEvent`、事件访问器与 `RaiseClickedEvent` 方法。
泛型重载可指定自定义事件处理程序委托，生成器会从委托签名推断事件参数类型：

```csharp
public delegate void RatingChangedEventHandler(object sender, RatingChangedEventArgs e);

[RoutedEvent<RatingChangedEventHandler>("RatingChanged", RoutingStrategy.Bubble)]
public partial class RatingControl : UserControl { }
```

### 7. 生成附加路由事件

```csharp
[AttachedRoutedEvent<TagAddedEventHandler>("TagAdded", RoutingStrategy.Bubble)]
public static partial class TagInputAssist { }
```

生成器会产出 `TagAddedEvent`、`AddTagAddedHandler` / `RemoveTagAddedHandler` 与 `RaiseTagAddedEvent`。
附加路由事件只提供静态方法，事件源需要自行接线（示例见 `InteractionAssist.EnableDoubleClick`）。

### 8. 生成器诊断

生成器的问题用 `LENABP` 前缀的专用诊断报告，位置指向源码，可跳转、可检索：

| ID | 级别 | 触发条件 |
| --- | --- | --- |
| `LENABP001` | Error | 视图有构造函数但没有无参版本 |
| `LENABP002` | Error | 类型自身或外层类型未声明 `partial` |
| `LENABP003` | Error | 多个视图使用相同 `ViewName` |
| `LENABP004` | Error | 特性参数非法（名称为空、名称重复、处理程序类型非委托） |
| `LENABP005` | Error | 生成器内部异常 |
| `LENABP006` | Warning | 视图模型未声明 `partial`，无法自动注册 |
| `LENABP007` | Warning | 视图模型构造函数含不可注入参数 |
| `LENABP008` | Error | 视图类型是泛型 |
| `LENABP009` | Error | 视图模型声明了 `RequiredParameters` 但不是 `partial` |
| `LENABP010` | Error | 视图模型是值类型 |
| `LENABP011` | Error | 视图模型是开放泛型 |
| `LENABP012` | Error | 视图的宿主类型是静态类或结构体 |
| `LENABP013` | Error | 非附加依赖属性/路由事件的宿主是静态类 |
| `LENABP014` | Warning | `RequiredParameters` 里的类型名无法解析 |
| `LENABP015` | Warning | 视图不在 `Views` 目录下 |
| `LENABP016` | Warning | 视图模型不在 `ViewModels` 目录下 |

001–005、008–013 是错误级。抑制方式与普通编译器诊断相同：`#pragma warning disable LENABP00x`
或 `<NoWarn>LENABP00x</NoWarn>`。不过这些诊断都指向会导致编译失败或行为静默失效的问题，
抑制之前先确认你知道后果。

### 9. 示例程序

完整示例（`samples/advanced`）把自定义控件放在独立类库 `samples/advanced/WpfApp.Controls` 中，
并通过程序集级特性暴露 XAML 命名空间，用法与 `Len.Abp.Wpf.Abstractions` 一致：

```csharp
[assembly: XmlnsPrefix("http://github.com/lenfon/wpfapp/controls", "wc")]
[assembly: XmlnsDefinition("http://github.com/lenfon/wpfapp/controls", "WpfApp.Controls")]
```

引用该程序集后，XAML 里用 URL 前缀即可访问控件，不需要 `clr-namespace`：

```xml
xmlns:wc="http://github.com/lenfon/wpfapp/controls"

<wc:RatingControl Rating="{Binding Rating, Mode=TwoWay}" />
<wc:TagInput wc:TagInputAssist.MaxTagLength="12" />
```

控件库把生成器的能力全部用了一遍：

| 控件 | 覆盖的生成器能力 |
| --- | --- |
| `RatingControl` | 依赖属性（默认值、分类、描述、元数据标志、只读）、校验 / 强制值 / 变更钩子、冒泡 / 隧道 / 直接三种路由策略 |
| `TagInput` + `TagInputAssist` | 附加依赖属性（引用类型、值类型、默认值表达式、只读）、附加路由事件、自定义事件参数 |
| `InteractionAssist` | 非控件宿主承载附加属性与附加路由事件 |

两个控件都是真正的自定义控件：派生自 `Control`，外观由 `ControlTemplate` 提供，
使用方可以整体替换模板。样式按控件拆成独立文件，由 `Themes/Default.xaml` 合并：

```
Themes/
  RatingControl.xaml   RatingControl 的默认样式
  TagInput.xaml        TagInput 的默认样式
  Default.xaml         合并上面两个文件
```

控件库是一个 ABP 模块，只加载 `Default.xaml` 这一个入口，新增控件时改 Default 即可：

```csharp
[DependsOn(typeof(LenAbpWpfAbstractionsModule))]
public class WpfAppControlsModule : AbpWpfModule
{
    public override void ConfigureResources(ICollection<ResourceDictionary> resources)
    {
        resources.Add(new UriResourceDictionary(
            "pack://application:,,,/WpfApp.Controls;component/Themes/Default.xaml"));
    }
}
```

应用侧只需声明模块依赖，样式就会随模块一起合并进 `Application.Resources`：

```csharp
[DependsOn(typeof(LenAbpWpfAbstractionsModule))]
[DependsOn(typeof(WpfAppControlsModule))]
public class WpfAppModule : AbpWpfModule { }
```

主窗口左侧的导航按钮按能力分组，每个按钮对应一个可交互的演示：

| 演示 | 覆盖的能力 |
| --- | --- |
| 控件画廊 | 依赖属性、附加依赖属性、路由事件、附加路由事件（见上表） |
| 单例 / 瞬态 / 作用域视图 | `Lifetime` 的三种取值，界面上的实例编号直接反映复用情况 |
| 视图瞬态 + 视图模型单例 | `ViewModelLifetime` 单独指定：视图每次重建，视图模型始终复用 |
| 嵌套区域（8 层） | 递归拆除、按实例记录所有权、子树回收（含弱引用存活统计） |
| 区域适配器与导航 | `TabControl` / `ItemsControl` / 自定义 `ListBox` 适配器、`TabHeader`、`GoBack`、必需参数校验、取消、自定义区域行为 |
| 导航示例 | 最简的「按钮 → 导航到另一个视图」 |
| 登录界面 | 纯 XAML 布局，视图模型由生成器自动注册 |

「区域适配器与导航」是覆盖最广的一页，可以直接在界面上验证：

- **`TabControl` 区域**：每个视图一个选项卡，`SelectedItem` 与激活双向同步；
  前两个页面的标题来自 `RegionHost.TabHeader`，第三个页面刻意不设置，标题回退为视图名。
- **`ItemsControl` 区域**：所有视图同时显示，与激活状态无关；多活动区域不会被导航自动失活。
- **自定义 `ListBox` 区域**：`samples/advanced/WpfApp/Regions/ListBoxRegionAdapter.cs`
  继承 `BasicRegionAdapter<ListBox>` 并按键控注册，`RegionAdapterFinder` 沿继承链命中它。
  这是「接入自定义宿主控件」的完整范例：只需实现 `Adapt` 与 `CreateRegion` 两个方法。
- **自定义区域行为**：同目录的 `RegionActivityBehavior` 以键控方式注册 `IRegionBehavior`，
  被 `LenAbpWpfModule` 自动收集到 `RegionOptions.BehaviorKeys`，之后每个新建区域都会附加它。
  页面底部的操作日志就是它写入的——区域创建、视图激活与失活都会实时出现在这里。
- **`GoBack`**：在选项卡区域里连续导航几次后点「回退」，回到上一个页面。
- **必需参数**：`DetailView` 声明了 `RequiredParameters = ["id:int", "source"]`。
  「带参数打开详情」成功，「缺参数打开详情」在激活之前失败，界面保持原状，
  失败原因（缺哪个参数、已提供哪些）显示在状态文本里。
- **取消**：发起一次已取消的导航，`OperationCanceledException` 穿透而不被包装成失败结果。

### 10. 宿主配置

应用模块依赖 `LenAbpWpfModule`，它会在初始化时启动两个后台工作器：单实例检测与 WPF UI 线程宿主。

```csharp
[DependsOn(typeof(LenAbpWpfModule))]
public class MyAppModule : AbpWpfModule { }
```

行为通过 `WpfOptions` 配置：

| 选项 | 默认值 | 说明 |
| --- | --- | --- |
| `IsLifetimeLinked` | `true` | UI 应用停止时是否同时停止宿主 |
| `ShutdownMode` | `OnLastWindowClose` | WPF 关闭模式 |
| `SingleInstance` | `false` | 是否只允许单个实例；重复启动时激活已有窗口并退出新实例 |
| `HandleUnhandledExceptions` | `false` | UI 线程未处理异常是否标记为已处理 |

```csharp
Configure<WpfOptions>(options =>
{
    options.SingleInstance = true;
    options.HandleUnhandledExceptions = true;
});
```

`HandleUnhandledExceptions` 为 `false` 时保持 WPF 原有的崩溃行为；为 `true` 时异常被记录后进程继续运行。
后台任务的未观察异常（`TaskScheduler.UnobservedTaskException`）无论该开关如何都会被记录并标记为已观察，
因为未观察异常默认只在 GC 时静默丢弃，不会崩溃也不会留下线索。
应用退出时会先拆除所有区域并释放视图，再按 `IsLifetimeLinked` 决定是否停止宿主。

需要弹窗、上报或按环境区分行为时，实现 `IWpfExceptionHandler` 替换默认实现：

```csharp
[Dependency(ReplaceServices = true)]
[ExposeServices(typeof(IWpfExceptionHandler))]
public sealed class MyExceptionHandler(ILogger<MyExceptionHandler> logger)
    : IWpfExceptionHandler, ISingletonDependency
{
    public bool HandleDispatcherException(Exception exception)
    {
        logger.LogError(exception, "UI 线程异常。");
        MessageBox.Show(exception.Message, "出错了");
        return true;   // 进程继续运行
    }

    public void HandleUnobservedTaskException(Exception exception) =>
        logger.LogError(exception, "后台任务异常。");
}
```

必须同时实现 `ISingletonDependency`：ABP 的约定注册只处理带生命周期标记的类型，
仅靠 `[Dependency]` + `[ExposeServices]` 不会注册。标记已观察由框架完成，实现方不必处理。

## 参与开发

- 用户手册（面向第一次上手）：[docs/USER-GUIDE.md](docs/USER-GUIDE.md)
- 方向与定位：[ROADMAP.md](ROADMAP.md)
- 构建、测试与发布流程：[CONTRIBUTING.md](CONTRIBUTING.md)

## 许可证

[MIT](LICENSE.txt)
