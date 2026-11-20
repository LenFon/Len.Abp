# Len.Abp.Core

ABP 框架的 WPF 基础扩展：枚举名称解析与集合遍历。

本包是 `Len.Abp.Wpf` 系列的底层依赖，提供两件与 WPF 无关、但桌面端普遍需要的能力。
它依赖 `Volo.Abp.Core` 与 `Volo.Abp.Localization`。

## 安装

```bash
dotnet add package Len.Abp.Core
```

## 枚举名称解析

`IEnumResolver<TEnum>` 把枚举项解析为本地化的名称与值对，`EnumHelper` 提供静态入口。

```csharp
public enum OrderStatus
{
    Pending,
    Completed,
}

foreach (var item in EnumHelper.GetNameValues<OrderStatus>())
{
    Console.WriteLine($"{item.Value}: {item.Name}");
}
```

`NameValue<TEnum>` 来自 `Volo.Abp`，`Name` 由 ABP 的 `IAbpEnumLocalizer` 解析。
它按以下顺序查本地化资源，全部未命中时回退到成员名：

1. `Enum:{类型名}.{成员名}`
2. `{类型名}.{成员名}`
3. `{成员名}`

因此**不需要配置任何资源即可直接使用**，得到的名称就是成员名。
要显示中文或其它文案，需要提供对应的本地化资源，而不是给枚举成员加 `[Display]`——
ABP 的枚举本地化器不读取 `DisplayAttribute`。

未定义的值与 `[Flags]` 组合值拿不到单一成员名，此时回退到 `ToString()`：
前者得到数字字符串，后者得到逗号分隔的成员名。

`EnumHelper` 通过 `AbpServiceProvider` 取解析器，**必须先加载 `LenAbpCoreModule`**：

```csharp
[DependsOn(typeof(LenAbpCoreModule))]
public class MyModule : AbpModule { }
```

未初始化时抛 `InvalidOperationException`，消息会提示检查模块是否已加载。

## 集合遍历

`EnumerableExtensions.ForEach` 提供 `IEnumerable<T>` 上的同步遍历（命名空间 `Len.Abp.Core.Extensions`）：

```csharp
using Len.Abp.Core.Extensions;

var names = new[] { "a", "b" };
names.ForEach(Console.WriteLine);
```

这是 C# 14 的扩展成员语法（`extension<T>(IEnumerable<T> source)`），
调用形式与实例方法一致。它不替代 LINQ——需要返回值或链式操作时仍用 `Select` / `Where`。

## 模块

`LenAbpCoreModule` 负责：

- 注册 `IEnumResolver<>` 的开放泛型实现；
- 在 `OnPreApplicationInitialization` 中初始化静态 `AbpServiceProvider`。

`AbpServiceProvider` 是进程级单例，框架里只有本模块调用 `Initialize`。
它本身允许重复调用并替换现有提供程序，多容器场景下后初始化的会覆盖前者；
桌面应用单容器是常态，因此未做额外防护。

## 许可证

MIT
