using Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PointValueStoreClient;
using Web.Client.DTOs;
using Web.Helpers;

namespace Web.Controllers;

[Route("api/[controller]")]
[ApiController]
public class InverterScheduleController : ControllerBase
{
    private readonly HomeSystemContext context;
    private readonly HttpClient httpClient;
    private readonly IConfiguration configuration;
    private readonly PointValueStore pointValueStore;

    public InverterScheduleController(HomeSystemContext context, HttpClient httpClient, IConfiguration configuration)
    {
        this.context = context;
        this.httpClient = httpClient;
        this.configuration = configuration;

        httpClient.BaseAddress = new Uri(configuration["PointValueStoreConnectorUrl"]
            ?? throw new InvalidOperationException("PointValueStoreConnectorUrl is missing from config"));

        pointValueStore = PointValueStoreClientFactory.Create(httpClient);
    }

    [HttpGet("{date}")]
    public async Task<ActionResult<InverterDayScheduleDto>> Get(DateOnly date)
    {
        var points = await context.DevicePoints.AsNoTrackingWithIdentityResolution().Where(x => x.Device.Type == "inverter_schedule").ToArrayAsync();
        if (points.Length < 3)
        {
            return NotFound("Schedule points have not been configured");
        }

        var baseUrl = configuration["PointValueStoreConnectorUrl"];

        var batteryLevelValues = await PointValueStoreHelpers.GetPointValues(points, "battery-level", date, pointValueStore, true);
        var batterySellLevelValues = await PointValueStoreHelpers.GetPointValues(points, "battery-sell-level", date, pointValueStore, true);
        var gridChargeEnableValues = await PointValueStoreHelpers.GetPointValues(points, "grid-charge-enable", date, pointValueStore, true);
        var adaptiveSellEnableValues = await PointValueStoreHelpers.GetPointValues(points, "adaptive-sell-enable", date, pointValueStore, true);

        var result = new InverterDayScheduleDto { Entries = new InverterScheduleDto[96] };
        for (int i = 0; i < 96; i++)
        {
            var isGridChargeEnabled = gridChargeEnableValues.Values!.FirstOrDefault(x => (int)(x.Timestamp.TimeOfDay.TotalMinutes / 15) == i)?.Value;
            var isAdaptiveSellEnabled = adaptiveSellEnableValues.Values!.FirstOrDefault(x => (int)(x.Timestamp.TimeOfDay.TotalMinutes / 15) == i)?.Value;
            var hour = new InverterScheduleDto
            {
                Hour = i / 4,
                Minute = (i % 4) * 15,
                IsGridChargeEnabled = isGridChargeEnabled.HasValue ? isGridChargeEnabled.Value > 0.0 : null,
                BatteryLevel = (int?)batteryLevelValues.Values!.FirstOrDefault(x => (int)(x.Timestamp.TimeOfDay.TotalMinutes / 15) == i)?.Value,
                BatterySellLevel = (int?)batterySellLevelValues.Values!.FirstOrDefault(x => (int)(x.Timestamp.TimeOfDay.TotalMinutes / 15) == i)?.Value,
                IsAdaptiveSellEnabled = isAdaptiveSellEnabled.HasValue ? isAdaptiveSellEnabled.Value > 0.0 : null
            };
            result.Entries[i] = hour;
        }

        return result;
    }

    [HttpPut("{date}")]
    public async Task<ActionResult> Put(DateOnly date, InverterDayScheduleDto daySchedule)
    {
        DayScheduleHelpers.ValidateDaySchedule(daySchedule, true);

        var points = await context.DevicePoints.AsNoTrackingWithIdentityResolution().Where(x => x.Device.Type == "inverter_schedule").ToArrayAsync();
        if (points.Length < 3)
        {
            return NotFound("Schedule points have not been configured");
        }

        var batteryLevelPoint = points.FirstOrDefault(x => x.Address == "battery-level");
        var batterySellLevelPoint = points.FirstOrDefault(x => x.Address == "battery-sell-level");
        var gridChargeEnablePoint = points.FirstOrDefault(x => x.Address == "grid-charge-enable");
        var adaptiveSellEnablePoint = points.FirstOrDefault(x => x.Address == "adaptive-sell-enable");

        if (batteryLevelPoint == null || gridChargeEnablePoint == null || adaptiveSellEnablePoint == null || batterySellLevelPoint == null)
        {
            return NotFound("Schedule points have not been configured");
        }

        var hoursPerDate = (int)(date.ToDateTime(new TimeOnly(), DateTimeKind.Local).AddDays(1).ToUniversalTime()
            - date.ToDateTime(new TimeOnly(), DateTimeKind.Local).ToUniversalTime()).TotalHours;

        var gridChargeEnableValues = new ValueContainerDto { Values = new NumericValueDto[hoursPerDate * 12] };
        var batteryLevelValues = new ValueContainerDto { Values = new NumericValueDto[hoursPerDate * 12] };
        var batterySellLevelValues = new ValueContainerDto { Values = new NumericValueDto[hoursPerDate * 12] };
        var adaptiveSellEnableValues = new ValueContainerDto { Values = new NumericValueDto[hoursPerDate * 12] };

        var time0 = date.ToDateTime(new TimeOnly(), DateTimeKind.Local).ToUniversalTime();
        var entries = daySchedule.Entries;
        if (hoursPerDate == 25)
        {
            entries = [.. entries[0..(4 * 4)], .. entries[(3 * 4)..(4 * 4)], .. entries[(4 * 4)..]];
        }
        else if (hoursPerDate == 23)
        {
            entries = [.. entries[0..(3 * 4)], .. entries[(4 * 4)..]];
        }

        for (int i = 0; i < hoursPerDate * 4; i++)
        {
            var time = time0.AddMinutes(i * 15);
            var hourSchedule = entries[i];
            if (i == 0)
            {
                hourSchedule.BatterySellLevel ??= 100;
            }
            else
            {
                var prevHourSchedule = entries[i - 1];
                hourSchedule.BatterySellLevel ??= prevHourSchedule.BatterySellLevel;
                if (hourSchedule.BatteryLevel == null)
                {
                    hourSchedule.BatteryLevel = prevHourSchedule.BatteryLevel;
                    hourSchedule.IsGridChargeEnabled = prevHourSchedule.IsGridChargeEnabled;
                    hourSchedule.IsAdaptiveSellEnabled = prevHourSchedule.IsAdaptiveSellEnabled;
                }
            }

            double? batteryLevel = hourSchedule.BatteryLevel;
            if (batteryLevel.HasValue)
            {
                if (batteryLevel < 0)
                    batteryLevel = 0;
                else if (batteryLevel > 100)
                    batteryLevel = 100;
            }

            double? batterySellLevel = hourSchedule.BatterySellLevel;
            if (batterySellLevel.HasValue)
            {
                if (batterySellLevel < 0)
                    batterySellLevel = 0;
                else if (batterySellLevel > 100)
                    batterySellLevel = 100;
            }

            double? gridChargeEnable = batteryLevel.HasValue
                ? ((hourSchedule.IsGridChargeEnabled ?? false) ? 1.0 : 0.0)
                : null;

            PointValueStoreHelpers.Fill15Minutes(gridChargeEnableValues, i, time, gridChargeEnable);
            PointValueStoreHelpers.Fill15Minutes(batteryLevelValues, i, time, batteryLevel);
            PointValueStoreHelpers.Fill15Minutes(batterySellLevelValues, i, time, batterySellLevel);

            double? adaptiveChargeEnable = batteryLevel.HasValue
                ? ((hourSchedule.IsAdaptiveSellEnabled ?? false) ? 1.0 : 0.0)
                : null;

            PointValueStoreHelpers.Fill15Minutes(adaptiveSellEnableValues, i, time, adaptiveChargeEnable);
        }

        var baseUrl = configuration["PointValueStoreConnectorUrl"];

        await PointValueStoreHelpers.UpdatePoints(gridChargeEnablePoint, gridChargeEnableValues, baseUrl, httpClient);
        await PointValueStoreHelpers.UpdatePoints(batteryLevelPoint, batteryLevelValues, baseUrl, httpClient);
        await PointValueStoreHelpers.UpdatePoints(batterySellLevelPoint, batterySellLevelValues, baseUrl, httpClient);
        await PointValueStoreHelpers.UpdatePoints(adaptiveSellEnablePoint, adaptiveSellEnableValues, baseUrl, httpClient);

        return Ok();
    }
}
