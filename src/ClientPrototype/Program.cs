﻿// See https://aka.ms/new-console-template for more information

using ClientPrototype.Abstractions;
using ClientPrototype.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

var env = builder.Environment.EnvironmentName;

Console.WriteLine($"Current environment: {env}");

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

var app = builder.Build();

try
{
    await app.Services.GetRequiredService<IDriverWorker>().Watch();
}
finally
{
    await app.Services.GetRequiredService<IDriverWorker>().Stop();
}