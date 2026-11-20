using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using Volo.Abp.DependencyInjection;

namespace Len.Abp.Wpf;

/// <summary>
/// 把已有实例的窗口带到前台。
/// </summary>
/// <remarks>
/// 必须在 UI 线程上调用：<see cref="Window"/> 有线程亲和性。
/// Windows 会拒绝非前台进程的 <see cref="Window.Activate"/>，因此临时切换
/// <see cref="Window.Topmost"/> 绕过限制，这是桌面应用的常见做法。
/// </remarks>
internal class ShellActivator(IAbpLazyServiceProvider abpLazyServiceProvider) : ITransientDependency
{
    private readonly IAbpLazyServiceProvider _abpLazyServiceProvider = abpLazyServiceProvider;

    /// <summary>
    /// 激活所有 Shell 窗口：最小化的还原，隐藏的显示，然后置前。
    /// </summary>
    public virtual void Activate()
    {
        foreach (var shell in _abpLazyServiceProvider.GetServices<IShell>())
        {
            if (shell is not Window window)
            {
                continue;
            }

            if (window.WindowState == WindowState.Minimized)
            {
                window.WindowState = WindowState.Normal;
            }

            // 隐藏的窗口无法被激活，先显示出来。
            if (!window.IsVisible)
            {
                window.Show();
            }

            window.Activate();
            window.Topmost = true;
            window.Topmost = false;
        }
    }
}
