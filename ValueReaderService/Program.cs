using Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NLog.Extensions.Logging;
using SharedServices;
using ValueReaderService;
using ValueReaderService.Services;
using ValueReaderService.Services.ChineseRoomController;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddLogging(loggingBuilder =>
        {
            // configure Logging with NLog
            loggingBuilder.ClearProviders();
            loggingBuilder.SetMinimumLevel(LogLevel.Information);
            loggingBuilder.AddNLog();
        });

        services.AddHostedService<Worker>();

        services.AddDbContext<HomeSystemContext>(options =>
            options.UseNpgsql(context.Configuration.GetConnectionString("HomeSystemContext")));

        services.AddSingleton<BacnetDeviceReader>();
        services.AddScoped<ChineseRoomControllerReader>();
        services.AddSingleton<ShellyDeviceReader>();
        services.AddSingleton<PointValueStore>();

        services.AddHttpClient();
    })
    .UseWindowsService(options =>
    {
        options.ServiceName = "Home System Service";
    })
    .Build();

await host.RunAsync();