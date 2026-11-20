# Len.Abp.Wpf

基于 [ABP Framework](https://abp.io) 的 WPF 桌面应用运行时：UI 线程宿主、导航服务、区域引擎，
并**内含源生成器**——引用本包即同时获得运行时与代码生成，无需再引分析器包。

## 安装

```bash
dotnet add package Len.Abp.Wpf
```

依赖 `Len.Abp.Wpf.Abstractions` 与 `Volo.Abp.BackgroundWorkers`。

包内布局：

```
lib/net10.0-windows*/Len.Abp.Wpf.dll          运行时
analyzers/dotnet/cs/Len.Abp.Analyzer.Wpf.dll  5 个源生成器
analyzers/dotnet/cs/Len.Abp.Analyzer.dll      生成器基础设施
```

分析器只参与编译，不进入运行时输出——`Microsoft.CodeAnalysis` 不会出现在你的应用里。

## 快速开始

### 1. 声明模块

```csharp
[DependsOn(typeof(LenAbpWpfModule))]
public class MyAppModule : AbpWpfModule { }
```

`LenAbpWpfModule` 在应用初始化时启动两个后台工作器：单实例检测与 WPF UI 线程宿主。

### 2. 声明视图

```csharp
[View<MainViewModel>(ViewName = "main", IsShell = true)]
public partial class MainView : Window
{
    public MainView() => InitializeComponent();
}
```

生成器产出 `IView<T>` 实现、`ViewName`、键控服务注册与接收视图模型的构造函数：

```csharp
public MainView(MainViewModel viewModel) : this() => DataContext = viewModel;
```

视图必须保留无参构造函数（`InitializeComponent` 在其中执行）。

视图模型也会被自动注册：`partial` 类且构造函数参数可注入时，生成器追加生命周期接口，
生命周期跟随视图的 `Lifetime`（可用 `ViewModelLifetime` 单独指定）。
已手写生命周期接口的视图模型不会被重复注册；不满足条件时生成 `LENABP006` / `LENABP007` 警告。

### 3. 声明区域并导航

```xml
<ContentControl len:RegionHost.Name="MainRegion" />
```

```csharp
await NavigationService.NavigateAsync("MainRegion", "demo", IParameters.Create("?id=42"));
```

## 区域适配器

| 控件 | 区域类型 | 行为 |
| --- | --- | --- |
| `ContentControl` | 单活动视图 | 活动视图同步到 `Content` |
| `TabControl` | 单活动视图 | 每个视图一个选项卡，`SelectedItem` 与激活双向同步 |
| `ItemsControl` | 多活动视图 | 所有视图同步到 `ItemsSource` |

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

## 导航

### 生命周期

视图模型实现 `INavigationAware` 参与导航：

```csharp
public class DemoViewModel : INavigationAware
{
    public Task<bool> IsNavigationTargetAsync(INavigationContext context) => Task.FromResult(true);
    public Task OnNavigatedFromAsync(INavigationContext context) => Task.CompletedTask;
    public Task OnNavigatedToAsync(INavigationContext context) => Task.CompletedTask;
}
```

### 取消

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

try
{
    await NavigationService.NavigateAsync("MainRegion", "demo", IParameters.Create(), cts.Token);
}
catch (OperationCanceledException)
{
    // 在激活之前被取消，区域状态不变
}
```

取消只在激活之前生效。视图模型自己抛出的 `OperationCanceledException` 仍按导航失败处理。

### 参数契约

视图用 `RequiredParameters` 声明输入契约，导航在激活前校验：

```csharp
[View<DetailViewModel>(ViewName = "detail", RequiredParameters = ["Id:int", "Source"])]
```

只写名称时校验存在性，写成 `名称:类型` 时同时校验类型。校验失败返回失败结果，
异常消息列出缺失项与已提供的参数，界面状态不变。

契约成员生成到视图模型上（`INavigationContract`），因为视图模型才是参数的消费者，
测试里直接 `new` 就能断言。视图模型必须声明为 `partial`；
声明了必需参数却不是 `partial` 时报 `LENABP009`（错误级）。

读取参数时，`GetValue<T>` 缺失返回 `default`，`GetRequired<T>` 缺失或类型不符抛异常并列出可用键。

### 回退

```csharp
var result = await NavigationService.GoBackAsync("MainRegion");
```

历史按区域独立记录，只保存视图名与参数，不保存实例——回退走与普通导航相同的解析路径。
回退本身不产生新历史条目。每个区域默认保留 10 条，超出丢弃最旧：

```csharp
Configure<NavigationOptions>(options => options.HistoryDepth = 20);
```

跨区域回退不受支持：父区域回退后子区域的视图可能已被拆除，语义不可预期。

### 并发

导航按调用顺序串行执行：同一时刻只有一次导航在跑，回调顺序与调用顺序一致。
回调内部再次导航（例如切换子区域）不会死锁——重入直接执行，不排队。

### 失败诊断

三种失败都会写日志，可从日志定位到具体视图名/区域名：

| 失败点 | 级别 | 内容 |
| --- | --- | --- |
| 区域不存在 | `Warning` | 区域名 + 已注册区域列表 |
| 视图解析失败 | `Error` | 视图名 + 区域名 + 原始异常 |
| 导航回调异常 | `Error` | 视图名 + 区域名 + 阶段 |
| 回退时无更早历史 | `Debug` | 区域名（栈空是正常状态，不算失败） |

视图解析失败时异常消息包含视图名与区域名，原始容器异常保留为 `InnerException`。
区域不存在时 `INavigationResult.Exception` 为 `null`（与成功相同），需结合 `Result` 判断。

## 视图保活

视图失活后是否留在区域中复用：

| 条件 | 行为 |
| --- | --- |
| 视图模型声明 `KeepAlive = true` | 保留，下次导航复用同一实例 |
| 视图或视图模型是单例 | 按 IoC 规则保留 |
| 其余情况（含未实现 `IRegionMemberLifetime`、作用域视图） | 移出区域并释放 |

作用域视图不自动保活：每次导航由独立作用域创建新实例，保留旧实例会让作用域无法释放。

## 宿主配置

`WpfOptions` 通过 `Configure<WpfOptions>` 配置：

| 选项 | 默认值 | 说明 |
| --- | --- | --- |
| `IsLifetimeLinked` | `true` | UI 应用停止时是否同时停止宿主 |
| `ShutdownMode` | `OnLastWindowClose` | WPF 关闭模式 |
| `SingleInstance` | `false` | 是否只允许单个实例；重复启动时激活已有窗口 |
| `HandleUnhandledExceptions` | `false` | UI 线程未处理异常是否标记为已处理 |

```csharp
Configure<WpfOptions>(options =>
{
    options.SingleInstance = true;
    options.HandleUnhandledExceptions = true;
});
```

应用退出时会先拆除所有区域并释放视图，再停止宿主。

### 自定义全局异常处理

实现 `IWpfExceptionHandler` 并替换默认实现，可加入弹窗、上报等行为：

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
        return true;
    }

    public void HandleUnobservedTaskException(Exception exception) =>
        logger.LogError(exception, "后台任务异常。");
}
```

必须同时实现 `ISingletonDependency`：ABP 的约定注册只处理带生命周期标记的类型，
仅靠 `[Dependency]` + `[ExposeServices]` 不会注册。
`SetObserved` 由框架调用，实现方不必处理。

## 源生成器

本包内含 5 个生成器，用法见 `Len.Abp.Wpf.Abstractions` 的特性定义：

```csharp
[View<MainViewModel>(ViewName = "main", IsShell = true)]
public partial class MainView : Window { }

[DependencyProperty<string>("Title", DefaultValue = "Untitled")]
public partial class MyControl : Control { }

[AttachedDependencyProperty<int>("MaxTagLength", DefaultValue = 12)]
public partial class TagInputAssist : DependencyObject { }

[RoutedEvent("Clicked", RoutingStrategy.Bubble)]
public partial class MyControl : Control { }

[AttachedRoutedEvent<TagAddedEventHandler>("TagAdded", RoutingStrategy.Bubble)]
public static partial class TagInputAssist { }
```

### 诊断

| ID | 级别 | 触发条件 |
| --- | --- | --- |
| `LENABP001` | Error | 视图有构造函数但没有无参版本 |
| `LENABP002` | Error | 类型自身或外层类型未声明 `partial` |
| `LENABP003` | Error | 多个视图使用相同 `ViewName` |
| `LENABP004` | Error | 特性参数非法（名称为空、名称重复、处理程序类型非委托） |
| `LENABP005` | Error | 生成器内部异常 |
| `LENABP006` | Warning | 视图模型未声明 `partial`，无法自动注册 |
| `LENABP007` | Warning | 视图模型构造函数含不可注入参数 |

001–005 是错误级。抑制方式与普通编译器诊断相同：`#pragma warning disable LENABP00x`
或 `<NoWarn>LENABP00x</NoWarn>`。

## 许可证

MIT
