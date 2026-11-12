using Len.Abp.Wpf.Mvvm;

namespace Len.Abp.Wpf.Tests;

/// <summary>
/// 测试用的视图替身。
/// </summary>
internal sealed class FakeView(string viewName, object? viewModel = null) : IView
{
    /// <inheritdoc/>
    public object ViewModel { get; } = viewModel ?? new object();

    /// <inheritdoc/>
    public string ViewName { get; } = viewName;
}
