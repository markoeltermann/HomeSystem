using CommonLibrary.Extensions;
using Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Web.Client.DTOs;
using Web.Helpers;

namespace Web.Controllers;

[Route("api/[controller]")]
[ApiController]
public class HeatPumpScheduleController(HomeSystemContext context, HttpClient httpClient, IConfiguration configuration) : ControllerBase
{
    [HttpGet("{date}")]
    public async Task<ActionResult<HeatPumpDayScheduleDto>> Get(DateOnly date)
    {
        var points = await context.DevicePoints.AsNoTrackingWithIdentityResolution().Where(x => x.Device.Type == "heat_pump_schedule").ToArrayAsync();
        if (points.Length < 2)
        {
            return NotFound("Schedule points have not been configured");
        }
        var baseUrl = configuration["PointValueStoreConnectorUrl"];

        var heatingOffsetValues = await PointValueStoreHelpers.GetPointValues(points, "heating-offset", date, httpClient, baseUrl);
        var hotWaterModeValues = await PointValueStoreHelpers.GetPointValues(points, "hot-water-mode", date, httpClient, baseUrl);

        var result = new HeatPumpDayScheduleDto { Entries = new HeatPumpHourlyScheduleDto[24] };
        for (int i = 0; i < 24; i++)
        {
            var heatingOffset = heatingOffsetValues.Values.FirstOrDefault(x => x.Timestamp.Hour == i)?.Value;
            var hotWaterMode = hotWaterModeValues.Values.FirstOrDefault(x => x.Timestamp.Hour == i)?.Value;
            var hour = new HeatPumpHourlyScheduleDto
            {
                HeatingOffset = (int?)heatingOffset,
                HotWaterMode = (int?)hotWaterMode,
            };
            result.Entries[i] = hour;
        }

        return result;
    }

    [HttpPut("{date}")]
    public async Task<ActionResult> Put(DateOnly date, HeatPumpDayScheduleDto daySchedule)
    {
        DayScheduleHelpers.ValidateDaySchedule(daySchedule, false);

        var points = await context.DevicePoints.AsNoTrackingWithIdentityResolution().Where(x => x.Device.Type == "heat_pump_schedule").ToArrayAsync();
        if (points.Length < 2)
        {
            return NotFound("Schedule points have not been configured");
        }

        var heatingOffsetPoint = points.FirstOrDefault(x => x.Address == "heating-offset");
        var hotWaterModePoint = points.FirstOrDefault(x => x.Address == "hot-water-mode");

        if (heatingOffsetPoint == null || hotWaterModePoint == null)
        {
            return NotFound("Schedule points have not been configured");
        }

        var hoursPerDate = (int)(date.ToDateTime(new TimeOnly(), DateTimeKind.Local).AddDays(1).ToUniversalTime()
            - date.ToDateTime(new TimeOnly(), DateTimeKind.Local).ToUniversalTime()).TotalHours;

        var heatingOffsetValues = new ValueContainerDto { Values = new NumericValueDto[hoursPerDate * 6] };
        var hotWaterModeValues = new ValueContainerDto { Values = new NumericValueDto[hoursPerDate * 6] };

        var time0 = date.ToDateTime(new TimeOnly(), DateTimeKind.Local).ToUniversalTime();
        var entries = daySchedule.Entries;
        if (hoursPerDate == 25)
        {
            entries = [.. entries[0..4], entries[3], .. entries[4..]];
        }
        else if (hoursPerDate == 23)
        {
            entries = [.. entries[0..3], .. entries[4..]];
        }

        for (int i = 0; i < hoursPerDate; i++)
        {
            var time = time0.AddHours(i);
            var hourSchedule = entries[i];
            if (i == 0)
            {
                hourSchedule.HeatingOffset = hourSchedule.HeatingOffset?.Truncate(-10, 10) ?? 0;
                hourSchedule.HotWaterMode = hourSchedule.HotWaterMode?.Truncate(0, 2) ?? 1;
            }
            else
            {
                var prevHourSchedule = entries[i - 1];
                hourSchedule.HeatingOffset = hourSchedule.HeatingOffset?.Truncate(-10, 10) ?? prevHourSchedule.HeatingOffset;
                hourSchedule.HotWaterMode = hourSchedule.HotWaterMode?.Truncate(0, 2) ?? prevHourSchedule.HotWaterMode;
            }

            PointValueStoreHelpers.FillHour(heatingOffsetValues, i, time, hourSchedule.HeatingOffset);
            PointValueStoreHelpers.FillHour(hotWaterModeValues, i, time, hourSchedule.HotWaterMode);
        }

        var baseUrl = configuration["PointValueStoreConnectorUrl"];

        await PointValueStoreHelpers.UpdatePoints(heatingOffsetPoint, heatingOffsetValues, baseUrl, httpClient);
        await PointValueStoreHelpers.UpdatePoints(hotWaterModePoint, hotWaterModeValues, baseUrl, httpClient);

        return Ok();
    }
}
