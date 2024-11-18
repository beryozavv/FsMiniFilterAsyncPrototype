// See https://aka.ms/new-console-template for more information

using ClientPrototype.Abstractions;
using ClientPrototype.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

await Task.Delay(TimeSpan.FromSeconds(10));
var builder = Host.CreateApplicationBuilder(args);

var env = builder.Environment.EnvironmentName;
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .AddJsonFile($"appsettings.{env}.json", optional: true)
    .AddJsonFile($"appsettings.{env}.local.json", optional: true);

builder.Services.AddDriverAdapter(builder.Configuration);

// Configure NLog
builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.SetMinimumLevel(LogLevel.Trace);
    logging.AddNLog($"nlog.{env}.config");
});

// Add NLog as the logger provider
builder.Services.AddSingleton<ILoggerProvider, NLogLoggerProvider>();

var cancellationTokenSource = new CancellationTokenSource();

var app = builder.Build();

try
{
    await app.Services.GetRequiredService<IDriverWorker>().Watch(cancellationTokenSource.Token);
}
finally
{
    app.Services.GetRequiredService<IDriverClient>().Disconnect(CancellationToken.None);
}