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

    public Task StartAsync()
    {
        var tcs = new TaskCompletionSource();
        var thread = new Thread(() =>
        {
            tcs.SetResult();
            PreStartUiThread();
            _uiContext.IsRunning = true;
            StartUiThread();
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

            if (_uiContext.WpfApplication.Dispatcher.Thread.ThreadState != ThreadState.Running)
            {
                _uiContext.WpfApplication.Run();
            }
        });
    }
}