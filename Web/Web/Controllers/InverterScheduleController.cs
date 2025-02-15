using CommonLibrary.Helpers;
using Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Web.Client.DTOs;

namespace Web.Controllers;

[Route("api/[controller]")]
[ApiController]
public class InverterScheduleController(HomeSystemContext context, HttpClient httpClient, IConfiguration configuration) : ControllerBase
{
    [HttpGet("{date}")]
    public async Task<ActionResult<DayScheduleDto>> Get(DateOnly date)
    {
        var points = await context.DevicePoints.AsNoTrackingWithIdentityResolution().Where(x => x.Device.Type == "inverter_schedule").ToArrayAsync();
        if (points.Length < 2)
        {
            return NotFound("Schedule points have not been configured");
        }

        var batteryLevelPoint = points.FirstOrDefault(x => x.Address == "battery-level");
        var gridChargeEnablePoint = points.FirstOrDefault(x => x.Address == "grid-charge-enable");

        if (batteryLevelPoint == null || gridChargeEnablePoint == null)
        {
            return NotFound("Schedule points have not been configured");
        }

        var batteryLevelValues = await httpClient.GetFromJsonAsync<ValueContainerDto>(DevicePointsController.GetPointValueRequestUrl(configuration, batteryLevelPoint.Id, date, date));
        var gridChargeEnableValues = await httpClient.GetFromJsonAsync<ValueContainerDto>(DevicePointsController.GetPointValueRequestUrl(configuration, gridChargeEnablePoint.Id, date, date));

        if (batteryLevelValues == null || batteryLevelValues.Values == null || batteryLevelValues.Values.Length != 24 * 6 + 1)
        {
            return BadRequest("Battery level values could not be retrieved");
        }

        if (gridChargeEnableValues == null || gridChargeEnableValues.Values == null || gridChargeEnableValues.Values.Length != 24 * 6 + 1)
        {
            return BadRequest("Grid charge enable values could not be retrieved");
        }

        var result = new DayScheduleDto { Hours = new HourlyScheduleDto[24] };
        for (int i = 0; i < 24; i++)
        {
            var isGridChargeEnabled = gridChargeEnableValues.Values.FirstOrDefault(x => x.Timestamp.Hour == i)?.Value;
            var hour = new HourlyScheduleDto
            {
                IsGridChargeEnabled = isGridChargeEnabled.HasValue ? isGridChargeEnabled.Value > 0.0 : null,
                BatteryLevel = (int?)batteryLevelValues.Values.FirstOrDefault(x => x.Timestamp.Hour == i)?.Value,
            };
            result.Hours[i] = hour;
        }

        return result;
    }

    [HttpPut("{date}")]
    public async Task<ActionResult> Put(DateOnly date, DayScheduleDto daySchedule)
    {
        if (daySchedule == null || daySchedule.Hours == null || daySchedule.Hours.Length != 24)
        {
            return BadRequest("Invalid schedule");
        }

        var hours = daySchedule.Hours.OrderBy(x => x.Hour).ToArray();
        for (int i = 0; i < 24; i++)
        {
            if (hours[i].Hour != i)
            {
                return BadRequest("Invalid schedule");
            }
        }

        var points = await context.DevicePoints.AsNoTrackingWithIdentityResolution().Where(x => x.Device.Type == "inverter_schedule").ToArrayAsync();
        if (points.Length < 2)
        {
            return NotFound("Schedule points have not been configured");
        }

        var batteryLevelPoint = points.FirstOrDefault(x => x.Address == "battery-level");
        var gridChargeEnablePoint = points.FirstOrDefault(x => x.Address == "grid-charge-enable");

        if (batteryLevelPoint == null || gridChargeEnablePoint == null)
        {
            return NotFound("Schedule points have not been configured");
        }

        var gridChargeEnableValues = new ValueContainerDto { Values = new NumericValueDto[24 * 6] };
        var batteryLevelValues = new ValueContainerDto { Values = new NumericValueDto[24 * 6] };

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

            gridChargeEnableValues.Values[i * 6] = new NumericValueDto { Timestamp = time, Value = gridChargeEnable };
            gridChargeEnableValues.Values[i * 6 + 1] = new NumericValueDto { Timestamp = time.AddMinutes(10), Value = gridChargeEnable };
            gridChargeEnableValues.Values[i * 6 + 2] = new NumericValueDto { Timestamp = time.AddMinutes(20), Value = gridChargeEnable };
            gridChargeEnableValues.Values[i * 6 + 3] = new NumericValueDto { Timestamp = time.AddMinutes(30), Value = gridChargeEnable };
            gridChargeEnableValues.Values[i * 6 + 4] = new NumericValueDto { Timestamp = time.AddMinutes(40), Value = gridChargeEnable };
            gridChargeEnableValues.Values[i * 6 + 5] = new NumericValueDto { Timestamp = time.AddMinutes(50), Value = gridChargeEnable };

            batteryLevelValues.Values[i * 6] = new NumericValueDto { Timestamp = time, Value = batteryLevel };
            batteryLevelValues.Values[i * 6 + 1] = new NumericValueDto { Timestamp = time.AddMinutes(10), Value = batteryLevel };
            batteryLevelValues.Values[i * 6 + 2] = new NumericValueDto { Timestamp = time.AddMinutes(20), Value = batteryLevel };
            batteryLevelValues.Values[i * 6 + 3] = new NumericValueDto { Timestamp = time.AddMinutes(30), Value = batteryLevel };
            batteryLevelValues.Values[i * 6 + 4] = new NumericValueDto { Timestamp = time.AddMinutes(40), Value = batteryLevel };
            batteryLevelValues.Values[i * 6 + 5] = new NumericValueDto { Timestamp = time.AddMinutes(50), Value = batteryLevel };
        }

        var baseUrl = configuration["PointValueStoreConnectorUrl"];
        var url = UrlHelpers.GetUrl(baseUrl!, $"points/{gridChargeEnablePoint.Id}/values", null);
        var response = await httpClient.PutAsJsonAsync(url, gridChargeEnableValues);
        if (!response.IsSuccessStatusCode)
        {
            return BadRequest("The request to point value store failed with code " + response.StatusCode);
        }

        url = UrlHelpers.GetUrl(baseUrl!, $"points/{batteryLevelPoint.Id}/values", null);
        response = await httpClient.PutAsJsonAsync(url, batteryLevelValues);
        if (!response.IsSuccessStatusCode)
        {
            return BadRequest("The request to point value store failed with code " + response.StatusCode);
        }

        return Ok();
    }
}
