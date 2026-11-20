using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Volo.Abp;
using WpfApp.Host;

Log.Logger = new LoggerConfiguration()
#if DEBUG
    .MinimumLevel.Debug()
#else
    .MinimumLevel.Information()
#endif
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Async(c => c.File("Logs/logs.txt"))
    .WriteTo.Async(c => c.Console())
    .CreateLogger();

try
{
    Log.Information("正在启动 WPF 宿主。");

    var builder = Host.CreateApplicationBuilder(args);

    builder.Configuration.AddAppSettingsSecretsJson();
    builder.Logging.ClearProviders().AddSerilog();

    builder.ConfigureContainer(builder.Services.AddAutofacServiceProviderFactory());

    await builder.Services.AddApplicationAsync<WpfAppHostModule>();

    var host = builder.Build();

    await host.InitializeAsync();

    await host.RunAsync();

    return 0;
}
catch (OperationCanceledException)
{
    // 宿主在启动完成前被要求停止。单实例模式下后续实例会走这条路径：
    // 检测到已有实例后发出激活请求并立即停止，具体原因由工作器记录。
    // 这是正常退出，不是故障，因此不写 Fatal 也不返回非零退出码。
    Log.Information("宿主在启动完成前被取消，正常退出。");
    return 0;
}
catch (Exception ex)
{
    if (ex is HostAbortedException)
    {
        throw;
    }

    Log.Fatal(ex, "宿主意外终止！");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}