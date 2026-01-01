using Domain;
using Microsoft.EntityFrameworkCore;
using NLog.Extensions.Logging;
using SharedServices;
using ValueReaderService;
using ValueReaderService.Services;
using ValueReaderService.Services.AirobotThermostat;
using ValueReaderService.Services.ChineseRoomController;
using ValueReaderService.Services.InverterSchedule;
using ValueReaderService.Services.SolarModel;
using ValueReaderService.Services.YrNoWeatherForecast;

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

        services.AddScoped<BacnetDeviceReader>();
        services.AddScoped<ChineseRoomControllerReader>();
        services.AddScoped<ShellyDeviceReader>();
        services.AddScoped<MyUplinkDeviceReader>();
        services.AddScoped<AirobotThermostatReader>();
        services.AddScoped<ModbusDeviceReader>();
        services.AddScoped<ElectricityPriceReader>();
        services.AddScoped<YrNoWeatherForecastReader>();
        services.AddScoped<InverterScheduleRunner>();
        services.AddScoped<HeatPumpScheduleRunner>();
        services.AddScoped<SolarModelRunner>();
        services.AddSingleton<PointValueStore>();
        services.AddSingleton<ConfigModel>();
        services.AddScoped<PointValueStoreAdapter>();
        services.AddScoped<SolarmanV5Adapter>();

        services.AddHttpClient();
    })
    .UseWindowsService(options =>
    {
        options.ServiceName = "Home System Service";
    })
    .Build();

await host.RunAsync();