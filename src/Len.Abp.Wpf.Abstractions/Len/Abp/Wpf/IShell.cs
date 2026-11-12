using Volo.Abp.DependencyInjection;

namespace Len.Abp.Wpf;

/// <summary>
/// 标记主窗口视图。实现该接口的视图固定注册为单例，并作为应用的根视图。
/// </summary>
public interface IShell : ISingletonDependency
{ }