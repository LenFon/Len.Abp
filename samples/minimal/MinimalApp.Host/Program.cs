using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Volo.Abp;

namespace MinimalApp.Host;

/// <summary>
/// 最小示例的入口：创建 Generic Host，初始化 ABP 模块并运行。
/// </summary>
/// <remarks>
/// WPF 的 UI 线程由 <c>LenAbpWpfModule</c> 注册的后台工作器启动，
/// 因此这里不需要创建 <c>Application</c>，也不需要写消息循环。
/// 日志在宿主构建之前就绪，启动阶段的失败同样能落盘。
/// </remarks>
internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
#if DEBUG
            .MinimumLevel.Debug()
#else
            .MinimumLevel.Information()
#endif
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .WriteTo.Async(c => c.File("Logs/logs.txt"))
            .WriteTo.Async(c => c.Console())
            .CreateLogger();

        try
        {
            Log.Information("正在启动最小示例宿主。");

            var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder(args);

            builder.Configuration.AddAppSettingsSecretsJson();
            builder.Logging.ClearProviders().AddSerilog();

            await builder.Services.AddApplicationAsync<MinimalAppHostModule>();

            var host = builder.Build();

            await host.InitializeAsync();
            await host.RunAsync();

            return 0;
        }
        catch (Exception exception)
        {
            if (exception is HostAbortedException)
            {
                throw;
            }

            Log.Fatal(exception, "宿主意外终止！");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
