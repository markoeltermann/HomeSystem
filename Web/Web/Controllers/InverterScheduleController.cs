using Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Web.Client.DTOs;
using Web.Helpers;

namespace Web.Controllers;

[Route("api/[controller]")]
[ApiController]
public class InverterScheduleController(HomeSystemContext context, HttpClient httpClient, IConfiguration configuration) : ControllerBase
{
    [HttpGet("{date}")]
    public async Task<ActionResult<InverterDayScheduleDto>> Get(DateOnly date)
    {
        var points = await context.DevicePoints.AsNoTrackingWithIdentityResolution().Where(x => x.Device.Type == "inverter_schedule").ToArrayAsync();
        if (points.Length < 3)
        {
            return NotFound("Schedule points have not been configured");
        }

        var baseUrl = configuration["PointValueStoreConnectorUrl"];

        var batteryLevelValues = await PointValueStoreHelpers.GetPointValues(points, "battery-level", date, httpClient, baseUrl);
        var gridChargeEnableValues = await PointValueStoreHelpers.GetPointValues(points, "grid-charge-enable", date, httpClient, baseUrl);
        var adaptiveSellEnableValues = await PointValueStoreHelpers.GetPointValues(points, "adaptive-sell-enable", date, httpClient, baseUrl);

        var result = new InverterDayScheduleDto { Hours = new InverterHourlyScheduleDto[24] };
        for (int i = 0; i < 24; i++)
        {
            var isGridChargeEnabled = gridChargeEnableValues.Values.FirstOrDefault(x => x.Timestamp.Hour == i)?.Value;
            var isAdaptiveSellEnabled = adaptiveSellEnableValues.Values.FirstOrDefault(x => x.Timestamp.Hour == i)?.Value;
            var hour = new InverterHourlyScheduleDto
            {
                IsGridChargeEnabled = isGridChargeEnabled.HasValue ? isGridChargeEnabled.Value > 0.0 : null,
                BatteryLevel = (int?)batteryLevelValues.Values.FirstOrDefault(x => x.Timestamp.Hour == i)?.Value,
                IsAdaptiveSellEnabled = isAdaptiveSellEnabled.HasValue ? isAdaptiveSellEnabled.Value > 0.0 : null
            };
            result.Hours[i] = hour;
        }

        return result;
    }

    [HttpPut("{date}")]
    public async Task<ActionResult> Put(DateOnly date, InverterDayScheduleDto daySchedule)
    {
        DayScheduleHelpers.ValidateDaySchedule(daySchedule);

        var points = await context.DevicePoints.AsNoTrackingWithIdentityResolution().Where(x => x.Device.Type == "inverter_schedule").ToArrayAsync();
        if (points.Length < 3)
        {
            return NotFound("Schedule points have not been configured");
        }

        var batteryLevelPoint = points.FirstOrDefault(x => x.Address == "battery-level");
        var gridChargeEnablePoint = points.FirstOrDefault(x => x.Address == "grid-charge-enable");
        var adaptiveSellEnablePoint = points.FirstOrDefault(x => x.Address == "adaptive-sell-enable");

        if (batteryLevelPoint == null || gridChargeEnablePoint == null || adaptiveSellEnablePoint == null)
        {
            return NotFound("Schedule points have not been configured");
        }

        var gridChargeEnableValues = new ValueContainerDto { Values = new NumericValueDto[24 * 6] };
        var batteryLevelValues = new ValueContainerDto { Values = new NumericValueDto[24 * 6] };
        var adaptiveSellEnableValues = new ValueContainerDto { Values = new NumericValueDto[24 * 6] };

        var time0 = date.ToDateTime(new TimeOnly(), DateTimeKind.Local).ToUniversalTime();

        for (int i = 0; i < 24; i++)
        {
            var time = time0.AddHours(i);
            var hourSchedule = daySchedule.Hours[i];
            if (i != 0 && hourSchedule.BatteryLevel == null)
            {
                var prevHourSchedule = daySchedule.Hours[i - 1];
                hourSchedule.BatteryLevel = prevHourSchedule.BatteryLevel;
                hourSchedule.IsGridChargeEnabled = prevHourSchedule.IsGridChargeEnabled;
                hourSchedule.IsAdaptiveSellEnabled = prevHourSchedule.IsAdaptiveSellEnabled;
            }

            double? batteryLevel = hourSchedule.BatteryLevel;
            if (batteryLevel.HasValue)
            {
                if (batteryLevel < 0)
                    batteryLevel = 0;
                else if (batteryLevel > 100)
                    batteryLevel = 100;
            }
            double? gridChargeEnable = batteryLevel.HasValue
                ? ((hourSchedule.IsGridChargeEnabled ?? false) ? 1.0 : 0.0)
                : null;

            PointValueStoreHelpers.FillHour(gridChargeEnableValues, i, time, gridChargeEnable);
            PointValueStoreHelpers.FillHour(batteryLevelValues, i, time, batteryLevel);

            double? adaptiveChargeEnable = batteryLevel.HasValue
                ? ((hourSchedule.IsAdaptiveSellEnabled ?? false) ? 1.0 : 0.0)
                : null;

            PointValueStoreHelpers.FillHour(adaptiveSellEnableValues, i, time, adaptiveChargeEnable);
        }

        var baseUrl = configuration["PointValueStoreConnectorUrl"];

        await PointValueStoreHelpers.UpdatePoints(gridChargeEnablePoint, gridChargeEnableValues, baseUrl, httpClient);
        await PointValueStoreHelpers.UpdatePoints(batteryLevelPoint, batteryLevelValues, baseUrl, httpClient);
        await PointValueStoreHelpers.UpdatePoints(adaptiveSellEnablePoint, adaptiveSellEnableValues, baseUrl, httpClient);

        return Ok();
    }
}
