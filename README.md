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
| `demos/wpf/WpfApp.Controls` | net10.0-windows | 示例自定义控件库（独立程序集，通过 `XmlnsDefinition` 暴露 XAML 命名空间） |
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

区域可以无限层嵌套：视图内部再声明区域，该区域就归属于这个视图实例。

```xml
<!-- DemoView.xaml：DemoView 被导航进 MainRegion 后，DemoRegion 属于这个 DemoView 实例 -->
<UserControl ...>
    <ContentControl len:Region.Name="DemoRegion" />
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
- **容器管理的嵌套视图保留自己的区域**。区域只在视图首次加载时创建，摘掉后不会重建，所以单例/作用域视图的区域必须留着，供下次导航复用。

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

#### 视图保活

视图失活时是否留在区域中复用，按以下优先级判断：

| 条件 | 行为 |
| --- | --- |
| 视图模型声明 `KeepAlive = true` | 视图与视图模型都保留，下次导航复用同一实例 |
| 视图或视图模型是单例 / 作用域 | 按 IoC 规则保留，下次导航复用容器中的实例 |
| 其余情况（含未实现 `IRegionMemberLifetime`） | 不保留，移出区域并释放瞬态实例 |

```csharp
public class GalleryViewModel : ObservableObject, IRegionMemberLifetime
{
    // 需要保留状态（如日志、输入内容）时显式保活。
    public bool KeepAlive => true;
}
```

未实现 `IRegionMemberLifetime` 等同于 `KeepAlive = false`，避免忘记声明契约导致视图在区域里无限累积。

容器管理的实例（`ISingletonDependency` / `IScopedDependency`）即使 `KeepAlive = false` 也会保留：
它们由 IoC 容器持有，移除区域不会释放它们，反而会丢掉视图内部的嵌套区域，下次导航还得重建整棵视图子树。
释放只针对瞬态实例，单例与作用域实例由容器负责销毁。

拆除父视图时会递归处理整棵区域子树，但容器管理的嵌套视图是例外：它们的区域会保留，
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
宿主必须派生自 `DependencyObject`——WPF 注册附加属性时要求 `ownerType` 满足该条件，静态类不行。

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

### 8. 示例程序

自定义控件放在独立类库 `demos/wpf/WpfApp.Controls` 中，并通过程序集级特性暴露 XAML 命名空间，
用法与 `Len.Abp.Wpf.Abstractions` 一致：

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

主窗口左侧的导航按钮还演示了 `Lifetime` 的三种取值：单例视图反复导航复用同一实例，
瞬态视图每次导航创建新实例，作用域视图在作用域内复用。

## 许可证

[MIT](LICENSE.txt)
