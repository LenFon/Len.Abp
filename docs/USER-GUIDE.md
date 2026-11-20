# Len.Abp 用户手册

> 面向第一次接触本项目的读者。不需要懂 ABP 的内部机制，也不需要懂 Roslyn 源生成器。
> 只要会写 C# 和 XAML，就能按本文把功能用起来。

**本文是自包含的**：所有需要的知识都写在本文里，不需要再去读其它文档。
示例代码全部来自 `samples/` 下真实可运行的项目，可以直接对照。
凡是本文没写的能力，就是项目里没有；凡是写了"不能"的地方，都是源码里明确不支持的。

---

## 目录

1. [这个项目是什么](#1-这个项目是什么)
2. [先跑起来：两个示例程序](#2-先跑起来两个示例程序)
3. [在自己的项目里用起来](#3-在自己的项目里用起来)
4. [功能一：声明视图](#4-功能一声明视图)
5. [功能二：声明区域](#5-功能二声明区域)
6. [功能三：导航](#6-功能三导航)
7. [功能四：导航参数与必需参数校验](#7-功能四导航参数与必需参数校验)
8. [功能五：导航历史与回退](#8-功能五导航历史与回退)
9. [功能六：视图保活与生命周期](#9-功能六视图保活与生命周期)
10. [功能七：生成依赖属性](#10-功能七生成依赖属性)
11. [功能八：生成附加依赖属性](#11-功能八生成附加依赖属性)
12. [功能九：生成路由事件](#12-功能九生成路由事件)
13. [功能十：生成附加路由事件](#13-功能十生成附加路由事件)
14. [功能十一：宿主配置](#14-功能十一宿主配置)
15. [功能十二：全局异常处理](#15-功能十二全局异常处理)
16. [功能十三：自定义区域适配器与区域行为](#16-功能十三自定义区域适配器与区域行为)
17. [功能十四：资源字典合并](#17-功能十四资源字典合并)
18. [功能十五：枚举名称解析](#18-功能十五枚举名称解析)
19. [诊断速查表](#19-诊断速查表)
20. [常见问题](#20-常见问题)
21. [附录：公开 API 一览](#21-附录公开-api-一览)

---

## 1. 这个项目是什么

Len.Abp 是一个**基于 ABP Framework 的 WPF 桌面应用开发框架**。

它解决两个具体问题：

1. **WPF 的样板代码太多**。依赖属性、附加属性、路由事件，每个都要写几十行注册代码。
   本项目用 Roslyn 源生成器，让你只写一行特性，剩下的代码在编译期生成。
2. **桌面端的依赖注入和模块体系与后端割裂**。本项目让 WPF 应用直接跑在 ABP 的模块
   与依赖注入体系上，后端已有的模块、服务、生命周期管理可以沿用。

在此之上，它还提供一套 Prism 风格的**区域导航**：在 XAML 里声明一块区域，
用代码往这块区域里切换视图，支持嵌套、回退、参数校验、视图保活。

### 1.1 三个包，各管什么

项目发布到 NuGet 的只有三个包：

| 包 | 作用 | 什么时候需要 |
| --- | --- | --- |
| `Len.Abp.Core` | 枚举名称解析、集合遍历扩展 | 被另外两个包依赖，通常不用单独引 |
| `Len.Abp.Wpf.Abstractions` | 契约层：5 个生成器特性、导航与区域接口、导航参数解析 | 只写自定义控件库、不需要运行时的时候 |
| `Len.Abp.Wpf` | 运行时：UI 线程宿主、导航服务、区域引擎，**并且内含生成器** | 写完整应用时引这一个就够 |

**关键点：生成器在 `Len.Abp.Wpf` 包里，不在 `Abstractions` 包里。**
只引 `Len.Abp.Wpf.Abstractions` 会得到特性定义，但不会生成任何代码。

### 1.2 现在能装到 NuGet 上吗

**不能。** 项目尚未发布任何版本，所有包停在 `0.0.0-dev`。
所以本文第 3 节讲的是**从源码引用**的做法，不是 `dotnet add package`。

等包正式发布后，第 3 节的引用方式会简化为引一个 `Len.Abp.Wpf` 包；
其余各章的内容不受影响。

---

## 2. 先跑起来：两个示例程序

项目自带两个示例，都放在 `samples/` 下。建议按顺序跑。

### 2.1 环境要求

- Windows（WPF 只能跑在 Windows 上）
- .NET SDK 10.0 或更高

除此之外不需要额外安装任何工具。

### 2.2 最小示例

```bash
dotnet run --project samples/minimal/MinimalApp.Host
```

窗口标题是"Len.Abp 最小示例"，界面很朴素：顶部一排按钮，下方一块空白区域。

| 按钮 | 会发生什么 |
| --- | --- |
| `Hello` | 下方区域显示 "Hello, Len.Abp!" |
| `Goodbye` | 下方区域显示 "Goodbye!" |
| `回退` | 回到上一个视图；没有更早的历史时，状态栏显示"没有更早的历史" |
| `带参数问候` | 显示 "Hello, Len!"，因为传了 `?name=Len` |
| `缺参数问候` | **导航失败**，状态栏显示参数校验失败的原因，界面保持原样 |

这五个按钮覆盖了导航、回退、参数校验三件事。想理解本项目的最小用法，
看 `samples/minimal/MinimalApp` 里的这几个文件就够：

- `Views/MainWindow.xaml` —— 一个 `ContentControl` 区域
- `Views/MainWindow.xaml.cs` —— 一行 `[View<MainViewModel>(ViewName = "main", IsShell = true)]`
- `Views/HelloView.xaml.cs` —— 一行 `[View<HelloViewModel>(ViewName = "hello")]`
- `Views/GreetingView.xaml.cs` —— 一行带 `RequiredParameters = ["name"]` 的 `[View]`
- `ViewModels/MainViewModel.cs` —— 四个 `[RelayCommand]` 方法（Hello / Goodbye / 回退 / 问候）调用导航服务

### 2.3 完整示例

```bash
dotnet run --project samples/advanced/WpfApp.Host
```

窗口左侧是导航栏，按能力分组，共 9 个按钮。每个按钮对应一个可交互的演示：

| 按钮 | 演示什么 | 怎么观察 |
| --- | --- | --- |
| 控件画廊 | 依赖属性、附加属性、路由事件、附加路由事件 | 点星星改评分，看下方事件日志依次出现隧道 / 冒泡 / 直接三种事件；在标签框输入后回车；在蓝色区域双击 |
| 单例视图 | `Lifetime = Singleton` | 反复点这个按钮，界面上的"视图模型实例编号"始终不变 |
| 瞬态视图 | `Lifetime = Transient`（默认） | 每次点都得到新的实例编号 |
| 作用域视图 | `Lifetime = Scoped` | 每次点都得到新的实例编号（原因见 9.3） |
| 视图瞬态 + 视图模型单例 | `ViewModelLifetime` 单独指定 | 实例编号不变，但"进入次数"持续累加 |
| 嵌套区域（8 层） | 递归嵌套、按实例记录所有权、子树回收 | 逐层点"进入下一层"，再点"统计实例存活"看弱引用统计 |
| 区域适配器与导航 | `TabControl` / `ItemsControl` / 自定义 `ListBox` 三种区域，回退、参数校验、取消、全局异常 | 见下方说明 |
| 导航示例 | 最简的"按钮 → 导航到另一个视图" | 点 `Click Me`，下方区域换成 Demo2 |
| 登录界面 | 纯 XAML 布局，视图模型由生成器自动注册 | 只是界面，没有登录逻辑 |

「区域适配器与导航」这一页信息量最大，建议这样走一遍：

1. 点「选项卡一」，`TabControl` 里出现一个选项卡。再点「选项卡二」，
   选项卡一消失、只剩选项卡二——因为示例里的页面视图模型没有声明保活，
   切走就被拆除了（这是单活动区域的正常行为，见 5.2）。
   点「选项卡三」时标题显示为 `tab-page-3` 而不是中文，因为它故意没设置 `RegionHost.TabHeader`。
2. 点「回退」，回到上一个页面。历史按区域独立记录，所以能退回去。
3. 点「带参数打开详情」，成功；点「缺参数打开详情」，失败，状态文本说明缺哪个参数。
4. 点「追加列表项一」「追加列表项二」，`ItemsControl` 里两个视图同时显示——
   它是多活动区域，不会因为导航而失活。
5. 点「列表项一」「列表项二」，`ListBox` 里只有选中项处于激活状态。
6. 点「发起一次已取消的导航」，状态显示 `OperationCanceledException` 已穿透。
7. 点「在 UI 线程抛异常」，会弹窗并记入异常列表，进程不退出。
8. 页面底部的黑色"操作日志"由自定义区域行为写入，区域创建、视图激活与失活都会实时出现。

### 2.4 构建失败怎么办

如果构建时报：

```
CS2015: 是二进制文件而非文本文件
```

说明有文件加密或 DLP 代理拦截了 `obj` 下的生成文件。按顺序试：

1. 清理 `obj` 和 `bin` 后重新构建；
2. 用串行构建：`dotnet build Len.Abp.slnx -m:1`。

---

## 3. 在自己的项目里用起来

### 3.1 从源码引用（当前唯一可行方式）

推荐按示例的结构拆成两个项目：**UI 项目**（放视图与视图模型）和**宿主项目**（可执行）。
这样 UI 项目只依赖契约层，宿主负责引入运行时。

**第一步：UI 项目引用契约层与生成器。** 参照 `samples/minimal/MinimalApp/MinimalApp.csproj`：

```xml
<ItemGroup>
  <!-- 契约层：特性、导航/区域接口、参数解析 -->
  <ProjectReference Include="..\..\src\Len.Abp.Wpf.Abstractions\Len.Abp.Wpf.Abstractions.csproj" />

  <!-- 生成器必须单独引用，见下方说明 -->
  <ProjectReference Include="..\..\src\Len.Abp.Analyzer\Len.Abp.Analyzer.csproj"
                    OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
  <ProjectReference Include="..\..\src\Len.Abp.Analyzer.Wpf\Len.Abp.Analyzer.Wpf.csproj"
                    OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
</ItemGroup>
```

**第二步：宿主项目引用运行时与 UI 项目。** 参照 `samples/minimal/MinimalApp.Host/MinimalApp.Host.csproj`：

```xml
<ItemGroup>
  <ProjectReference Include="..\..\src\Len.Abp.Wpf\Len.Abp.Wpf.csproj" />
  <ProjectReference Include="..\MyApp\MyApp.csproj" />
</ItemGroup>
```

> **为什么生成器要单独引用？**
> `Len.Abp.Wpf.csproj` 内部确实以 `OutputItemType="Analyzer"` 引用了两个生成器项目，
> 但这种引用**不会传递给引用它的项目**。所以使用方必须自己再引一次，
> 否则 `[View<T>]` 之类的特性不会生成任何代码，而且不会报错——只是静默地什么都不发生。
> 仓库里两个示例的 UI 项目都显式引用了这两个生成器项目，原因就在这里。
>
> 包已经配置好把生成器打进 `analyzers/dotnet/cs/`（见 `Len.Abp.Wpf.csproj`），
> 发布到 NuGet 后引一个包就够了，不需要这一步。

> **也可以只用一个项目。** 完整示例的 `WpfApp` 同时引了 `Len.Abp.Wpf` 与两个生成器
> （因为它要写自定义区域适配器，需要运行时程序集）。拆不拆项目取决于你是否需要
> "UI 库不依赖运行时"这种边界，功能上没有区别。

**第三步：写宿主入口。** 参照 `samples/minimal/MinimalApp.Host/Program.cs`：

```csharp
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Volo.Abp;

var builder = Host.CreateApplicationBuilder(args);

await builder.Services.AddApplicationAsync<MyHostModule>();

var host = builder.Build();

await host.InitializeAsync();
await host.RunAsync();
```

注意这里**没有** `new Application()`，也**没有** `app.Run()`。
WPF 的 `Application` 和消息循环由框架在后台工作器里创建（见 14.1）。

**第四步：声明模块。**

```csharp
// 宿主模块：引入 WPF 运行时
[DependsOn(typeof(LenAbpWpfModule))]
[DependsOn(typeof(MyAppModule))]
public class MyHostModule : AbpModule { }

// 应用模块：放视图和业务
[DependsOn(typeof(LenAbpWpfAbstractionsModule))]
public class MyAppModule : AbpWpfModule { }
```

`AbpWpfModule` 是 `AbpModule` 的子类，只多了一个 `ConfigureResources` 方法（见第 17 节）。

**第五步：写一个 Shell 窗口。**

```csharp
[View<MainViewModel>(ViewName = "main", IsShell = true)]
public partial class MainWindow : Window
{
    public MainWindow() => InitializeComponent();
}
```

`IsShell = true` 的视图会被注册为单例，并在应用启动时自动 `Show()`。
**它必须派生自 `Window`**：框架在 UI 线程准备阶段会把所有 `IShell` 转成 `Window`
（`GetServices<IShell>().Cast<Window>()`），不是 `Window` 的会在启动时抛
`InvalidCastException`，整个应用起不来。

### 3.2 不依赖 Autofac

框架只使用 `Microsoft.Extensions.DependencyInjection` 的 keyed services，
默认容器即可运行。最小示例（`MinimalApp.Host`）就是纯默认容器，没有引 Autofac。
完整示例（`WpfApp.Host`）引了 Autofac，但那只是演示"也能用 Autofac"，不是必需。

框架内部有一处专门为"默认容器也能跑"做的处理：`BackgroundWorkerBase.LazyServiceProvider`
由 ABP 的属性注入填充，而属性注入只有 Autofac 集成提供。`LenAbpWpfModule`
在 `OnPreApplicationInitialization` 里手动给所有后台工作器补齐这个属性，
所以用默认容器时后台工作器不会在 `Logger` 上抛 `NullReferenceException`。

### 3.3 目录结构建议

参照完整示例的组织方式：

```
MyApp/
  Views/            视图（XAML + 代码后置）
  ViewModels/       视图模型
  Regions/          自定义区域适配器与区域行为（需要时才建）
  MyAppModule.cs    应用模块
MyApp.Host/
  Program.cs        入口
  MyHostModule.cs   宿主模块
```

如果不需要自定义区域适配器，UI 项目可以只引 `Len.Abp.Wpf.Abstractions`，
运行时由宿主项目引入（最小示例就是这么分的）。

视图与视图模型必须分目录存放，详见 [4.2 目录约定](#42-目录约定)。

---

## 4. 功能一：声明视图

### 4.1 一行特性搞定

```csharp
using Len.Abp.Wpf.Mvvm;

[View<DemoViewModel>(ViewName = "demo")]
public partial class DemoView : UserControl
{
    public DemoView() => InitializeComponent();
}
```

这一行 `[View<DemoViewModel>(ViewName = "demo")]` 会在编译期生成：

1. 让 `DemoView` 实现 `IView<DemoViewModel>`，提供强类型的 `ViewModel` 属性；
2. 生成 `ViewName` 属性，返回 `"demo"`；
3. 生成一个接收视图模型的构造函数：

   ```csharp
   public DemoView(DemoViewModel viewModel) : this() => DataContext = viewModel;
   ```

4. 把视图以 `"demo"` 为键注册到容器，导航时按这个键解析。

生成的代码分成**两个文件**：视图的成员写在 `<视图名>.MvvmGenerator.g.cs`，
视图模型的注册与契约成员写在 `<视图模型名>.MvvmGenerator.ViewModel.g.cs`。
多个视图共用同一个视图模型时，视图模型那份只生成一次。

### 4.2 目录约定

按职责把类型放进对应目录：

```
MyApp/
  Views/           视图（XAML + 代码后置），以及没有 XAML 的纯代码视图
  ViewModels/      视图模型
```

**生成器会检查这条约定**，违反时报警告：

| 情况 | 诊断 |
| --- | --- |
| 视图不在 `Views` 目录下 | `LENABP015`（警告） |
| 视图模型不在 `ViewModels` 目录下 | `LENABP016`（警告） |

检查只要求路径里出现对应的目录段，不要求它是项目根下的第一层——
按功能分目录（`Features/Orders/Views/OrderView.cs`）同样合规。
目录名比较忽略大小写，`views` 与 `Views` 等价。

视图与视图模型**不要写在同一个文件里**。两类类型混在一个文件里时，
读代码的人无法从目录判断某个类型是视图还是视图模型，
生成器产出的两个文件也会与源文件的组织方式对不上。

视图模型只在它是 `partial` 时检查：非 `partial` 时生成器不会为它追加任何成员，
目录问题与生成器无关，那种情况已由 `LENABP006` 覆盖。

### 4.3 必须遵守的三条规则

| 规则 | 不遵守会怎样 |
| --- | --- |
| 视图类必须写 `partial` | 报 `LENABP002`（错误），不生成代码 |
| 视图必须有无参构造函数 | 报 `LENABP001`（错误）。生成的构造函数要 `: this()` 链过去 |
| 视图不能是泛型 | 报 `LENABP008`（错误）。特性参数不能用类型参数 |

无参构造函数里要调 `InitializeComponent()`，否则视图没有视觉树。
如果你完全不写构造函数，生成器会帮你补一个带 `InitializeComponent()` 的无参构造函数。

### 4.4 `[View]` 的六个参数

```csharp
[View<DemoViewModel>(
    ViewName = "demo",                              // 导航用的名字
    IsShell = false,                                // 是否为主窗口
    Lifetime = ServiceLifetime.Transient,           // 视图在容器里的生命周期
    RequiredParameters = ["id:int", "source"],      // 导航必需参数
    ViewModelLifetime = ServiceLifetime.Singleton,  // 视图模型的生命周期
    HasViewModelLifetime = true                     // 上面那行是否生效
)]
```

逐个说明：

**`ViewName`** —— 导航时用它定位视图。可以不写，不写时视图**不会**按名字注册到容器，
导航到它必然失败；但它仍然按类型注册，可以注入 `IView<T>` 或具体视图类型来解析。
显式设成空字符串或纯空白会报 `LENABP004`。

**`IsShell`** —— 设为 `true` 时视图被注册为单例主窗口，并在启动时自动显示。
此时 `Lifetime` 设置会被忽略（`IShell` 本身继承 `ISingletonDependency`）。

**`Lifetime`** —— 视图的注册生命周期，取值来自 MS DI 的 `ServiceLifetime`：

| 取值 | 效果 |
| --- | --- |
| `Transient`（默认） | 每次导航都新建视图实例 |
| `Scoped` | 每次导航创建一个独立作用域，视图从该作用域解析，所以**每次导航也是新实例** |
| `Singleton` | 整个应用共用一个实例，反复导航复用 |

**`RequiredParameters`** —— 导航必需参数，详见第 7 节。

**`ViewModelLifetime` + `HasViewModelLifetime`** —— 单独指定视图模型的生命周期。
**这两个必须一起用**：`ViewModelLifetime` 的默认值是 `Transient`，
特性命名参数又不能是可空枚举，所以框架用 `HasViewModelLifetime` 标记"你是否真的设了它"。
只写 `ViewModelLifetime = Singleton` 而不写 `HasViewModelLifetime = true`，**设置不生效**。

完整示例里的写法：

```csharp
[View<LifetimeSharedStateViewModel>(
    ViewName = "lifetime-shared-state",
    ViewModelLifetime = ServiceLifetime.Singleton,
    HasViewModelLifetime = true
)]
public partial class LifetimeSharedStateView : LifetimeDemoView { }
```

### 4.5 视图模型会被自动注册

只要视图模型满足两个条件，生成器会自动给它追加生命周期接口，不需要你手写
`ITransientDependency`：

1. 视图模型声明为 `partial`；
2. 构造函数的所有参数都能从容器解析——参数类型必须是接口或类，
   且不能是 `string` 或 `object`（MS DI 不会为这两个类型提供实例）。

视图模型的生命周期默认跟随视图的 `Lifetime`，也可以用 `ViewModelLifetime` 单独指定。

不满足条件时：

| 情况 | 诊断 | 级别 |
| --- | --- | --- |
| 不是 `partial`，也没声明必需参数 | `LENABP006` | 警告（不能自动注册，但可以手动注册） |
| 不是 `partial`，但声明了必需参数 | `LENABP009` | **错误**（契约生成不了，参数校验会静默失效） |
| 构造函数有不可注入的参数 | `LENABP007` | 警告 |

如果你已经手写了 `ITransientDependency` 之类的接口，生成器会跳过，尊重你的选择。

### 4.6 视图模型必须实现什么

**什么都不用实现。** 视图模型可以是任意普通类：

```csharp
// 完整示例里的 LoginViewModel，就这么多
public partial class LoginViewModel : ObservableObject { }
```

需要参与导航流程时才实现 `INavigationAware`（见 6.4），
需要保活时才实现 `IRegionMemberLifetime`（见第 9 节）。

### 4.7 视图名不能重复

两个视图用同一个 `ViewName` 会报 `LENABP003`（错误）。
导航按名字解析，重名时只会命中其中一个，另一个永远导航不到。

比较是**大小写敏感**的：`"Demo"` 与 `"demo"` 不算重复，不会报 `LENABP003`。
但导航时的键控解析同样大小写敏感，两个名字互不影响——只是容易混淆，不建议这样命名。

---

## 5. 功能二：声明区域

### 5.1 一行附加属性

在 XAML 里给控件加上 `len:RegionHost.Name`：

```xml
<Window ...
        xmlns:len="http://github.com/lenfon">
    <ContentControl len:RegionHost.Name="MainRegion" />
</Window>
```

`xmlns:len="http://github.com/lenfon"` 这个命名空间由
`Len.Abp.Wpf.Abstractions` 的程序集特性提供，不需要 `clr-namespace`。

它只映射了 `Len.Abp.Wpf.Navigation` 这一个命名空间，所以 XAML 里能用的是
`RegionHost` 这类导航类型。`Len.Abp.Wpf.Mvvm` 下的 `IView` 等类型不在其中，
需要时仍要写 `clr-namespace`。

区域在控件**加载完成后**才真正创建（监听 `Loaded` 事件），所以：

- 在 `Window` 的构造函数里立刻导航，区域还不存在；
- 要等区域宿主加载完再导航。完整示例的做法是挂到区域宿主自己的 `Loaded` 上：

  ```csharp
  public MainShell()
  {
      InitializeComponent();
      RegionHost.Loaded += OnRegionHostLoaded;
  }

  private async void OnRegionHostLoaded(object sender, RoutedEventArgs e)
  {
      RegionHost.Loaded -= OnRegionHostLoaded;
      await ViewModel.ShowGalleryCommand.ExecuteAsync(null);
  }
  ```

### 5.2 三种内置区域宿主

| 宿主控件 | 区域类型 | 行为 |
| --- | --- | --- |
| `ContentControl` | 单活动视图 | 活动视图同步到 `Content` |
| `TabControl` | 单活动视图 | 每个视图一个选项卡，`SelectedItem` 与激活双向同步 |
| `ItemsControl` | 多活动视图 | 所有视图同步到 `ItemsSource`，与激活状态无关 |

`TabControl` 继承自 `ItemsControl`，查找器从具体类型沿继承链向上匹配，
所以 `TabControl` 会命中自己的适配器，不会落到 `ItemsControl` 那个。

**`TabControl` 区域里不保活的视图，切走后会连同选项卡一起消失。**
因为它和 `ContentControl` 一样是单活动视图：切换选项卡会激活新视图、失活旧视图，
旧视图再按保活规则决定去留。想要"切走还留在选项卡里"，需要声明 `KeepAlive = true`。

**`ContentControl` 的 `Content` 必须为空。** 如果已经设了 `Content` 或绑定了它，
适配器会抛 `ArgumentException`。同理，`ItemsControl` 和 `TabControl` 的
`ItemsSource` 也不能被占用。

### 5.3 选项卡标题

`TabControl` 区域的选项卡标题默认取视图名（如 `tab-page-3`）。
想要可读标题，在视图上设置 `RegionHost.TabHeader`：

```xml
<UserControl len:RegionHost.TabHeader="登录">
```

也可以在代码里设：

```csharp
RegionHost.SetTabHeader(this, "选项卡一");
```

完整示例里 `TabPage1View` 和 `TabPage2View` 设了中文标题，
`TabPage3View` 故意不设，用来对比回退行为。

### 5.4 嵌套区域

区域可以无限层嵌套：视图内部再声明区域，这个区域就归属于这个视图实例。

```xml
<!-- DemoView.xaml -->
<UserControl ...>
    <StackPanel>
        <Button Command="{Binding ClickCommand}" Content="Click Me" />
        <ContentControl len:RegionHost.Name="DemoRegion" />
    </StackPanel>
</UserControl>
```

```csharp
// DemoViewModel 里导航到自己的子区域
[RelayCommand]
private async Task ClickAsync()
{
    Message = "Button Clicked!";
    await NavigationService.NavigateAsync("DemoRegion", "demo2", IParameters.Create());
}
```

嵌套区域的四条语义：

1. **所有权按视图实例记录**，不是按视图名。同一个视图存在多个实例时，各自的子区域互不干扰。
2. **同名区域优先命中可见的那个**。多个实例拥有同名子区域时，导航命中活动链上的实例，
   也就是用户当前看到的那个。
3. **拆除是递归的**。父视图失活且不保活时，其内部任意深度的区域子树一并摘除。
4. **容器管理的嵌套视图保留自己的区域**。区域只在视图首次加载时创建，摘掉后不会重建，
   所以单例视图的区域必须留着，供下次导航复用。

完整示例的"嵌套区域（8 层）"演示了全部四条。它的做法是八层视图共用一份 XAML，
每层在构造函数里设置自己的子区域名：

```csharp
[View<NestedRootViewModel>(ViewName = "nested-root")]
public partial class NestedRootView : NestedRegionView
{
    public NestedRootView() => Configure("嵌套区域 · 第 1 层", "NestedLevel1Region");
}
```

### 5.5 区域名可以重复吗

可以。嵌套区域天然会出现同名子区域（同一视图的多个实例各有一个）。
`RegionFinder` 在多个同名候选中优先返回"当前可见"的那个。

但**同一个元素上重复设置 `RegionHost.Name` 没有意义**：`RegionBuilder` 有幂等保护，
只保留第一次的订阅，后续设置不会创建第二个区域。

**在控件加载之后再改区域名，改动不生效。** 区域只在 `Loaded` 时按当时的名字创建一次；
加载完成后再改附加属性，既不会重建区域，也不会改掉已有区域的名字。
所以代码里 `RegionHost.SetName(...)` 必须在控件加载之前调用。
完整示例的 `NestedRegionView` 是在构造函数里设的：

```csharp
public NestedRegionView()
{
    InitializeComponent();
    NestedInstanceTracker.TrackView(this);
}

protected void Configure(string title, string? childRegionName)
{
    TitleText.Text = title;

    if (childRegionName is not null)
    {
        // 设置附加属性会触发区域构建，区域在控件加载完成后创建。
        RegionHost.SetName(ChildRegionHost, childRegionName);
    }
}
```

派生视图在**自己的构造函数**里调用 `Configure`，此时控件还没加载：

```csharp
public NestedRootView() => Configure("嵌套区域 · 第 1 层", "NestedLevel1Region");
```

### 5.6 区域宿主被卸载再加载

区域宿主从可视树摘除时，区域会从提供程序里摘除但**不拆除**；
重新加载时复用同一个区域并登记回去。这样窗口隐藏再显示、选项卡切换都不会让区域失效。

**区域名称在卸载期间被改掉**时，旧区域会被丢弃并重建。此时要求适配器实现
`IRegionAdapterDetach`（框架自带的三个都实现了），否则重建可能因控件状态残留而失败，
异常包装成 `RegionCreationException`。

---

## 6. 功能三：导航

### 6.1 注入导航服务

```csharp
public partial class MainViewModel(INavigationService navigationService) : ObservableObject
{
}
```

`INavigationService` 由框架注册为单例，直接构造函数注入即可。

### 6.2 两个导航方法

```csharp
// 带参数
await navigationService.NavigateAsync("MainRegion", "demo2", IParameters.Create("?id=42"));

// 不带参数
await navigationService.NavigateAsync("MainRegion", "demo2");
```

第一个参数是**区域名**，第二个是**视图名**（即 `[View]` 上的 `ViewName`）。
两个方法都返回 `ValueTask<INavigationResult>`，还都接受一个可选的
`CancellationToken`（见 6.5）。

返回值是 `INavigationResult`：

```csharp
var result = await navigationService.NavigateAsync("MainRegion", "demo2");

if (result.Result)
{
    // 成功
}
else
{
    // 失败，result.Exception 说明原因
}
```

**导航失败不抛异常**，而是返回 `Result == false` 的结果。

但有三种情况会**抛出**异常，需要调用方自己处理：

| 异常 | 触发条件 |
| --- | --- |
| `ArgumentException` | `viewName` 为 `null` 或空字符串（`regionName` 不校验，为空时只会返回"区域不存在"的失败结果） |
| `OperationCanceledException` | 取消令牌已取消（见 6.5） |
| `InvalidOperationException` | WPF 应用尚未启动，消息是"WPF 应用尚未启动，无法导航。" |

最后一条通常只在你绕过框架自己调导航时出现——正常流程里导航都由 UI 线程上的命令触发，
那时应用一定已经启动。

### 6.3 `INavigationResult` 的三个成员

| 成员 | 说明 |
| --- | --- |
| `Result` | 是否成功 |
| `Exception` | 失败原因；成功时为 `null` |
| `Context` | 本次导航的 `Uri` 与 `Parameters` |

**注意 `Exception` 为 `null` 有两种含义**：导航成功，或目标区域不存在。
区域不存在属于调用方配置错误，框架不构造异常。要区分时同时看 `Result`，
并查看日志——日志里会列出当前已注册的所有区域名。

### 6.4 导航生命周期回调

视图模型实现 `INavigationAware` 即可参与导航流程：

```csharp
public partial class DetailViewModel : ObservableObject, INavigationAware
{
    // 是否接受本次导航。返回 false 时导航服务会解析新实例。
    public Task<bool> IsNavigationTargetAsync(INavigationContext context) => Task.FromResult(true);

    // 视图失活之前调用
    public Task OnNavigatedFromAsync(INavigationContext context) => Task.CompletedTask;

    // 视图激活之后调用
    public Task OnNavigatedToAsync(INavigationContext context)
    {
        Id = context.Parameters.GetRequired<int>("id");
        return Task.CompletedTask;
    }
}
```

三个回调都是可选的——不实现接口就完全不参与。

**`IsNavigationTargetAsync` 的作用**：导航到一个视图时，如果区域里已经有同名视图，
框架会先问它"你接受这次导航吗"。返回 `true` 就复用现有实例，返回 `false` 就新建一个。
不实现这个接口的视图，只要视图名匹配就直接复用。

**例外：不保活的作用域视图永远不复用。** `Lifetime = Scoped` 且没声明
`KeepAlive = true` 的视图，即使实现了 `IsNavigationTargetAsync` 并返回 `true`，
每次导航仍会新建——它的语义就是"每次导航一个新实例"，复用会让作用域无法释放。

### 6.5 取消导航

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

try
{
    await navigationService.NavigateAsync("MainRegion", "demo2", IParameters.Create(), cts.Token);
}
catch (OperationCanceledException)
{
    // 导航在激活之前被取消，区域状态保持不变
}
```

**取消是唯一不会被包装成失败结果的异常。** 调用方可以据此区分"被取消"与"导航失败"。

取消只在**激活之前**生效。一旦开始通知 `OnNavigatedFrom`，界面状态已开始切换，
此时不再响应取消。

视图模型自己抛出的 `OperationCanceledException`（例如它内部的超时）
仍按导航失败处理，不会穿透到调用方。框架用
`catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)`
区分两者——只有**你传进来的令牌**被取消时才穿透。

**取消时新建的视图会被回收。** 如果导航已经解析出新视图但还没激活就被取消，
该视图会从区域移除，它的导航作用域也一并释放，不会随每次取消累积。

### 6.6 并发与重入

导航按调用顺序**串行执行**，同一时刻只有一次导航在跑。

在导航回调内部再次导航（例如切换子区域）**不会死锁**——重入直接执行，不排队。

### 6.7 导航失败时界面会怎样

框架保证"失败即回到导航前的样子"：

| 失败点 | 界面状态 |
| --- | --- |
| 区域不存在 | 不变 |
| 视图解析失败（名字拼错、忘记加 `[View]`） | 不变，旧视图不会收到 `OnNavigatedFrom` |
| 参数校验失败 | 不变 |
| 取消（激活前） | 不变 |
| `OnNavigatedToAsync` 抛异常 | 回滚到旧视图 |

最后一条的实现方式是"延迟拆除"：激活新视图会让旧视图失活，但拆除被推迟到导航确认成功之后。
失败时取消拆除，旧视图原样留在区域中。

**回滚不会重新通知旧视图 `OnNavigatedToAsync`**——它从未真正离开。
新视图也不会收到 `OnNavigatedFromAsync`：它的 `OnNavigatedToAsync` 已经失败，
视图模型没有进入"已激活"状态。

**重复导航到当前视图时失败，界面不会被清空。** 这是刻意处理的边界：
此时新旧视图是同一个实例，如果按常规回滚逻辑失活它，被拆掉的正是用户正在看的界面。

### 6.8 导航失败怎么排查

三种失败都会写日志：

| 失败点 | 日志级别 | 日志内容 |
| --- | --- | --- |
| 区域不存在 | `Warning` | 区域名 + 当前已注册的区域列表 |
| 视图解析失败 | `Error` | 视图名 + 区域名 + 原始异常 |
| 导航回调异常 | `Error` | 视图名 + 区域名 + 失败阶段 |

区域不存在时日志里的"已注册区域列表"能帮你区分"区域名拼错"和"区域根本没创建"。

---

## 7. 功能四：导航参数与必需参数校验

### 7.1 传参数

```csharp
// 从查询字符串创建
await navigationService.NavigateAsync("MainRegion", "detail", IParameters.Create("?id=42&source=region-demo"));

// 也可以不带前导的 '?'
IParameters.Create("id=42&source=region-demo");

// 空参数
IParameters.Create();
```

参数值以 `object` 存储。从查询字符串创建时值都是字符串，读取时自动转换成目标类型；
用 `Add(key, value)` 添加时可以直接放任意对象，读取时类型匹配就直接用。

**查询串按 URI 规则解码，不按表单规则。** 只处理百分号转义（`%20` → 空格），
`+` 原样保留：

```csharp
IParameters.Create("?q=hello+world")["q"];   // "hello+world"
IParameters.Create("?q=hello%20world")["q"]; // "hello world"
```

需要空格时用 `%20`。base64 之类含 `+` 的值因此可以安全地放在查询串里。

### 7.2 读参数：`GetValue` 与 `GetRequired`

```csharp
// 键不存在或转换失败时返回 default
var id = context.Parameters.GetValue<int>("id");

// 键不存在或转换失败时抛 InvalidOperationException，消息里列出可用键
var id = context.Parameters.GetRequired<int>("id");
```

**推荐用 `GetRequired<T>`**：`GetValue<T>` 在键名拼错时静默返回 `default`，
bug 会藏到很远的地方才暴露。

`GetRequired<T>` 的异常消息长这样：

```
无法获取必需参数 "id"：参数不存在。可用参数：source, name。
```

### 7.3 `IParameters` 的其他成员

| 成员 | 说明 |
| --- | --- |
| `Count` | 参数项数量 |
| `Keys` | 所有键 |
| `this[key]` | 按键取值，不存在时返回 `null` |
| `ContainsKey(key)` | 是否存在该键 |
| `GetValues<T>(key)` | 取同名键的所有值，无法转换的跳过 |
| `TryGetValue<T>(key, out value)` | 尝试读取，返回是否成功 |
| `Add(key, value)` | 追加一个参数，返回自身便于链式调用 |

**键名大小写不敏感。** `IParameters` 的键比较用 `StringComparison.OrdinalIgnoreCase`，
`?id=42` 之后用 `"Id"`、`"ID"` 都能读到。这一点与必需参数校验一致（见 7.5）。

> **仅大小写不同的键会解析到同一个条目。** `Add("Id", 1)` 与 `Add("id", 2)` 都能插入，
> 但查找只会命中先插入的那个。不要依赖这个行为区分参数。

### 7.4 声明必需参数

在 `[View]` 上声明输入契约，导航服务在**激活视图之前**校验：

```csharp
// 只声明名称：只校验存在性
[View<DetailViewModel>(ViewName = "detail", RequiredParameters = ["Id", "Source"])]

// 名称 + 类型：同时校验存在性与类型
[View<DetailViewModel>(ViewName = "detail", RequiredParameters = ["Id:int"])]

// 两种写法可以混用
[View<DetailViewModel>(ViewName = "detail", RequiredParameters = ["id:int", "source"])]
```

校验失败时 `NavigateAsync` 返回失败结果，异常消息列出缺失项与已提供的参数：

```
视图 "detail" 的参数校验失败：缺少参数 source。已提供：id。
```

**校验发生在激活之前，界面状态不变。**

### 7.5 校验的四个细节

1. **校验时大小写不敏感**。`RequiredParameters = ["Id"]` 能匹配调用方传的 `?id=42`。
   这是刻意的：参数来自查询串或调用方，大小写不该成为失败原因。
2. **可以多传**。声明的是输入契约，不是调用约束。多余的参数被忽略。
3. **类型校验复用读取时的转换规则**。校验器内部调用 `ParameterValueConverter.CanConvert`，
   它和 `IParameters.TryGetValue<T>` 走同一套 `ParameterConversion` 逻辑，
   所以不会出现"校验说能转、读取却转不了"的矛盾。
4. **类型名写错会降级**。`RequiredParameters = ["Id:Int32Typo"]` 解析不出类型时，
   退化为"只校验存在性"，并报 `LENABP014`（警告）。

> **校验与读取口径一致。** `IParameters` 的键比较同样不区分大小写（见 7.3），
> 所以声明 `["Id"]` 而调用方传 `?id=42` 时，校验通过，视图模型里
> `GetRequired<int>("Id")` 和 `GetRequired<int>("id")` 都能读到同一个值。

### 7.6 契约生成在哪里

生成器把 `RequiredParameters` 翻译成**视图模型**上的
`INavigationContract.RequiredNavigationParameters`。

为什么放在视图模型上：视图模型才是参数的消费者，而且它是普通类，
测试里直接 `new` 就能断言契约，不需要 WPF 控件和 STA 线程。

**视图模型必须声明为 `partial`**，否则报 `LENABP009`（错误级）。

手写 `IView` 实现（不经过生成器）仍然可以通过 `IView.RequiredNavigationParameters`
声明契约，校验器会回落到那里。两处都声明时**以视图模型为准**。

### 7.7 支持的类型名写法

```csharp
RequiredParameters = [
    "id:int",                        // 关键字别名
    "name:string",
    "count:long",
    "ratio:double",
    "when:DateTime",                 // 相对类型名，会依次尝试视图所在命名空间、System 命名空间
    "mode:Demo.Outer.Mode",          // 嵌套类型
    "flag:bool",
    "maybe:int?",                    // 可空值类型
    "text:string?"                   // 可空引用类型，等价于 string
]
```

---

## 8. 功能五：导航历史与回退

### 8.1 回退

```csharp
var result = await navigationService.GoBackAsync("MainRegion");

if (!result.Result)
{
    // 没有更早的历史，或回退目标已无法解析
}
```

导航历史**按区域独立记录**。`GoBackAsync` 回退到该区域的上一个视图。

### 8.2 六条行为规则

1. **只记录视图名与参数，不记录实例**。回退走与普通导航完全相同的解析路径：
   视图还在就复用，已被释放就新建。
2. **回退本身不产生新的历史条目**。否则 A→B 回退到 A 会把 A 压回栈顶，再次回退又回到 B，
   在两个视图之间来回震荡。
3. **只有完整成功的导航才记入历史**。失败或取消的导航不会出现在回退路径上。
4. **连续导航到同一视图不重复压栈**。例如刷新当前页，回退一次不会"仍在原地"。
5. **回退失败时历史栈保持原样**。只有回退成功才弹出，避免一次失败让用户再点后退时跳步。
6. **跨区域回退不支持**。区域名是导航的定位单位，父区域回退后子区域的视图可能已被拆除，
   跨区域跳转的语义不可预期。需要时由调用方自行记录路径。

### 8.3 调整历史深度

每个区域默认保留 10 条，超出丢弃最旧：

```csharp
Configure<NavigationOptions>(options => options.HistoryDepth = 20);
```

设为 0 或负数会回退到默认值 10，避免配置失误让回退完全失效。

---

## 9. 功能六：视图保活与生命周期

### 9.1 保活规则

视图失活时是否留在区域中复用，按以下优先级判断：

| 条件 | 行为 |
| --- | --- |
| 视图模型声明 `KeepAlive = true` | 视图与视图模型都保留，下次导航复用同一实例 |
| 视图或视图模型是单例 | 按 IoC 规则保留，下次导航复用容器中的实例 |
| 其余情况（含未实现 `IRegionMemberLifetime`、作用域视图） | 不保留，移出区域并释放 |

**默认不保活。** 未实现 `IRegionMemberLifetime` 等同于 `KeepAlive = false`，
避免忘记声明契约导致视图在区域里无限累积。

### 9.2 声明保活

```csharp
public partial class GalleryViewModel : ObservableObject, IRegionMemberLifetime
{
    // 需要保留状态（如日志、输入内容）时显式保活
    public bool KeepAlive => true;
}
```

完整示例里 `ControlsGalleryViewModel` 和 `RegionAdaptersViewModel` 都声明了保活，
因为它们的事件日志和操作日志需要在来回导航后保留。

### 9.3 三种生命周期的实际表现

这是最容易搞混的地方，用完整示例的"生命周期"演示页可以直接观察：

| 视图的 `Lifetime` | 每次导航 | 界面上的实例编号 |
| --- | --- | --- |
| `Transient`（默认） | 新建视图与视图模型 | 每次都变 |
| `Scoped` | 新建视图与视图模型 | **每次都变** |
| `Singleton` | 复用同一实例 | 始终不变 |

**为什么 `Scoped` 和 `Transient` 表现一样？** 因为每次导航都会创建一个独立作用域，
视图从该作用域解析，作用域用完就释放。所以 `Scoped` 视图每次导航都得到新实例，
它依赖的 `Scoped` 服务也来自同一个导航作用域，视图被拆除时作用域随之释放。

需要跨导航复用请用 `Singleton`，需要每次新建请用 `Transient`。

**`Scoped` 还有一条额外规则**：不保活的作用域视图**不参与复用**。
即使它实现了 `IsNavigationTargetAsync` 并返回 `true`，每次导航仍会新建——
复用区域中的旧实例会让作用域无法释放。要让它保活，必须显式声明 `KeepAlive = true`。

### 9.4 视图瞬态 + 视图模型单例

"界面要重建、状态要保留"的场景：

```csharp
[View<LifetimeSharedStateViewModel>(
    ViewName = "lifetime-shared-state",
    ViewModelLifetime = ServiceLifetime.Singleton,
    HasViewModelLifetime = true
)]
```

效果：视图每次导航都重建，但视图模型始终是同一个实例。
界面上的实例编号不变，"进入次数"持续累加。

### 9.5 释放规则

| 情况 | 谁负责释放 |
| --- | --- |
| 视图来自导航作用域 | 释放作用域，由作用域统一释放它创建的实例 |
| 其余非单例实例 | 逐个释放 |
| 单例 | 不释放，归容器所有 |

每个实例最多释放一次。这是防御性的：退出时按快照遍历区域，
同一视图可能先后被父区域和嵌套区域各拆一次，第二次 `Dispose` 会让
持有 `CancellationTokenSource` 的视图模型抛 `ObjectDisposedException`。

### 9.6 保活只对区域直接承载的视图生效

父视图被拆除时，它拥有的区域会先被整体摘除，其中所有视图——**包括声明了 `KeepAlive` 的**——
都随子树一起释放。原因是承载它们的区域已不可达，跳过释放只会留下既无法导航到、
也不会被回收的孤儿实例。

需要跨导航保留嵌套视图时，**把保活声明放在父视图上**，父视图保活则整棵子树都不拆除。

### 9.7 显式移除

```csharp
region.Remove(view);      // 移除并释放，无视保活声明
region.RemoveAll();       // 清空整个区域
```

`Remove` 是显式回收：视图离开区域后不会再被导航复用，所以即使它声明了保活，
其作用域与可释放资源也会被释放。

**不要用 `Deactivate` 回收。** 它的职责是失活：对从未激活的视图是空操作，
对已激活的视图按保活声明决定是否拆除。要确保回收请用 `Remove`。

`ItemsControl` 区域是多活动视图，导航不会自动失活其中的视图，
需要回收时由使用者显式调用 `Remove` 或 `RemoveAll`。

---

## 10. 功能七：生成依赖属性

### 10.1 最小用法

```csharp
using Len.Abp.Wpf;

[DependencyProperty<string>("Title", DefaultValue = "Untitled")]
public partial class MyControl : Control
{
}
```

生成器产出：

- `public static readonly DependencyProperty TitleProperty`
- `public string Title { get; set; }`（CLR 包装属性）
- 三个可选的分部钩子：`CoerceTitle`、`IsTitleValid`、`OnTitleChanged`

### 10.2 三个钩子

```csharp
[DependencyProperty<int>("Rating", DefaultValue = 0)]
public partial class RatingControl : Control
{
    // 校验：返回 false 时 WPF 抛 ArgumentException
    static partial void IsRatingValid(int value, ref bool result) => result = value is >= 0 and <= 100;

    // 强制值：把值收敛到合法范围
    static partial void CoerceRating(int value, ref int result) => result = Math.Clamp(value, 0, 5);

    // 变更通知
    partial void OnRatingChanged(int oldValue, int newValue) { }
}
```

执行顺序是**校验 → 强制值 → 变更通知**。校验在强制值之前，
所以校验钩子只该拦截"明显非法"的值，把"超出上限"交给强制值处理。

**强制值钩子必须显式写入 `result`**：生成的回调会先给 `result` 赋默认值再调用你的方法。

### 10.3 全部参数

| 参数 | 说明 |
| --- | --- |
| `DefaultValue` | 默认值。泛型特性上类型是 `T?` |
| `DefaultValueExpression` | 默认值表达式，用于非编译期常量，如 `"new global::System.Windows.Thickness(2)"` |
| `Category` | 分类，生成 `[Category]` 特性，显示在属性面板 |
| `Description` | 描述，写进 CLR 包装属性的 XML 文档注释 |
| `Flags` | `FrameworkPropertyMetadataOptions` 标志 |
| `UpdateSourceTrigger` | 更新源触发器 |
| `IsReadOnly` | 注册为只读依赖属性 |

`Description` 只影响文档注释，不会生成 `[Description]` 特性，也不会显示在属性面板里。
要在属性面板显示说明，需要自己给 CLR 属性加 `[Description]`。

**`DefaultValueExpression` 优先于 `DefaultValue`。** 空白字符串按"未指定"处理。

**`UpdateSourceTrigger` 只在显式指定非 `Default` 值时才写进元数据。**
显式传 `UpdateSourceTrigger.Default` 会让 WPF 抛"这样会创建循环定义"的异常。

**`Flags` 只要非空就会走 `FrameworkPropertyMetadata`。** 只写 `Flags = None` 会回退到普通
`PropertyMetadata`。

### 10.4 只读依赖属性

```csharp
[DependencyProperty<string>(
    "DisplayText",
    DefaultValue = "未评分",
    IsReadOnly = true)]
public partial class RatingControl : Control { }
```

生成器改用 `RegisterReadOnly`，产出：

- `public static readonly DisplayTextProperty`
- `protected internal static readonly DisplayTextPropertyKey`
- CLR 属性的 setter 为 `protected`

控件内部通过 `SetValue(DisplayTextPropertyKey, value)` 写入。
外部调用 `SetValue(DisplayTextProperty, ...)` 会抛 `InvalidOperationException`，
只读约束由 WPF 依赖属性系统保证。

密封类上键字段与 setter 都降级为 `public`（密封类不能声明新的保护成员）。

### 10.5 默认值的类型必须精确匹配

WPF 在注册依赖属性时会检查"默认值类型与属性类型是否一致"，不一致直接抛异常。
生成器已经处理了各种字面量后缀，你按正常写法写就行：

```csharp
[DependencyProperty<double>("Ratio", DefaultValue = 1.0)]     // 生成 1D
[DependencyProperty<float>("Scale", DefaultValue = 0.5f)]     // 生成 (float)(0.5)
[DependencyProperty<byte>("Level", DefaultValue = 3)]         // 生成 (byte)(3)
[DependencyProperty<uint>("Mask", DefaultValue = 7)]          // 生成 7U
[DependencyProperty<MyEnum>("Mode", DefaultValue = MyEnum.B)] // 生成 (MyEnum)(MyEnum.B)
```

（这些是生成器内部产出的表达式形态，你不需要手写。列在这里是为了让你在
"默认值类型不匹配"的异常里能认出生成器已经做了什么。）

### 10.6 宿主类型限制

非附加的依赖属性生成**实例成员**，所以宿主不能是静态类，否则报 `LENABP013`（错误）。
需要静态宿主请用附加版本（第 11 节）。

---

## 11. 功能八：生成附加依赖属性

### 11.1 最小用法

```csharp
[AttachedDependencyProperty<int>("MaxTagLength", DefaultValue = 12)]
public partial class TagInputAssist : DependencyObject
{
    static partial void IsMaxTagLengthValid(int value, ref bool result) => result = value is >= 1 and <= 64;
}
```

生成器产出：

- `public static readonly MaxTagLengthProperty`
- `public static int GetMaxTagLength(DependencyObject element)`
- `public static void SetMaxTagLength(DependencyObject element, int value)`
- 三个静态分部钩子：`CoerceMaxTagLength`、`IsMaxTagLengthValid`、`OnMaxTagLengthChanged`

### 11.2 宿主必须派生自 `DependencyObject`

```csharp
public partial class TagInputAssist : DependencyObject { }   // 正确
public static partial class TagInputAssist { }               // 错误：附加属性不行
```

生成器用 `DependencyProperty.Register` 注册附加属性（不是 `RegisterAttached`），
而 `Register` 要求 `ownerType` 派生自 `DependencyObject`。静态类宿主能编译通过，
但类型初始化时抛 `ArgumentException: "Xxx" 类型必须派生自 DependencyObject`。
**附加路由事件**的宿主可以是静态类（第 13 节）。

### 11.3 钩子比普通属性多一个参数

附加属性的钩子是静态方法，且多一个 `DependencyObject element` 参数：

```csharp
static partial void CoerceMaxTagLength(DependencyObject element, int value, ref int result);
static partial void IsMaxTagLengthValid(int value, ref bool result);   // 这个没有 element
static partial void OnMaxTagLengthChanged(DependencyObject element, int oldValue, int newValue);
```

### 11.4 只读附加属性

```csharp
[AttachedDependencyProperty<int>("TagCount", DefaultValue = 0, IsReadOnly = true)]
public partial class TagInputAssist : DependencyObject { }
```

`SetTagCount` 为 `protected`（静态类或密封类宿主上退化为 `private`），
外部只能通过 `GetTagCount` 读取。键字段 `TagCountPropertyKey` 的可见性同理。

**在 `ControlTemplate` 里绑定只读附加属性时注意**：`StringFormat` 与 `Mode=OneWay`
组合会让绑定取不到值，应直接写：

```xml
{Binding Path=(wc:TagInputAssist.TagCount), RelativeSource={RelativeSource TemplatedParent}}
```

### 11.5 集合类型的默认值不能放进元数据

元数据里的默认值由所有实例共享，集合放进去会在不同控件之间串数据。
正确做法是按实例创建：

```csharp
public TagInput()
{
    TagInputAssist.SetTags(this, []);
}
```

完整示例的 `TagInput` 就是这么做的。

---

## 12. 功能九：生成路由事件

### 12.1 最小用法

```csharp
[RoutedEvent("Clicked", RoutingStrategy.Bubble)]
public partial class MyControl : Control { }
```

生成器产出：

- `public static readonly RoutedEvent ClickedEvent`
- `public event RoutedEventHandler Clicked`（事件访问器）
- `protected void RaiseClickedEvent(RoutedEventArgs args)`

### 12.2 三种路由策略

```csharp
[RoutedEvent("RatingChanged", RoutingStrategy.Bubble)]    // 冒泡：从源向上
[RoutedEvent("PreviewRatingChanged", RoutingStrategy.Tunnel)]  // 隧道：从根向下
[RoutedEvent("RatingCommitted", RoutingStrategy.Direct)]  // 直接：只在源上触发
```

完整示例的 `RatingControl` 同时声明了三种，评分变化时依次触发，
可以在事件日志里看到顺序。

### 12.3 自定义事件参数

用泛型重载指定委托类型，生成器从委托签名推断事件参数类型：

```csharp
public delegate void RatingChangedEventHandler(object sender, RatingChangedEventArgs e);

[RoutedEvent<RatingChangedEventHandler>("RatingChanged", RoutingStrategy.Bubble)]
public partial class RatingControl : UserControl { }
```

非泛型重载也可以显式传委托类型：

```csharp
[RoutedEvent("PreviewRatingChanged", RoutingStrategy.Tunnel, typeof(RatingChangedEventHandler))]
```

**委托必须至少有两个参数**（`sender` 与事件参数），且第二个参数必须派生自
`System.Windows.RoutedEventArgs`，否则报 `LENABP004`（错误）。

**不指定委托时默认用 `RoutedEventHandler`。** 如果你的处理器签名用了自定义事件参数，
必须用泛型重载或显式传 `typeof(...)`，否则 XAML 会报"无法从文本创建事件"。

### 12.4 宿主类型限制

非附加的路由事件生成实例成员，宿主不能是静态类，否则报 `LENABP013`（错误）。

---

## 13. 功能十：生成附加路由事件

### 13.1 最小用法

```csharp
[AttachedRoutedEvent<TagAddedEventHandler>("TagAdded", RoutingStrategy.Bubble)]
public partial class TagInputAssist : DependencyObject { }
```

生成器产出三个静态方法：

- `public static void AddTagAddedHandler(DependencyObject d, TagAddedEventHandler handler)`
- `public static void RemoveTagAddedHandler(DependencyObject d, TagAddedEventHandler handler)`
- `public static void RaiseTagAddedEvent(DependencyObject d, TagEventArgs? args = default)`

参数类型是 `DependencyObject`，但三个方法内部都会先判断 `d is UIElement`，
不是 `UIElement` 时**静默返回**。路由事件只能挂在 `UIElement` 上，
把参数放宽到 `DependencyObject` 是为了让调用点不必先做类型转换。

### 13.2 事件源要自己接线

附加路由事件只提供上面三个静态方法，**不会自动触发**。你需要在合适的时机调用 `Raise`：

```csharp
// 完整示例 TagInput 里的做法
if (tag.Length > TagInputAssist.GetMaxTagLength(this))
{
    TagInputAssist.RaiseTagRejectedEvent(this, new TagEventArgs(tag));
    return;
}

tags.Add(tag);
TagInputAssist.RaiseTagAddedEvent(this, new TagEventArgs(tag));
```

### 13.3 宿主可以是静态类

附加路由事件只生成静态方法，所以宿主是静态类完全合法：

```csharp
[AttachedRoutedEvent<TagAddedEventHandler>("TagAdded", RoutingStrategy.Bubble)]
public static partial class TagInputAssist { }
```

但如果你在同一个类上还声明了附加依赖属性，就必须派生自 `DependencyObject`
（原因见 11.2）。

### 13.4 `Raise` 的 `args` 不能省略

生成的 `Raise` 方法签名是 `RaiseXxxEvent(DependencyObject d, XxxEventArgs? args = default)`，
`args` 有默认值但**不能真的省略**：方法内部会先 `ThrowIfNull(args)`，
传 `null` 或省略都会抛 `ArgumentNullException`。

生成器不会自动 `new()` 一个事件参数：WPF 的事件参数类型大多没有公共无参构造
（`MouseButtonEventArgs`、`KeyEventArgs` 等由 WPF 内部构造并传入），
自动 `new()` 会让所有实用场景编译失败。

### 13.5 在 XAML 里订阅

附加路由事件可以直接在 XAML 里写：

```xml
<wc:TagInput
    wc:TagInputAssist.TagAdded="OnTagAdded"
    wc:TagInputAssist.TagRejected="OnTagRejected" />
```

处理器的签名要与委托一致：

```csharp
private void OnTagAdded(object sender, TagEventArgs e) => ViewModel.Log($"TagAdded 标签 = {e.Tag}");
```

---

## 14. 功能十一：宿主配置

### 14.1 框架替你做了什么

应用模块依赖 `LenAbpWpfModule` 后，框架在初始化时启动两个后台工作器：

1. **单实例检测**（`SingleInstanceWpfBackgroundWorker`）
2. **WPF UI 线程宿主**（`WpfBackgroundWorker`）

UI 线程宿主会在独立的 STA 线程上创建 `Application`、合并模块资源、解析并显示所有
`IShell` 窗口，然后进入消息循环。**你不需要写 `App.xaml`，也不需要写 `app.Run()`。**
仓库里两个示例都没有 `App.xaml`，可以直接对照。

启动顺序是：先完成同步准备（资源合并、Shell 解析），再发出"启动成功"信号，
最后进入阻塞的消息循环。这样准备阶段失败时，异常能通过 `StartAsync` 返回的任务
传播给调用方，而不是让界面永远不出现还没有日志。

### 14.2 `WpfOptions` 四个选项

```csharp
public override void ConfigureServices(ServiceConfigurationContext context)
{
    Configure<WpfOptions>(options =>
    {
        options.SingleInstance = true;
        options.HandleUnhandledExceptions = true;
    });
}
```

| 选项 | 默认值 | 说明 |
| --- | --- | --- |
| `IsLifetimeLinked` | `true` | UI 应用停止时是否同时停止宿主 |
| `ShutdownMode` | `OnLastWindowClose` | WPF 关闭模式 |
| `SingleInstance` | `false` | 是否只允许单个实例 |
| `HandleUnhandledExceptions` | `false` | UI 线程未处理异常是否标记为已处理 |

### 14.3 单实例模式

`SingleInstance = true` 时：

- 第一个实例正常启动，并开始监听后续实例的激活请求；
- 再次启动时，新实例检测到已有实例，**请求它激活窗口，然后自己退出**；
- 已有实例收到请求后，把最小化的窗口还原、隐藏的窗口显示、然后置前。

单实例判定用命名互斥量，激活请求用命名事件，名称带 `Local\` 前缀，
作用域限于当前登录会话。应用名取自宿主的 `IHostEnvironment.ApplicationName`。

**验证界面改动时注意**：单实例模式下新启动的进程会直接退出，
你看到的窗口可能属于残留的旧进程。先确认没有残留进程再启动。

### 14.4 Shell 窗口

`IsShell = true` 的视图会被注册为单例，并在启动时全部 `Show()`。
可以有多个 Shell 窗口，它们都会被显示。

**Shell 必须派生自 `Window`。** 框架在准备阶段执行
`GetServices<IShell>().Cast<Window>()`，任何一个 Shell 不是 `Window` 都会抛
`InvalidCastException`，启动直接失败。这个异常会通过启动任务传播给宿主，
不会静默——但错误信息只提到类型转换，容易看漏真正的原因。

### 14.5 退出流程

应用退出时：

1. 先拆除所有区域并释放视图；
2. 再按 `IsLifetimeLinked` 决定是否停止宿主。

先拆区域再停宿主，是因为视图的 `Dispose` 可能依赖容器仍然可用。

---

## 15. 功能十二：全局异常处理

### 15.1 默认行为

框架默认只把异常写进日志，并按 `HandleUnhandledExceptions` 决定是否让进程继续运行：

| 配置 | UI 线程未处理异常 |
| --- | --- |
| `HandleUnhandledExceptions = false`（默认） | 保持 WPF 原有的崩溃行为 |
| `HandleUnhandledExceptions = true` | 异常被记录后进程继续运行 |

**后台任务的未观察异常无论该开关如何都会被记录并标记为已观察。**
因为未观察异常默认只在 GC 时静默丢弃，既不崩溃也不留线索。

### 15.2 替换成自己的实现

需要弹窗、上报或按环境区分行为时，实现 `IWpfExceptionHandler`：

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

**必须同时实现 `ISingletonDependency`。** ABP 的约定注册只处理带生命周期标记的类型，
仅靠 `[Dependency]` + `[ExposeServices]` 不会注册。

标记已观察由框架完成，实现方不必处理 `SetObserved`。

**返回值决定进程去留**：`HandleDispatcherException` 返回 `true` 时进程继续运行，
返回 `false` 时保持 WPF 原有的崩溃行为。注意这个返回值**覆盖**了
`WpfOptions.HandleUnhandledExceptions`——一旦你替换了实现，那个选项就不再起作用。

### 15.3 完整示例的做法

`samples/advanced/WpfApp/WpfAppExceptionHandler.cs` 在默认日志之外，
把异常写进界面可见的列表并弹窗。注意它只在 UI 线程异常时弹窗——
后台任务的未观察异常可能来自任意线程，在非 UI 线程上创建窗口会抛异常。

### 15.4 怎么在界面上触发一次

完整示例的「区域适配器与导航」页有个「在 UI 线程抛异常」按钮。它的实现方式值得注意：

```csharp
Application.Current.Dispatcher.BeginInvoke(
    new Action(() => throw new InvalidOperationException("这是示例抛出的未处理异常。"))
);
```

**不能直接抛**：直接抛会被命令执行链捕获，走不到 `DispatcherUnhandledException`。

---

## 16. 功能十三：自定义区域适配器与区域行为

### 16.1 什么时候需要自定义适配器

内置适配器只覆盖 `ContentControl`、`TabControl`、`ItemsControl`。
需要别的宿主控件（例如 `ListBox`、`TreeView`）时，继承 `BasicRegionAdapter<T>`：

```csharp
[ExposeKeyedService<IRegionAdapter>(typeof(ListBox))]
public class ListBoxRegionAdapter(IAbpLazyServiceProvider abpLazyServiceProvider)
    : BasicRegionAdapter<ListBox>(abpLazyServiceProvider), ISingletonDependency
{
    protected override void Adapt(IRegion region, ListBox regionTarget)
    {
        // 把区域内容同步到控件
        regionTarget.ItemsSource = region.Views;
    }

    protected override IRegion CreateRegion(string regionName, IView ownerView) =>
        new SingleActiveRegion(regionName, ownerView.ViewName, ownerView);
}
```

只需要实现两个方法：`Adapt`（怎么把视图显示到控件上）和 `CreateRegion`
（创建单活动还是多活动区域）。

上面的例子是最小形态，它有两个问题，实际写的时候要补上：

1. `ItemsSource` 已被占用时没有守卫，会静默覆盖使用者的数据源；
2. 没有登记清理动作，区域被丢弃后订阅还在（见 16.3）。

完整示例的 `samples/advanced/WpfApp/Regions/ListBoxRegionAdapter.cs` 补上了第 1 条
（`ItemsSource` 守卫）并实现了选中与激活的双向同步，但订阅仍用内联 lambda，
没有登记清理动作——它的区域在演示里从不被丢弃，所以问题不会暴露。
自己写适配器时请按 16.3 补上第 2 条。

### 16.2 注册方式

用 `[ExposeKeyedService<IRegionAdapter>(typeof(控件类型))]` 按键控方式注册。
`RegionAdapterFinder` 从具体类型沿继承链向上查找，所以 `ListBox` 会命中你的适配器，
而 `ItemsControl` 仍走内置的那个。

适配器必须是单例（`ISingletonDependency`）：一个实例会服务多个同类型控件。

### 16.3 适配器是单例，订阅要按区域记录

因为一个适配器实例服务多个控件，订阅和清理动作**不能放在实例字段上**，
否则后一个区域的订阅会覆盖前一个。基类提供了 `OnDetach` 登记清理动作：

```csharp
protected override void Adapt(IRegion region, ListBox regionTarget)
{
    void OnSelectionChanged(object? sender, SelectionChangedEventArgs e) { /* ... */ }

    regionTarget.SelectionChanged += OnSelectionChanged;

    // 区域被丢弃时解除订阅
    OnDetach(region, () => regionTarget.SelectionChanged -= OnSelectionChanged);
}
```

**必须登记清理动作。** 不解订阅会让旧区域继续驱动控件，两者都无法回收。

### 16.4 解除适配（可选能力）

适配器在初始化时会把控件状态单向指向区域内容。区域被丢弃后这些引用还在，
如果之后要重建区域（例如区域名改了），适配器的守卫会判定"控件状态已被占用"并抛异常。

实现 `IRegionAdapterDetach` 可以让适配器解除占用：

```csharp
protected override void Detach(IRegion region, ListBox regionTarget)
{
    base.Detach(region, regionTarget);   // 建议调用

    if (ReferenceEquals(regionTarget.ItemsSource, region.Views))
    {
        regionTarget.ItemsSource = null;
    }
}
```

`BasicRegionAdapter<T>` 已经实现了这个接口，你只需要重写 `Detach` 清掉自己写入的控件状态。
即使漏调 `base.Detach`，订阅清理也不会丢——基类的显式接口实现会先执行清理动作。

### 16.5 自定义区域行为

区域行为用于扩展区域语义（注册、保活、日志等）。以键控方式注册后，
`LenAbpWpfModule` 会在 `PostConfigureServices` 扫描到它，之后**每个新建区域都会自动附加**：

```csharp
[ExposeKeyedService<IRegionBehavior>(nameof(MyRegionBehavior))]
public class MyRegionBehavior(IMyLog log) : IRegionBehavior, ITransientDependency
{
    public IRegion Region { get; set; } = default!;

    public void Attach() => Region.ActiveViews.CollectionChanged += OnActiveViewsChanged;
}
```

`Attach` 在区域创建时调用，此时 `Region` 已赋值。

完整示例的 `RegionActivityBehavior` 就是这么写的，它把每次激活与失活写进操作日志，
页面底部那块黑色日志就是它的输出。

### 16.6 框架自带的两个行为

| 行为 | 作用 |
| --- | --- |
| `RegionProviderRegistrationBehavior` | 区域创建后登记到 `IRegionProvider`，导航才能按名字找到它 |
| `RegionMemberLifetimeBehavior` | 按保活规则决定视图失活后是否拆除 |

这两个也是以键控方式注册的，走同一条自动收集路径。

**不要重复注册同名的行为键。** `RegionOptions.AddBehaviorKey` 遇到重复键会抛
`InvalidOperationException`。框架在收集时已经按 `Distinct` 去重，
但如果你自己再往 `RegionOptions` 里加同一个键，就会撞上这个异常。

---

## 17. 功能十四：资源字典合并

### 17.1 在模块里声明

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

`UriResourceDictionary` 是用 URI 字符串构造的 `ResourceDictionary`，省掉手写 `Source`。

### 17.2 合并时机

框架在 UI 线程准备阶段遍历所有模块，对每个 `AbpWpfModule` 调用 `ConfigureResources`，
把结果合并进 `Application.Resources.MergedDictionaries`。**早于 Shell 窗口显示**，
所以窗口里的样式引用能正常解析。

### 17.3 控件库的推荐组织方式

完整示例的 `WpfApp.Controls` 把每个控件的样式拆成独立文件，由一个入口文件合并：

```
Themes/
  RatingControl.xaml    RatingControl 的默认样式
  TagInput.xaml         TagInput 的默认样式
  Default.xaml          合并上面两个文件
```

模块只加载 `Default.xaml` 这一个入口，新增控件时改 `Default.xaml` 即可，不用改模块。

### 17.4 让 XAML 认识你的控件

在控件库的程序集上声明 XML 命名空间：

```csharp
[assembly: XmlnsPrefix("http://github.com/lenfon/wpfapp/controls", "wc")]
[assembly: XmlnsDefinition("http://github.com/lenfon/wpfapp/controls", "WpfApp.Controls")]
```

引用该程序集后，XAML 里用 URL 前缀即可访问控件，不需要 `clr-namespace`：

```xml
xmlns:wc="http://github.com/lenfon/wpfapp/controls"

<wc:RatingControl Rating="{Binding Rating, Mode=TwoWay}" />
```

---

## 18. 功能十五：枚举名称解析

### 18.1 用法

```csharp
using Len.Abp.Core;

public enum OrderStatus { Pending, Completed }

foreach (var item in EnumHelper.GetNameValues<OrderStatus>())
{
    Console.WriteLine($"{item.Value}: {item.Name}");
}
```

`NameValue<TEnum>` 来自 `Volo.Abp`。`Name` 由 ABP 的 `IAbpEnumLocalizer` 解析，
按以下顺序查本地化资源，全部未命中时回退到成员名：

1. `Enum:{类型名}.{成员名}`
2. `{类型名}.{成员名}`
3. `{成员名}`

**不需要配置任何资源即可直接使用**，得到的名称就是成员名。

`EnumHelper` 只有 `GetNameValues<TEnum>()` 一个方法，返回全部枚举项。
要取单个值的名称，注入 `IEnumResolver<TEnum>` 用 `GetNameValue(value)`：

```csharp
public class MyService(IEnumResolver<OrderStatus> resolver)
{
    public string Describe(OrderStatus status) => resolver.GetNameValue(status).Name;
}
```

`IEnumResolver<>` 由 `LenAbpCoreModule` 以开放泛型注册，直接注入即可。

### 18.2 三个注意点

1. **要显示中文需要提供本地化资源**，而不是给枚举成员加 `[Display]`——
   ABP 的枚举本地化器不读取 `DisplayAttribute`。
2. **必须先加载 `LenAbpCoreModule`**（或依赖它的模块）。`EnumHelper` 通过静态
   `AbpServiceProvider` 取解析器，未初始化时抛 `InvalidOperationException`。
3. **未定义的值与 `[Flags]` 组合值**拿不到单一成员名，回退到 `ToString()`：
   前者得到数字字符串，后者得到逗号分隔的成员名。

### 18.3 集合遍历扩展

```csharp
using Len.Abp.Core.Extensions;

var names = new[] { "a", "b" };
names.ForEach(Console.WriteLine);
```

这是 C# 14 的扩展成员语法，调用形式与实例方法一致。
它不替代 LINQ——需要返回值或链式操作时仍用 `Select` / `Where`。

---

## 19. 诊断速查表

生成器的问题用 `LENABP` 前缀的专用诊断报告，位置指向源码，可跳转、可检索。

| ID | 级别 | 触发条件 | 怎么修 |
| --- | --- | --- | --- |
| `LENABP001` | 错误 | 视图有构造函数但没有无参版本 | 补一个无参构造函数 |
| `LENABP002` | 错误 | 类型自身或外层类型未声明 `partial` | 加 `partial` 修饰符 |
| `LENABP003` | 错误 | 多个视图使用相同 `ViewName` | 改成不同的名字 |
| `LENABP004` | 错误 | 特性参数非法 | 见下方说明 |
| `LENABP005` | 错误 | 生成器内部异常 | 这是框架 bug，请反馈 |
| `LENABP006` | 警告 | 视图模型未声明 `partial`，无法自动注册 | 加 `partial`，或手动注册 |
| `LENABP007` | 警告 | 视图模型构造函数含不可注入参数 | 改用工厂注册，或把参数改为从导航参数读取 |
| `LENABP008` | 错误 | 视图类型是泛型 | 改为非泛型，或手写注册 |
| `LENABP009` | 错误 | 视图模型声明了 `RequiredParameters` 但不是 `partial` | 加 `partial`，或手动实现 `INavigationContract` |
| `LENABP010` | 错误 | 视图模型是值类型 | 改为 `class` 或 `record` |
| `LENABP011` | 错误 | 视图模型是开放泛型 | 指定闭合的类型实参 |
| `LENABP012` | 错误 | 视图的宿主类型是静态类或结构体 | 改为非静态的类或 `record` |
| `LENABP013` | 错误 | 非附加依赖属性/路由事件的宿主是静态类 | 改用附加版本，或把宿主改为非静态类 |
| `LENABP014` | 警告 | `RequiredParameters` 里的类型名无法解析 | 检查拼写，或改用完全限定名 |
| `LENABP015` | 警告 | 视图不在 `Views` 目录下 | 把视图移入 `Views` 目录 |
| `LENABP016` | 警告 | 视图模型不在 `ViewModels` 目录下 | 把视图模型移入 `ViewModels` 目录 |

### 19.1 `LENABP004` 覆盖哪些情况

- 名称为空或全是空白；
- 名称不是合法的 C# 标识符（含空格、引号、连字符、数字开头）；
- 名称是 C# 关键字（如 `class`、`int`）；
- 同一类型上名称重复（**仅大小写不同也算重复**）；
- 实例版本与附加版本用了同一个名称（支撑字段同名，无法共存）；
- 同一个类上同时应用了 `[View]` 与 `[View<T>]`；
- 路由事件的处理程序类型不是委托；
- 路由事件的委托参数少于两个；
- 路由事件委托的第二个参数不派生自 `RoutedEventArgs`。

### 19.2 诊断能抑制吗

能。生成器诊断与普通编译器诊断一样受 `#pragma warning disable LENABP00x`
与 `<NoWarn>LENABP00x</NoWarn>` 控制，错误级与警告级没有区别。

不过这些诊断都指向真实问题：`LENABP001` 意味着生成的代码无法编译，
`LENABP006` 意味着视图模型不会被自动注册，`LENABP014` 意味着参数类型校验静默失效。
抑制之前先确认你知道后果。

---

## 20. 常见问题

### 20.1 加了 `[View]` 但什么都没生成

按顺序检查：

1. **生成器项目有没有被引用？** 从源码引用时，`Len.Abp.Wpf.csproj` 里的分析器引用
   **不会传递**，使用方必须自己再引 `Len.Abp.Analyzer` 与 `Len.Abp.Analyzer.Wpf`
   两个项目（见 3.1）。
2. **视图类写了 `partial` 吗？** 没写会报 `LENABP002`。
3. **外层类型写了 `partial` 吗？** 嵌套视图时外层也要写。
4. **有没有编译错误？** 生成器在参数非法时会跳过生成，先看错误列表。

### 20.2 导航返回失败，但 `Exception` 是 `null`

说明**目标区域不存在**。这是刻意的设计：区域不存在属于调用方配置错误，
框架不构造异常。检查：

1. 区域名拼对了吗？
2. 区域宿主加载了吗？区域在 `Loaded` 之后才创建。
3. 看日志里的"当前已注册的区域"列表，确认区域到底有没有创建。

### 20.3 导航到视图失败，异常说"无法解析视图"

```
无法解析视图 "demo2"（区域 "MainRegion"）。请确认该视图已标注 [View] 且 ViewName 正确。
```

三种可能：

1. `ViewName` 拼错了；
2. 忘记加 `[View]` 特性；
3. 视图没被注册（生成器没运行，见 20.1）。

原始容器异常保留在 `InnerException` 里。完整示例的 `Describe` 方法会把整条异常链
拼起来显示，所以界面上看到的是"外层消息 → 内层消息"，比只看外层更容易定位。

### 20.4 视图每次导航都重建，状态丢了

默认不保活。在视图模型上实现 `IRegionMemberLifetime` 并返回 `KeepAlive => true`。

### 20.5 视图是单例，但想让视图模型每次新建

**做不到。** 单例视图只构造一次，视图模型在构造时注入并被视图持有，
之后不会再解析。要每次新建，请把视图也改成 `Transient`（默认值）。

反过来是可行的：视图瞬态、视图模型单例（见 9.4），适合"界面重建、状态保留"。

### 20.6 设了 `ViewModelLifetime` 但没生效

**漏了 `HasViewModelLifetime = true`。** 见 4.4。

### 20.7 选项卡标题显示的是 `tab-page-3` 这种英文

没设 `RegionHost.TabHeader`。在视图上设置即可（见 5.3）。

### 20.8 `ContentControl` 区域报"Content 属性不为空"

`ContentControl` 的 `Content` 必须为空，适配器才能接管它。
检查 XAML 里有没有给这个 `ContentControl` 写内容或绑定 `Content`。

### 20.9 `ItemsControl` 区域报"ItemsSource 已被占用"

同理，`ItemsSource` 不能被占用。`TabControl` 也一样。

### 20.10 区域名改了之后界面报 `RegionCreationException`

区域宿主在卸载期间改名，会触发区域重建。重建要求适配器实现 `IRegionAdapterDetach`。
框架自带的三个适配器都实现了；自定义适配器需要自己实现（见 16.4）。

### 20.11 构建报 `CS2015: 是二进制文件而非文本文件`

有文件加密或 DLP 代理拦截了 `obj` 下的生成文件。清理 `obj`/`bin` 后重试，
或用 `-m:1` 串行构建。

### 20.12 改了代码但界面没变

单实例模式下，新启动的进程会直接退出，你看到的窗口可能属于残留的旧进程。
先确认没有残留进程再启动。另外 Host 输出目录里的依赖 DLL 可能陈旧，
必要时清 `obj`/`bin` 全量重建。

### 20.13 在 `Window` 构造函数里导航失败

区域在控件 `Loaded` 之后才创建。`Window.Loaded` 先于子元素触发，
所以要把导航挂到**区域宿主自己的** `Loaded` 上（见 5.1）。

### 20.14 后台任务的异常没有任何痕迹

框架已经把 `TaskScheduler.UnobservedTaskException` 接上了，异常会被记录并标记为已观察。
如果日志里也没有，检查日志配置是否在宿主构建之前就绪。

---

## 21. 附录：公开 API 一览

### 21.1 生成器特性（`Len.Abp.Wpf.Abstractions`）

| 特性 | 用途 | 宿主要求 |
| --- | --- | --- |
| `ViewAttribute` / `<T>` | 视图契约、键控注册、视图模型自动注册 | 非静态、非结构体、非泛型类 |
| `DependencyPropertyAttribute` / `<T>` | 依赖属性 | 非静态类 |
| `AttachedDependencyPropertyAttribute` / `<T>` | 附加依赖属性 | 派生自 `DependencyObject` |
| `RoutedEventAttribute` / `<T>` | 路由事件 | 非静态类 |
| `AttachedRoutedEventAttribute` / `<T>` | 附加路由事件 | 任意类（含静态类） |

### 21.2 导航（`Len.Abp.Wpf.Navigation`）

| 类型 | 所在包 | 说明 |
| --- | --- | --- |
| `INavigationService` | Abstractions | 两个 `NavigateAsync` 重载 + `GoBackAsync` |
| `INavigationContext` | Abstractions | 本次导航的 `Uri` 与 `Parameters` |
| `INavigationResult` | Abstractions | `Result`、`Exception`、`Context` |
| `INavigationAware` | Abstractions | 三个导航生命周期回调 |
| `INavigationContract` | Abstractions | 必需参数契约（生成到视图模型上） |
| `RegionHost` | Abstractions | XAML 附加属性：`Name` 声明区域，`TabHeader` 声明选项卡标题 |
| `NavigationOptions` | Wpf | `HistoryDepth` |
| `IViewScopeRegistry` | Wpf | 视图与导航作用域的关联表 |

> `NavigationService` 实现类上还有一个接收 `Uri` 的 `NavigateAsync` 重载，
> 但它不在 `INavigationService` 接口上。通过接口调用时只有两个重载可用。

### 21.3 区域（`Len.Abp.Wpf.Navigation.Regions`）

| 类型 | 所在包 | 说明 |
| --- | --- | --- |
| `IRegionBuilder` | Abstractions | 为控件创建区域 |
| `IRegionMemberLifetime` | Abstractions | 视图保活声明 |
| `IRegion` | Wpf | 区域：`Views`、`ActiveViews`、`Activate`、`Deactivate`、`Add`、`Remove`、`RemoveAll` |
| `IRegionProvider` | Wpf | 所有已创建区域的登记表 |
| `IRegionFinder` | Wpf | 按名字查区域 |
| `IRegionAdapter` | Wpf | 区域适配器契约 |
| `IRegionAdapterDetach` | Wpf | 可选：解除适配器对控件的占用 |
| `IRegionAdapterFinder` | Wpf | 按控件类型查适配器 |
| `IRegionBehavior` | Wpf | 区域行为契约 |
| `IRegionBehaviorCollection` | Wpf | 区域行为集合 |
| `IRegionTeardownBehavior` | Wpf（internal） | 拆除契约，供 `RemoveAll` 转交 |
| `IViewsCollection` | Wpf | 视图集合 |
| `RegionOptions` | Wpf | 自动附加的行为键 |
| `BasicRegionAdapter<T>` | Wpf | 适配器基类 |
| `Region` / `SingleActiveRegion` | Wpf | 区域实现（多活动 / 单活动） |
| `RegionCreationException` | Wpf | 区域创建失败 |

### 21.4 参数（`Len.Abp.Wpf`）

| 类型 | 说明 |
| --- | --- |
| `IParameters` | 导航参数集合，`Create` 从查询串构造 |
| `ParameterValueConverter` | 判断原始值能否转换为目标类型 |

### 21.5 宿主

| 类型 | 命名空间 | 说明 |
| --- | --- | --- |
| `WpfOptions` | `Len.Abp.Wpf` | 四个宿主选项 |
| `IWpfExceptionHandler` | `Len.Abp.Wpf` | 全局异常处理策略 |
| `IShell` | `Len.Abp.Wpf` | 主窗口标记 |
| `AbpWpfModule` | `Volo.Abp.Modularity` | 支持资源合并的模块基类 |
| `UriResourceDictionary` | `System.Windows` | 用 URI 构造的资源字典 |
| `LenAbpWpfModule` | `Len.Abp.Wpf` | WPF 运行时模块 |
| `LenAbpWpfAbstractionsModule` | `Len.Abp.Wpf` | 契约层模块 |

### 21.6 视图契约（`Len.Abp.Wpf.Mvvm`）

| 类型 | 说明 |
| --- | --- |
| `IView` | `ViewModel`、`ViewName`、`RequiredNavigationParameters` |
| `IView<TViewModel>` | 强类型 `ViewModel` |

### 21.7 核心（`Len.Abp.Core`）

| 类型 | 说明 |
| --- | --- |
| `EnumHelper` | 静态入口，`GetNameValues<TEnum>()` |
| `IEnumResolver<TEnum>` | 枚举解析器契约 |
| `AbpServiceProvider` | 静态服务提供程序，由 `LenAbpCoreModule` 初始化 |
| `EnumerableExtensions.ForEach` | 集合遍历扩展 |

---

## 手册覆盖范围

本文覆盖 `Len.Abp` 当前版本的全部对外能力，共 15 项功能：

| 类别 | 功能 | 章节 |
| --- | --- | --- |
| MVVM | 声明视图、视图模型自动注册 | 4 |
| 区域 | 声明区域、三种内置宿主、嵌套区域 | 5 |
| 导航 | 导航、参数与校验、历史与回退、保活与生命周期 | 6–9 |
| 生成器 | 依赖属性、附加依赖属性、路由事件、附加路由事件 | 10–13 |
| 宿主 | 宿主配置、全局异常处理、资源字典合并 | 14、15、17 |
| 扩展 | 自定义区域适配器与行为 | 16 |
| 基础库 | 枚举名称解析、集合遍历 | 18 |

**本文不覆盖的内容**：

- 项目自身的构建、测试、覆盖率与发布流程——那是贡献者需要的信息，与使用框架无关；
- 框架内部的设计取舍与历史决策——使用层面不需要了解，遇到具体行为时以本文描述为准；
- 需求规划与缺陷记录——同样属于项目维护范畴。

如果你在使用中遇到本文没有描述的行为，那多半是本文的疏漏，而不是隐藏功能。
