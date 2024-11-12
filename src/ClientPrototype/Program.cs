// See https://aka.ms/new-console-template for more information

using ClientPrototype.Abstractions;
using ClientPrototype.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.local.json", optional: true);

builder.Services.AddDriverAdapter(builder.Configuration);

var cancellationTokenSource = new CancellationTokenSource();

var app = builder.Build();

await app.Services.GetRequiredService<IDriverWorker>().Watch(cancellationTokenSource.Token);