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
    Log.Information("Starting wpf host.");

    var builder = Host.CreateApplicationBuilder(args);

    builder.Configuration.AddAppSettingsSecretsJson();
    builder.Logging.ClearProviders().AddSerilog();

    builder.ConfigureContainer(builder.Services.AddAutofacServiceProviderFactory());

    await builder.Services.AddApplicationAsync<WpfAppHostModule>();

    var host = builder.Build();
    var a = host.Services.GetHashCode();
    await host.InitializeAsync();

    await host.RunAsync();

    return 0;
}
catch (Exception ex)
{
    if (ex is HostAbortedException)
    {
        throw;
    }

    Log.Fatal(ex, "Host terminated unexpectedly!");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}