using Domain;
using Microsoft.EntityFrameworkCore;
using ValueReaderService.Services;
using ValueReaderService.Services.ChineseRoomController;

namespace ValueReaderService;

public class Worker(ILogger<Worker> logger, IServiceProvider serviceProvider, IConfiguration configuration) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await LogDeviceCount(stoppingToken);

        logger.LogInformation("Read loop starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            DateTime wakeTime;

#if DEBUG
            wakeTime = DateTime.UtcNow;
#else
            wakeTime = GetNextWakeTime(DateTime.UtcNow.AddSeconds(30)).AddSeconds(-20);
            await Task.Delay(wakeTime - DateTime.UtcNow, stoppingToken);
#endif

            logger.LogInformation("Read pre-starting");

            var devices = await GetDevices();

            var tasks = new List<Task>();

#if !DEBUG
            wakeTime = GetNextWakeTime(DateTime.UtcNow.AddSeconds(5));
            await Task.Delay(wakeTime - DateTime.UtcNow, stoppingToken);
#endif

            logger.LogInformation("Read starting");

            foreach (var device in devices)
            {
                if (device.IsEnabled)
                {
                    if (device.Type == "ventilation")
                    {
                        RunReader<BacnetDeviceReader>(serviceProvider, wakeTime, tasks, device);
                    }
                    else if (device.Type == "room_controller")
                    {
                        RunReader<ChineseRoomControllerReader>(serviceProvider, wakeTime, tasks, device);
                    }
                    else if (device.Type == "shelly")
                    {
                        RunReader<ShellyDeviceReader>(serviceProvider, wakeTime, tasks, device);
                    }
                }
            }

            await Task.WhenAll(tasks);

#if DEBUG
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
#endif
        }

        logger.LogInformation("Read loop stopping");
    }

    private static void RunReader<TReader>(IServiceProvider serviceProvider, DateTime wakeTime, List<Task> tasks, Device device) 
        where TReader : DeviceReader
    {
        tasks.Add(Task.Run(async () =>
        {
            using var scope = serviceProvider.CreateScope();

            var reader = scope.ServiceProvider.GetRequiredService<TReader>();
            await reader.ExecuteAsync(device, wakeTime);
        }, CancellationToken.None));
    }

    private async Task LogDeviceCount(CancellationToken stoppingToken)
    {
        try
        {
            using var countScope = serviceProvider.CreateScope();
            var dbContext = countScope.ServiceProvider.GetRequiredService<HomeSystemContext>();
            var deviceCount = await dbContext.Devices.CountAsync(stoppingToken);
            logger.LogInformation("Found {Count} devices", deviceCount);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to retrieve device count. Service will not start.");
            throw;
        }
    }

    private static DateTime GetNextWakeTime(DateTime now)
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

    private async Task<List<Device>> GetDevices()
    {
        var deviceType = configuration["DeviceType"] ?? throw new InvalidOperationException("DeviceType is missing from appSettings.");

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