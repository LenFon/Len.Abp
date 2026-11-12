using Len.Abp.Wpf;
using System.Windows;
using Xunit;

namespace Len.Abp.Wpf.Tests;

/// <summary>
/// 覆盖 <see cref="WpfOptions"/> 与 <see cref="WpfContext"/> 的默认值。
/// </summary>
public class WpfOptionsTests
{
    /// <summary>
    /// 默认链接宿主生命周期、按最后一个窗口关闭退出、不启用单实例。
    /// </summary>
    [Fact]
    public void Defaults_AreExpected()
    {
        var options = new WpfOptions();

        Assert.True(options.IsLifetimeLinked);
        Assert.Equal(ShutdownMode.OnLastWindowClose, options.ShutdownMode);
        Assert.False(options.SingleInstance);
    }

    /// <summary>
    /// <see cref="WpfContext"/> 的默认值与选项一致。
    /// </summary>
    [Fact]
    public void WpfContext_Defaults_AreExpected()
    {
        var context = new WpfContext();

        Assert.True(context.IsLifetimeLinked);
        Assert.False(context.IsRunning);
        Assert.Equal(ShutdownMode.OnLastWindowClose, context.ShutdownMode);
    }
}
