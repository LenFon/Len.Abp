# Len.Abp

[![codecov](https://codecov.io/gh/LenFon/Len.Abp/branch/main/graph/badge.svg)](https://codecov.io/gh/LenFon/Len.Abp)

基于 [ABP Framework](https://abp.io) 的 WPF 桌面应用开发框架。用 Roslyn 源生成器消除 WPF 样板代码，用 ABP 的模块化与依赖注入体系承载 WPF 应用，并提供一套 Prism 风格的区域导航。

## 特性

- **源生成器消除样板代码**：依赖属性、附加属性、路由事件、附加路由事件、MVVM 视图绑定，全部由特性驱动生成。
- **ABP 依赖注入打通 MVVM**：视图按名称键控注册，导航时直接从容器解析，无需 ViewModelLocator 或 DataTemplate 映射。
- **Prism 风格的区域导航**：在 XAML 中声明区域，通过 `INavigationService` 切换视图，支持导航生命周期回调与视图保活策略。
- **WPF 与 ABP 宿主生命周期融合**：UI 线程由 ABP 后台工作器托管，支持单实例运行、资源字典合并与关闭模式配置。

## 项目结构

| 项目 | 目标框架 | 说明 |
| --- | --- | --- |
| `Len.Abp.Core` | net10.0 | 枚举解析器与集合扩展方法 |
| `Len.Abp.Wpf.Abstractions` | net10.0-windows | 契约层：特性、导航/区域接口、参数解析 |
| `Len.Abp.Wpf` | net10.0-windows | 运行时：UI 线程宿主、导航服务、区域引擎 |
| `Len.Abp.Analyzer` | netstandard2.0 | Roslyn 增量生成器的通用基础设施 |
| `Len.Abp.Analyzer.Wpf` | netstandard2.0 | 5 个 WPF 专用源生成器 |
| `demos/wpf/WpfApp` | net10.0-windows | 示例界面（MahApps + MaterialDesign） |
| `demos/wpf/WpfApp.Host` | net10.0-windows | 示例宿主（Generic Host + Serilog + Autofac） |

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

```bash
dotnet run --project demos/wpf/WpfApp.Host
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
因此视图必须保留无参构造函数（`InitializeComponent` 在其中执行），且 ViewModel 需要注册到容器。

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

### 2. 声明区域

在 XAML 中用附加属性标记区域宿主：

```xml
<ContentControl len:Region.Name="MainRegion" />
```

### 3. 导航

```csharp
await NavigationService.NavigateAsync("MainRegion", "demo2", IParameters.Create("?id=42"));
```

导航参数支持从查询字符串解析，并自动转换为目标类型：

```csharp
public Task OnNavigatedToAsync(INavigationContext context)
{
    var id = context.Parameters.GetValue<int>("id");
    return Task.CompletedTask;
}
```

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

### 5. 生成路由事件

```csharp
[RoutedEvent("Clicked", RoutingStrategy.Bubble)]
public partial class MyControl : Control { }
```

## 许可证

[MIT](LICENSE.txt)
