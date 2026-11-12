using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Windows;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Modularity;

namespace Len.Abp.Wpf;

internal class WpfThreadService(
    IAbpLazyServiceProvider abpLazyServiceProvider,
    WpfContext wpfContext,
    IHostApplicationLifetime hostApplicationLifetime
) : ISingletonDependency
{
    private readonly IAbpLazyServiceProvider _abpLazyServiceProvider = abpLazyServiceProvider;
    private readonly IHostApplicationLifetime _hostApplicationLifetime = hostApplicationLifetime;
    private readonly WpfContext _uiContext = wpfContext;

    /// <summary>
    /// 在独立的 STA 线程上创建并启动 WPF 应用。
    /// </summary>
    /// <returns>WPF 应用创建完成后完成的任务。</returns>
    public Task StartAsync()
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                PreStartUiThread();
                _uiContext.IsRunning = true;

                // 在 Application 创建完成后再发出信号，使调用方可以安全依赖 WpfContext.WpfApplication。
                tcs.TrySetResult();

                StartUiThread();
            }
            catch (Exception exception)
            {
                // 线程体抛出的异常无法传播给调用方：必须通过任务传递，
                // 否则 StartAsync 会永久挂起，同时异常在后台线程上终止进程。
                _uiContext.IsRunning = false;
                tcs.TrySetException(exception);
            }
        })
        {
            IsBackground = true
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        return tcs.Task;
    }

    private void OnApplicationExit()
    {
        _uiContext.IsRunning = false;
        if (!_uiContext.IsLifetimeLinked)
        {
            return;
        }

        if (
            _hostApplicationLifetime.ApplicationStopped.IsCancellationRequested
            || _hostApplicationLifetime.ApplicationStopping.IsCancellationRequested
        )
        {
            return;
        }

        _hostApplicationLifetime.StopApplication();
    }

    private void PreStartUiThread()
    {
        var wpfApplication = new Application() { ShutdownMode = _uiContext.ShutdownMode };
        wpfApplication.Dispatcher.Invoke(() => wpfApplication.Exit += (s, e) => OnApplicationExit());

        _uiContext.WpfApplication = wpfApplication;
    }

    private void StartUiThread()
    {
        _uiContext.WpfApplication.Dispatcher.Invoke(() =>
        {
            _uiContext.IsRunning = true;
            var app = _abpLazyServiceProvider.LazyGetRequiredService<IAbpApplication>();

            foreach (var module in app.Modules)
            {
                if (module.Instance is AbpWpfModule abpModule)
                {
                    abpModule.ConfigureResources(_uiContext.WpfApplication.Resources.MergedDictionaries);
                }
            }

            var shellWindows = _abpLazyServiceProvider.GetServices<IShell>().Cast<Window>().ToList();

            _uiContext.WpfApplication.Startup += (s, e) =>
            {
                foreach (var window in shellWindows)
                {
                    window?.Show();
                }
            };

            _uiContext.WpfApplication.Run();
        });
    }
}