using Len.Abp.Wpf;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using Volo.Abp.DependencyInjection;
using Xunit;

namespace Len.Abp.Wpf.Tests;

/// <summary>
/// 覆盖 <see cref="ShellActivator"/> 把已有窗口带到前台的行为。
/// </summary>
/// <remarks>
/// <see cref="Window"/> 有线程亲和性，所有断言都在同一个 STA 线程上执行。
/// </remarks>
public class ShellActivatorTests
{
    /// <summary>
    /// 最小化的窗口被还原，隐藏的窗口被显示。
    /// </summary>
    [Fact]
    public void Activate_RestoresMinimizedAndShowsHiddenWindows()
    {
        Sta.Run(() =>
        {
            var minimized = new ShellWindow { WindowState = WindowState.Minimized };
            var hidden = new ShellWindow { Visibility = Visibility.Hidden };
            var activator = CreateActivator(minimized, hidden);

            activator.Activate();

            Assert.Equal(WindowState.Normal, minimized.WindowState);
            Assert.True(minimized.IsVisible);
            Assert.True(hidden.IsVisible);
        });
    }

    /// <summary>
    /// 置顶状态在激活后恢复，不会让窗口一直浮在最上层。
    /// </summary>
    [Fact]
    public void Activate_RestoresTopmostAfterActivation()
    {
        Sta.Run(() =>
        {
            var window = new ShellWindow();
            var activator = CreateActivator(window);

            activator.Activate();

            Assert.False(window.Topmost);
        });
    }

    /// <summary>
    /// 已显示的窗口不会被重复显示，状态保持不变。
    /// </summary>
    [Fact]
    public void Activate_VisibleWindow_KeepsState()
    {
        Sta.Run(() =>
        {
            var window = new ShellWindow();
            window.Show();

            var activator = CreateActivator(window);
            activator.Activate();

            Assert.True(window.IsVisible);
            Assert.Equal(WindowState.Normal, window.WindowState);
        });
    }

    /// <summary>
    /// 非 Window 的 Shell 实现被跳过，不抛异常。
    /// </summary>
    [Fact]
    public void Activate_NonWindowShell_IsSkipped()
    {
        Sta.Run(() =>
        {
            var activator = CreateActivator(new NotAWindowShell());

            activator.Activate();
        });
    }

    /// <summary>
    /// 没有 Shell 时激活是空操作。
    /// </summary>
    [Fact]
    public void Activate_NoShells_DoesNothing()
    {
        Sta.Run(() => CreateActivator().Activate());
    }

    private static ShellActivator CreateActivator(params IShell[] shells)
    {
        var services = new ServiceCollection();

        foreach (var shell in shells)
        {
            services.AddSingleton(shell);
        }

        var provider = services.BuildServiceProvider();

        return new ShellActivator(new AbpLazyServiceProvider(provider));
    }

    /// <summary>
    /// 记录激活次数的窗口。
    /// </summary>
    private sealed class RecordingWindow : ShellWindow
    {
        public int ActivateCount { get; private set; }

        public new void Activate()
        {
            ActivateCount++;
            base.Activate();
        }
    }

    /// <summary>
    /// 实现 <see cref="IShell"/> 的窗口，供激活器解析。
    /// </summary>
    private class ShellWindow : Window, IShell;

    /// <summary>
    /// 不是窗口的 Shell 实现。
    /// </summary>
    private sealed class NotAWindowShell : IShell;
}
