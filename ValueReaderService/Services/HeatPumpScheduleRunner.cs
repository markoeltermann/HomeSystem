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
    AirobotThermostatReader airobotThermostatReader,
    ShellyDeviceReader shellyDeviceReader) : DeviceReader(logger)
{

    private DeviceValue heatingOffset = new(-10, 10);
    private DeviceValue heatingDegreeMinutes = new(-300, 300);
    private DeviceValue hotWaterMode = new(0, 2);
    private DeviceValue thermostatMode = new(0, 1);
    private DeviceValue valveSignal = new(0, 100);

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
            .Where(x => x.Type == "heat_pump" || x.Type == "airobot_thermostat" || x.Type == "yrno_weather_forecast" || (x.Type == "shelly" && x.SubType == "heat-distribution-controller"))
            .ToListAsync();

        var heatPumpDevice = devices.FirstOrDefault(x => x.Type == "heat_pump")
            ?? throw new InvalidOperationException("Heat pump device not found");

        var livingRoomThermostat = devices.FirstOrDefault(x => x.Type == "airobot_thermostat" && x.SubType == "living-room");

        var weatherForecastDevice = devices.FirstOrDefault(x => x.Type == "yrno_weather_forecast");

        var heatDistributionControllerDevice = devices.FirstOrDefault(x => x.Type == "shelly" && x.SubType == "heat-distribution-controller");

        if (heatPumpDevice.Address is null)
            throw new InvalidOperationException("Heat pump device address is not set");

        var address = JsonSerializer.Deserialize<DeviceAddress>(heatPumpDevice.Address);
        if (address is null || address.DeviceId is null || address.ClientId is null || address.ClientSecret is null)
            throw new InvalidOperationException("Heat pump device address is invalid");

        var heatingOffsetPoint = GetPointByType(heatPumpDevice, "heating-offset");
        var hotWaterModePoint = GetPointByType(heatPumpDevice, "hot-water-mode");
        var heatingDegreeMinutesPoint = GetPointByType(heatPumpDevice, "heating-degree-minutes");
        var statusPoint = GetPointByType(heatPumpDevice, "status");
        var heatingEnumValue = GetRequiredEnumValue(statusPoint.EnumMembers, "heating");
        var defrostingEnumValue = GetRequiredEnumValue(statusPoint.EnumMembers, "defrosting");

        var thermostatModePoint = livingRoomThermostat?.DevicePoints.FirstOrDefault(x => x.Type == "mode");
        var comfortEnumValue = GetEnumValue(thermostatModePoint?.EnumMembers, "comfort");
        var awayEnumValue = GetEnumValue(thermostatModePoint?.EnumMembers, "away");

        var heatingOffsetSchedulePoint = GetPointByType(devicePoints, "heating-offset-schedule");
        var hotWaterModeSchedulePoint = GetPointByType(devicePoints, "hot-water-mode-schedule");

        var actualHeatingOffsetValues = await pointValueStoreAdapter.Get(heatingOffsetPoint.Id, date);
        var actualHotWaterModeValues = await pointValueStoreAdapter.Get(hotWaterModePoint.Id, date);
        var heatingDegreeMinutesValues = await pointValueStoreAdapter.Get(heatingDegreeMinutesPoint.Id, date);
        var statusValues = await pointValueStoreAdapter.Get(statusPoint.Id, date);
        var statusValue = PointValueStoreAdapter.GetCurrentValue(timestampLocal, statusValues);

        var heatingOffsetScheduleValues = await pointValueStoreAdapter.Get(heatingOffsetSchedulePoint.Id, date);
        var hotWaterModeScheduleValues = await pointValueStoreAdapter.Get(hotWaterModeSchedulePoint.Id, date);

        var thermostatModeValues = thermostatModePoint != null
            ? await pointValueStoreAdapter.Get(thermostatModePoint.Id, date)
            : null;

        heatingOffset.CurrentValue = PointValueStoreAdapter.GetCurrentValue(timestampLocal, actualHeatingOffsetValues);
        heatingOffset.NewValue = PointValueStoreAdapter.GetCurrentValue(timestampLocal, heatingOffsetScheduleValues);

        hotWaterMode.CurrentValue = PointValueStoreAdapter.GetCurrentValue(timestampLocal, actualHotWaterModeValues);
        hotWaterMode.NewValue = PointValueStoreAdapter.GetCurrentValue(timestampLocal, hotWaterModeScheduleValues);

        heatingDegreeMinutes.CurrentValue = PointValueStoreAdapter.GetCurrentValue(timestampLocal, heatingDegreeMinutesValues);

        if (heatingOffset.HasChanged && heatingOffset.NewValue.Value >= 0 && heatingOffset.CurrentValue.Value < 0 && heatingDegreeMinutes.CurrentValue > 0)
        {
            heatingDegreeMinutes.NewValue = 0;
        }
        else if (heatingOffset.HasChanged && heatingOffset.NewValue.Value < 0 && heatingOffset.CurrentValue.Value >= 0 && heatingDegreeMinutes.CurrentValue < 0)
        {
            heatingDegreeMinutes.NewValue = 0;
        }

        if (thermostatModeValues != null && heatingOffset.CurrentValue.HasValue)
        {
            thermostatMode.CurrentValue = PointValueStoreAdapter.GetCurrentValue(timestampLocal, thermostatModeValues);
            if (statusValue != null && thermostatMode.CurrentValue != null && comfortEnumValue != null && awayEnumValue != null)
            {
                if (heatingOffset.CurrentValue.Value >= 0)
                {
                    if ((int)thermostatMode.CurrentValue.Value == comfortEnumValue)
                        thermostatMode.NewValue = comfortEnumValue;
                    else
                        thermostatMode.NewValue = (int)statusValue == heatingEnumValue ? comfortEnumValue : awayEnumValue;
                }
                else
                {
                    thermostatMode.NewValue = awayEnumValue;
                }
            }
        }

        if (heatDistributionControllerDevice != null && weatherForecastDevice != null)
        {
            var valveSignalPoint = GetPointByType(heatDistributionControllerDevice, "valve-signal");
            var valveSignalValues = await pointValueStoreAdapter.Get(valveSignalPoint.Id, date);
            valveSignal.CurrentValue = PointValueStoreAdapter.GetCurrentValue(timestampLocal, valveSignalValues);

            var airTemperatureForecastPoint = GetPointByType(weatherForecastDevice, "air-temperature");
            var airTemperatureForecastValues = await pointValueStoreAdapter.Get(airTemperatureForecastPoint.Id, date, date.AddDays(1));

            var after24Hours = timestamp.AddHours(24);
            var upcomingAirTemperatureValues = airTemperatureForecastValues.Values!
                .Where(x => x.Value.HasValue)
                .Select(x => (x.Timestamp.UtcDateTime, x.Value!.Value))
                .Where(x => x.UtcDateTime >= timestamp && x.UtcDateTime < after24Hours)
                .GroupBy(x => (x.UtcDateTime - timestamp).Hours)
                .Select(g => g.First())
                .ToList();

            if (upcomingAirTemperatureValues.Count == 24)
            {
                var upcomingMeanTemperature = upcomingAirTemperatureValues.Average(x => x.Value);
                if (upcomingMeanTemperature > 5)
                {
                    valveSignal.NewValue = 100;
                }
                else if (statusValue != null)
                {
                    if ((int)statusValue == heatingEnumValue)
                    {
                        valveSignal.NewValue = 96;
                    }
                    else if ((int)statusValue != defrostingEnumValue)
                    {
                        valveSignal.NewValue = 100;
                    }
                }
            }
        }

        await SendHeatPumpCommands(address, heatingOffsetPoint, hotWaterModePoint, heatingDegreeMinutesPoint);

        await SendLivingRoomThermostatCommands(livingRoomThermostat, comfortEnumValue);

        await SendHeatDistributionCommands(heatDistributionControllerDevice);

        return null;
    }

    private async Task SendHeatDistributionCommands(Device? heatDistributionControllerDevice)
    {
        if (valveSignal.HasChanged)
        {
            await shellyDeviceReader.SendLightSetCommandAsync(heatDistributionControllerDevice!, 0, true, (int)valveSignal.NewValue.Value);
        }
    }

    private async Task SendLivingRoomThermostatCommands(Device? livingRoomThermostat, int? comfortEnumValue)
    {
        if (thermostatMode.HasChanged)
        {
            await airobotThermostatReader.WriteMode(livingRoomThermostat!, (int)thermostatMode.NewValue == comfortEnumValue!.Value ? AirobotThermostatMode.Home : AirobotThermostatMode.Away);
        }
    }

    private static DevicePoint GetPointByType(Device device, string type)
    {
        return device.DevicePoints.FirstOrDefault(x => x.Type == type)
            ?? throw new InvalidOperationException($"Device point of type '{type}' not found for device id {device.Id}.");
    }

    private static DevicePoint GetPointByType(ICollection<DevicePoint> devicePoints, string type)
    {
        return devicePoints.FirstOrDefault(x => x.Type == type)
            ?? throw new InvalidOperationException($"Device point of type '{type}' not found.");
    }

    private async Task SendHeatPumpCommands(DeviceAddress address, DevicePoint heatingOffsetPoint, DevicePoint hotWaterModePoint, DevicePoint heatingDegreeMinutesPoint)
    {
        if (heatingOffset.HasChanged || hotWaterMode.HasChanged)
        {
            using var httpClient = httpClientFactory.CreateClient(nameof(MyUplinkDeviceReader));
            var client = await MyUplinkClientFactory.Create(httpClient, address.ClientId!, address.ClientSecret!)
                ?? throw new InvalidOperationException("MyUplink client could not be created");

            var body = new PointsPatchRequestBody();
            if (heatingOffset.HasChanged)
            {
                body.AdditionalData.Add(heatingOffsetPoint.Address, (int)heatingOffset.NewValue.Value);
            }
            if (hotWaterMode.HasChanged)
            {
                body.AdditionalData.Add(hotWaterModePoint.Address, (int)hotWaterMode.NewValue.Value);
            }
            if (heatingDegreeMinutes.HasChanged)
            {
                body.AdditionalData.Add(heatingDegreeMinutesPoint.Address, (int)heatingDegreeMinutes.NewValue.Value);
            }

            await client.V2.Devices[address.DeviceId].Points.PatchAsync(body);
        }
    }

    private static int GetRequiredEnumValue(ICollection<EnumMember>? enumMembers, string type)
    {
        var value = GetEnumValue(enumMembers, type);
        return value ?? throw new InvalidOperationException($"Enum member with type '{type}' is missing.");
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
