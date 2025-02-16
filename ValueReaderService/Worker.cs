using Domain;
using Microsoft.EntityFrameworkCore;
using SharedServices;
using System.Collections.Concurrent;
using ValueReaderService.Services;
using ValueReaderService.Services.ChineseRoomController;
using ValueReaderService.Services.InverterSchedule;
using ValueReaderService.Services.YrNoWeatherForecast;

namespace ValueReaderService;

public class Worker(ILogger<Worker> logger, IServiceProvider serviceProvider, IConfiguration configuration) : BackgroundService
{
    private readonly Dictionary<Type, (DeviceReader, Device, DevicePoint[]?)> frequentReads = [];
    private readonly ConcurrentQueue<Task> frequentReadTasks = [];
    private readonly object frequentReadLock = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (stoppingToken.IsCancellationRequested)
            return;

        var deviceType = configuration["DeviceType"] ?? throw new InvalidOperationException("DeviceType is missing from appSettings.");

        await LogDeviceCount(stoppingToken);

        if (stoppingToken.IsCancellationRequested)
            return;

        logger.LogInformation("Read loop starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            DateTime wakeTime;

#if DEBUG
            wakeTime = DateTime.UtcNow;
#else
            wakeTime = GetNextWakeTime(DateTime.UtcNow.AddSeconds(30), deviceType).AddSeconds(-20);
            await Task.Delay(wakeTime - DateTime.UtcNow, stoppingToken);
#endif

            logger.LogInformation("Read pre-starting");

            var devices = await GetDevices(deviceType);

            var tasks = new List<Task>();

#if !DEBUG
            wakeTime = GetNextWakeTime(DateTime.UtcNow.AddSeconds(5), deviceType);
            await Task.Delay(wakeTime - DateTime.UtcNow, stoppingToken);
#endif

            logger.LogInformation("Read starting");

            try
            {
                foreach (var device in devices)
                {
                    if (device.IsEnabled)
                    {
                        if (device.Type == "ventilation")
                        {
                            RunReader<BacnetDeviceReader>(serviceProvider, wakeTime, tasks, device, stoppingToken);
                        }
                        else if (device.Type == "room_controller")
                        {
                            RunReader<ChineseRoomControllerReader>(serviceProvider, wakeTime, tasks, device, stoppingToken);
                        }
                        else if (device.Type == "shelly")
                        {
                            RunReader<ShellyDeviceReader>(serviceProvider, wakeTime, tasks, device, stoppingToken);
                        }
                        else if (device.Type == "heat_pump")
                        {
                            RunReader<MyUplinkDeviceReader>(serviceProvider, wakeTime, tasks, device, stoppingToken);
                        }
                        else if (device.Type == "airobot_thermostat")
                        {
                            RunReader<AirobotThermostatReader>(serviceProvider, wakeTime, tasks, device, stoppingToken);
                        }
                        else if (device.Type == "deye_inverter")
                        {
                            RunReader<ModbusDeviceReader>(serviceProvider, wakeTime, tasks, device, stoppingToken);
                        }
                        else if (device.Type == "electricity_price")
                        {
                            RunReader<ElectricityPriceReader>(serviceProvider, wakeTime, tasks, device, stoppingToken);
                        }
                        else if (device.Type == "inverter_schedule")
                        {
                            RunReader<InverterScheduleRunner>(serviceProvider, wakeTime, tasks, device, stoppingToken);
                        }
                        else if (device.Type == "yrno_weather_forecast")
                        {
                            RunReader<YrNoWeatherForecastReader>(serviceProvider, wakeTime, tasks, device, stoppingToken);
                        }
                    }
                }

                await Task.WhenAll(tasks);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Read failed");
            }

#if DEBUG
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
#endif
        }

        while (frequentReadTasks.TryDequeue(out var task))
        {
            await task;
        }

        logger.LogInformation("Read loop stopping");
    }

    private void RunReader<TReader>(IServiceProvider serviceProvider, DateTime wakeTime, List<Task> tasks, Device device, CancellationToken stoppingToken)
        where TReader : DeviceReader
    {
        tasks.Add(Task.Run(async () =>
        {
            var pointValueStore = serviceProvider.GetRequiredService<PointValueStore>();

            using (var scope = serviceProvider.CreateScope())
            {
                var reader = scope.ServiceProvider.GetRequiredService<TReader>();
                var pointValues = await reader.ExecuteAsync(device, wakeTime, device.DevicePoints);
                if (pointValues is not null)
                {
                    if (!reader.StorePointsWithReplace)
                    {
                        foreach (var (point, value, timestamp) in pointValues)
                        {
                            pointValueStore.StoreValue(device.Id, point.Id, timestamp ?? wakeTime, value);
                        }
                    }
                    else
                    {
                        foreach (var g in pointValues.GroupBy(x => (x.Point.DeviceId, x.Point.Id)))
                        {
                            pointValueStore.StoreValuesWithReplace(g.Key.DeviceId, g.Key.Id, g.Select(x => (x.TimeStamp ?? wakeTime, (string?)x.Value)).ToArray());
                        }
                    }
                }
            }

            HandleFrequentReadPoints<TReader>(serviceProvider, device, pointValueStore, stoppingToken);

        }, CancellationToken.None));
    }

    private void HandleFrequentReadPoints<TReader>(
        IServiceProvider serviceProvider,
        Device device,
        PointValueStore pointValueStore,
        CancellationToken stoppingToken) where TReader : DeviceReader
    {
        try
        {
            if (device.DevicePoints.Any(x => x.IsFrequentReadEnabled))
            {
                bool containsReader = false;
                lock (frequentReadLock)
                {
                    containsReader = frequentReads.ContainsKey(typeof(TReader));
                }
                if (!containsReader)
                {
                    var reader = serviceProvider.GetRequiredService<TReader>();
                    lock (frequentReadLock)
                    {
                        frequentReads[typeof(TReader)] = (reader, device, device.DevicePoints.Where(x => x.IsFrequentReadEnabled).ToArray());
                    }

                    var t = Task.Run(async () =>
                    {
                        while (!stoppingToken.IsCancellationRequested)
                        {
                            try
                            {
                                DeviceReader? reader = null;
                                Device? device = null;
                                DevicePoint[]? devicePoints = null;
                                bool found;
                                lock (frequentReadLock)
                                {
                                    found = frequentReads.TryGetValue(typeof(TReader), out var fr);
                                    if (found)
                                    {
                                        (reader, device, devicePoints) = fr;
                                    }
                                }
                                if (found)
                                {
                                    if (devicePoints == null || devicePoints.Length == 0)
                                    {
                                        return;
                                    }

                                    var now = DateTime.UtcNow;
                                    var pointValues = await reader!.ExecuteAsync(device!, now, devicePoints);
                                    if (pointValues is not null)
                                    {
                                        foreach (var (point, value, _) in pointValues)
                                        {
                                            pointValueStore.StoreFrequentValue(device!.Id, point.Id, now, value);
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                logger.LogError(ex, "Frequent read cycle has failed.");
                            }

                            try
                            {
                                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                            }
                            catch (TaskCanceledException)
                            {
                                return;
                            }
                        }
                    }, CancellationToken.None);
                    frequentReadTasks.Enqueue(t);
                }
                else
                {
                    lock (frequentReadLock)
                    {
                        var (deviceReader, _, _) = frequentReads[typeof(TReader)];
                        frequentReads[typeof(TReader)] = (deviceReader, device, device.DevicePoints.Where(x => x.IsFrequentReadEnabled).ToArray());
                    }
                }
            }
            else
            {
                lock (frequentReadLock)
                {
                    lock (frequentReadLock)
                    {
                        if (frequentReads.TryGetValue(typeof(TReader), out var fr))
                        {
                            var (reader, _, _) = fr;
                            frequentReads[typeof(TReader)] = (reader, device, null);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Frequent read handling has failed.");
        }
    }

    private async Task LogDeviceCount(CancellationToken stoppingToken)
    {
        while (true)
        {
            try
            {
                using var countScope = serviceProvider.CreateScope();
                var dbContext = countScope.ServiceProvider.GetRequiredService<HomeSystemContext>();
                var deviceCount = await dbContext.Devices.CountAsync(stoppingToken);
                logger.LogInformation("Found {Count} devices", deviceCount);
                return;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to retrieve device count.");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                if (stoppingToken.IsCancellationRequested)
                    return;
            }
        }
    }

#if !DEBUG

    private static DateTime GetNextWakeTime(DateTime now, string deviceType)
    {
        if (deviceType == "electricity_price")
        {
            return new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc).AddHours(1);
        }
        else
        {
            return now.Minute switch
            {
                < 10 => new DateTime(now.Year, now.Month, now.Day, now.Hour, 10, 0, DateTimeKind.Utc),
                < 20 => new DateTime(now.Year, now.Month, now.Day, now.Hour, 20, 0, DateTimeKind.Utc),
                < 30 => new DateTime(now.Year, now.Month, now.Day, now.Hour, 30, 0, DateTimeKind.Utc),
                < 40 => new DateTime(now.Year, now.Month, now.Day, now.Hour, 40, 0, DateTimeKind.Utc),
                < 50 => new DateTime(now.Year, now.Month, now.Day, now.Hour, 50, 0, DateTimeKind.Utc),
                _ => new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc).AddHours(1)
            };
        }
    }

#endif

    private async Task<List<Device>> GetDevices(string deviceType)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<HomeSystemContext>();

        while (true)
        {
            try
            {
                var devices = await dbContext.Devices
                    .Where(x => x.Type == deviceType)
                    .AsNoTracking()
                    .Include(d => d.DevicePoints).ThenInclude(dp => dp.DataType)
                    .Include(d => d.DevicePoints).ThenInclude(dp => dp.EnumMembers)
                    .ToListAsync();
                return devices;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to retrieve devices list");
            }
        }
    }
}