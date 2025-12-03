using Domain;
using Microsoft.EntityFrameworkCore;
using MyUplinkConnector;
using MyUplinkConnector.Client.V2.Devices.Item.Points;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using ValueReaderService.Services.AirobotThermostat;

namespace ValueReaderService.Services;

public class HeatPumpScheduleRunner(
    ILogger<DeviceReader> logger,
    HomeSystemContext dbContext,
    PointValueStoreAdapter pointValueStoreAdapter,
    IHttpClientFactory httpClientFactory,
    AirobotThermostatReader airobotThermostatReader) : DeviceReader(logger)
{
    protected override async Task<IList<PointValue>?> ExecuteAsyncInternal(Device device, DateTime timestamp, ICollection<DevicePoint> devicePoints)
    {
#if !DEBUG
        await Task.Delay(TimeSpan.FromSeconds(10));
#endif

        var timestampLocal = timestamp.ToLocalTime().AddSeconds(30);
        var date = DateOnly.FromDateTime(timestampLocal);

        var devices = await dbContext.Devices.AsNoTrackingWithIdentityResolution()
            .Include(x => x.DevicePoints)
            .ThenInclude(x => x.EnumMembers)
            .Where(x => x.Type == "heat_pump" || x.Type == "airobot_thermostat")
            .ToListAsync();

        var heatPumpDevice = devices.FirstOrDefault(x => x.Type == "heat_pump")
            ?? throw new InvalidOperationException("Heat pump device not found");

        var livingRoomThermostat = devices.FirstOrDefault(x => x.Type == "airobot_thermostat" && x.SubType == "living-room");

        if (heatPumpDevice.Address is null)
            throw new InvalidOperationException("Heat pump device address is not set");

        var address = JsonSerializer.Deserialize<DeviceAddress>(heatPumpDevice.Address);
        if (address is null || address.DeviceId is null || address.ClientId is null || address.ClientSecret is null)
            throw new InvalidOperationException("Heat pump device address is invalid");

        var heatingOffset = new DeviceValue(-10, 10);
        var heatingDegreeMinutes = new DeviceValue(-300, 300);
        var hotWaterMode = new DeviceValue(0, 2);
        var thermostatMode = new DeviceValue(0, 1);

        var actualHeatingOffsetPoint = heatPumpDevice.DevicePoints.FirstOrDefault(x => x.Type == "heating-offset");
        var actualHotWaterModePoint = heatPumpDevice.DevicePoints.FirstOrDefault(x => x.Type == "hot-water-mode");
        var heatingDegreeMinutesPoint = heatPumpDevice.DevicePoints.FirstOrDefault(x => x.Type == "heating-degree-minutes");
        var statusPoint = heatPumpDevice.DevicePoints.FirstOrDefault(x => x.Type == "status");
        var heatingValue = GetEnumValue(statusPoint?.EnumMembers, "heating");

        var thermostatModePoint = livingRoomThermostat?.DevicePoints.FirstOrDefault(x => x.Type == "mode");
        var comfortValue = GetEnumValue(thermostatModePoint?.EnumMembers, "comfort");
        var awayValue = GetEnumValue(thermostatModePoint?.EnumMembers, "away");

        if (actualHeatingOffsetPoint != null && actualHotWaterModePoint != null && heatingDegreeMinutesPoint != null && statusPoint != null)
        {
            var heatingOffsetPoint = devicePoints.FirstOrDefault(x => x.Address == "heating-offset");
            var hotWaterModePoint = devicePoints.FirstOrDefault(x => x.Address == "hot-water-mode");
            if (heatingOffsetPoint != null && hotWaterModePoint != null)
            {
                var actualHeatingOffsetValues = await pointValueStoreAdapter.Get(actualHeatingOffsetPoint.Id, date);
                var actualHotWaterModeValues = await pointValueStoreAdapter.Get(actualHotWaterModePoint.Id, date);
                var heatingDegreeMinutesValues = await pointValueStoreAdapter.Get(heatingDegreeMinutesPoint.Id, date);
                var statusValues = await pointValueStoreAdapter.Get(statusPoint.Id, date);

                var heatingOffsetValues = await pointValueStoreAdapter.Get(heatingOffsetPoint.Id, date);
                var hotWaterModeValues = await pointValueStoreAdapter.Get(hotWaterModePoint.Id, date);

                var thermostatModeValues = thermostatModePoint != null
                    ? await pointValueStoreAdapter.Get(thermostatModePoint.Id, date)
                    : null;

                heatingOffset.CurrentValue = PointValueStoreAdapter.GetCurrentValue(timestampLocal, actualHeatingOffsetValues);
                heatingOffset.NewValue = PointValueStoreAdapter.GetCurrentValue(timestampLocal, heatingOffsetValues);

                hotWaterMode.CurrentValue = PointValueStoreAdapter.GetCurrentValue(timestampLocal, actualHotWaterModeValues);
                hotWaterMode.NewValue = PointValueStoreAdapter.GetCurrentValue(timestampLocal, hotWaterModeValues);

                heatingDegreeMinutes.CurrentValue = PointValueStoreAdapter.GetCurrentValue(timestampLocal, heatingDegreeMinutesValues);

                if (heatingOffset.HasChanged && heatingOffset.NewValue.Value >= 0 && heatingOffset.CurrentValue.Value < 0 && heatingDegreeMinutes.CurrentValue > 0)
                {
                    heatingDegreeMinutes.NewValue = 0;
                }
                else if (heatingOffset.HasChanged && heatingOffset.NewValue.Value < 0 && heatingOffset.CurrentValue.Value >= 0 && heatingDegreeMinutes.CurrentValue < 0)
                {
                    heatingDegreeMinutes.NewValue = 0;
                }

                if (thermostatModeValues != null && heatingValue != null)
                {
                    thermostatMode.CurrentValue = PointValueStoreAdapter.GetCurrentValue(timestampLocal, thermostatModeValues);
                    var statusValue = PointValueStoreAdapter.GetCurrentValue(timestampLocal, statusValues);
                    if (statusValue != null && thermostatMode.CurrentValue != null && comfortValue != null && awayValue != null)
                    {
                        thermostatMode.NewValue = (int)statusValue == heatingValue.Value ? comfortValue : awayValue;
                    }
                }
            }
        }

        if (heatingOffset.HasChanged || hotWaterMode.HasChanged)
        {
            using var httpClient = httpClientFactory.CreateClient(nameof(MyUplinkDeviceReader));
            var client = await MyUplinkClientFactory.Create(httpClient, address.ClientId, address.ClientSecret)
                ?? throw new InvalidOperationException("MyUplink client could not be created");

            var body = new PointsPatchRequestBody();
            if (heatingOffset.HasChanged)
            {
                body.AdditionalData.Add(actualHeatingOffsetPoint!.Address, (int)heatingOffset.NewValue.Value);
            }
            if (hotWaterMode.HasChanged)
            {
                body.AdditionalData.Add(actualHotWaterModePoint!.Address, (int)hotWaterMode.NewValue.Value);
            }
            if (heatingDegreeMinutes.HasChanged)
            {
                body.AdditionalData.Add(heatingDegreeMinutesPoint!.Address, (int)heatingDegreeMinutes.NewValue.Value);
            }

            await client.V2.Devices[address.DeviceId].Points.PatchAsync(body);
        }

        if (thermostatMode.HasChanged)
        {
            await airobotThermostatReader.WriteMode(livingRoomThermostat!, (int)thermostatMode.NewValue == comfortValue!.Value ? AirobotThermostatMode.Home : AirobotThermostatMode.Away);
        }

        return null;
    }

    private static int? GetEnumValue(ICollection<EnumMember>? enumMembers, string type)
    {
        if (enumMembers == null)
            return null;

        var orderedMembers = enumMembers.OrderBy(x => x.Value).ToList();

        for (int i = 0; i < orderedMembers.Count; i++)
        {
            if (orderedMembers[i].Type == type)
                return i;
        }

        return null;
    }

    private struct DeviceValue(double min, double max)
    {
        private double? _NewValue;

        public double? CurrentValue { get; set; }
        public double? NewValue
        {
            readonly get => _NewValue;
            set
            {
                if (value is not null)
                {
                    if (value.Value < min)
                        _NewValue = min;
                    else if (value.Value > max)
                        _NewValue = max;
                    else
                        _NewValue = value.Value;
                }
                else
                {
                    _NewValue = null;
                }
            }
        }

        public readonly double Min => min;
        public readonly double Max => max;

        [MemberNotNullWhen(true, nameof(CurrentValue), nameof(NewValue))]
        public readonly bool IsValid => CurrentValue.HasValue && NewValue.HasValue;

        [MemberNotNullWhen(true, nameof(CurrentValue), nameof(NewValue))]
        public readonly bool HasChanged => IsValid && CurrentValue.Value != NewValue.Value;
    }
}
