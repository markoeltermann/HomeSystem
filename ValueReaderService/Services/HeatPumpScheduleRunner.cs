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
    ShellyDeviceReader shellyDeviceReader,
    IConfigurationStore<ConfigPointModel> configStore) : DeviceReader(logger, dbContext)
{
    [DevicePointAutoLoad("heat_pump", null, "heating-offset")]
    private DevicePoint HeatingOffsetPoint { get; set; } = null!;

    [DevicePointAutoLoad("heat_pump", null, "hot-water-mode")]
    private DevicePoint HotWaterModePoint { get; set; } = null!;

    [DevicePointAutoLoad("heat_pump", null, "heating-degree-minutes")]
    private DevicePoint HeatingDegreeMinutesPoint { get; set; } = null!;

    [DevicePointAutoLoad("heat_pump", null, "cooling-degree-minutes")]
    private DevicePoint CoolingDegreeMinutesPoint { get; set; } = null!;

    [DevicePointAutoLoad("heat_pump", null, "status")]
    private DevicePoint StatusPoint { get; set; } = null!;

    [DevicePointAutoLoad("heat_pump", null, "floor-circuit-supply-temperature")]
    private DevicePoint FloorCircuitSupplyTemperaturePoint { get; set; } = null!;

    [DevicePointAutoLoad("heat_pump", null, "heat-pump-supply-temperature")]
    private DevicePoint HeatPumpSupplyTemperaturePoint { get; set; } = null!;

    [DevicePointAutoLoad("airobot_thermostat", "living-room", "mode")]
    private DevicePoint ThermostatModePoint { get; set; } = null!;

    [DevicePointAutoLoad("yrno_weather_forecast", null, "air-temperature")]
    private DevicePoint AirTemperatureForecastPoint { get; set; } = null!;

    private DeviceValue heatingOffset = new(-10, 10);
    private DeviceValue heatingDegreeMinutes = new(-300, 300);
    private DeviceValue hotWaterMode = new(0, 2);
    private DeviceValue thermostatMode = new(0, 1);
    private double? floorCircuitSupplyTemperature;
    private double? statusValue;
    private double? currentCoolingDegreeMinutes;
    private double? outdoorTemperature;
    private Device? heatPumpDevice;
    private Device? livingRoomThermostat;
    private Device? heatDistributionControllerDevice;
    private DeviceAddress? heatPumpAddress;
    private HeatDistributionMode? newHeatDistributionMode;
    private double? newHeatDistributionSetpoint;
    private int? newHeatDistributionValue;
    private bool isFloorCoolingEnabled;

    protected override async Task<IList<PointValue>?> ExecuteAsyncInternal(Device device, DateTime timestamp, ICollection<DevicePoint> devicePoints)
    {
#if !DEBUG
        await Task.Delay(TimeSpan.FromSeconds(10));
#endif

        var timestampLocal = timestamp.ToLocalTime().AddSeconds(30);
        var date = DateOnly.FromDateTime(timestampLocal);
        var configPointModel = await configStore.LoadAsync();
        isFloorCoolingEnabled = configPointModel.IsFloorCoolingEnabled;

        var devices = await InitializeDevices();
        EnsureDevicesInitialized();

        var heatingEnumValue = GetRequiredEnumValue(StatusPoint.EnumMembers, "heating");
        var defrostingEnumValue = GetRequiredEnumValue(StatusPoint.EnumMembers, "defrosting");
        var hotWaterEnumValue = GetRequiredEnumValue(StatusPoint.EnumMembers, "hot-water");
        var coolingEnumValue = GetRequiredEnumValue(StatusPoint.EnumMembers, "cooling");

        var comfortEnumValue = GetEnumValue(ThermostatModePoint.EnumMembers, "comfort");
        var awayEnumValue = GetEnumValue(ThermostatModePoint.EnumMembers, "away");

        var heatingOffsetSchedulePoint = GetPointByType(devicePoints, "heating-offset-schedule");
        var hotWaterModeSchedulePoint = GetPointByType(devicePoints, "hot-water-mode-schedule");

        var actualHeatingOffsetValues = await pointValueStoreAdapter.Get(HeatingOffsetPoint.Id, date, fiveMinResolution: true);
        var actualHotWaterModeValues = await pointValueStoreAdapter.Get(HotWaterModePoint.Id, date, fiveMinResolution: true);
        var heatingDegreeMinutesValues = await pointValueStoreAdapter.Get(HeatingDegreeMinutesPoint.Id, date, fiveMinResolution: true);
        var coolingDegreeMinutesValues = await pointValueStoreAdapter.Get(CoolingDegreeMinutesPoint.Id, date, fiveMinResolution: true);
        var statusValues = await pointValueStoreAdapter.Get(StatusPoint.Id, date, fiveMinResolution: true);
        statusValue = PointValueStoreAdapter.GetCurrentValue(timestampLocal, statusValues);
        var floorCircuitSupplyTemperatureValues = await pointValueStoreAdapter.Get(FloorCircuitSupplyTemperaturePoint.Id, date, fiveMinResolution: true);
        floorCircuitSupplyTemperature = PointValueStoreAdapter.GetCurrentValue(timestampLocal, floorCircuitSupplyTemperatureValues);
        var heatPumpSupplyTemperatureValues = await pointValueStoreAdapter.Get(HeatPumpSupplyTemperaturePoint.Id, date, fiveMinResolution: true);
        var heatPumpSupplyTemperature = PointValueStoreAdapter.GetCurrentValue(timestampLocal, heatPumpSupplyTemperatureValues);

        var heatingOffsetScheduleValues = await pointValueStoreAdapter.Get(heatingOffsetSchedulePoint.Id, date, fiveMinResolution: true);
        var hotWaterModeScheduleValues = await pointValueStoreAdapter.Get(hotWaterModeSchedulePoint.Id, date, fiveMinResolution: true);

        var thermostatModeValues = await pointValueStoreAdapter.Get(ThermostatModePoint.Id, date, fiveMinResolution: true);

        outdoorTemperature = await GetOutdoorTemperature(timestampLocal, date, devices);

        heatingOffset.CurrentValue = PointValueStoreAdapter.GetCurrentValue(timestampLocal, actualHeatingOffsetValues);
        heatingOffset.NewValue = PointValueStoreAdapter.GetCurrentValue(timestampLocal, heatingOffsetScheduleValues);

        hotWaterMode.CurrentValue = PointValueStoreAdapter.GetCurrentValue(timestampLocal, actualHotWaterModeValues);
        hotWaterMode.NewValue = PointValueStoreAdapter.GetCurrentValue(timestampLocal, hotWaterModeScheduleValues);

        heatingDegreeMinutes.CurrentValue = PointValueStoreAdapter.GetCurrentValue(timestampLocal, heatingDegreeMinutesValues);

        currentCoolingDegreeMinutes = PointValueStoreAdapter.GetCurrentValue(timestampLocal, coolingDegreeMinutesValues);

        if (heatingOffset.HasChanged && heatingOffset.NewValue.Value >= 0 && heatingOffset.CurrentValue.Value < 0
            && heatingDegreeMinutes.CurrentValue > -60)
        {
            var heatingOffsetDelta = (int)(heatingOffset.NewValue.Value - heatingOffset.CurrentValue.Value);
            var newValue = heatingOffsetDelta * -10;
            if (newValue < -60)
                newValue = -60;
            if (heatingDegreeMinutes.CurrentValue.Value > newValue)
                heatingDegreeMinutes.NewValue = newValue;
        }
        else if (heatingOffset.HasChanged && heatingOffset.NewValue.Value < 0 && heatingOffset.CurrentValue.Value >= 0 && heatingDegreeMinutes.CurrentValue < 0)
        {
            heatingDegreeMinutes.NewValue = 0;
        }
        else if (statusValue != null && (int)statusValue == hotWaterEnumValue && heatingDegreeMinutes.CurrentValue < -80)
        {
            heatingDegreeMinutes.NewValue = -60;
        }
        else if (outdoorTemperature >= 0 && heatingDegreeMinutes.CurrentValue < -80)
        {
            heatingDegreeMinutes.NewValue = -60;
        }

        if (thermostatModeValues != null && heatingOffset.CurrentValue.HasValue)
        {
            thermostatMode.CurrentValue = PointValueStoreAdapter.GetCurrentValue(timestampLocal, thermostatModeValues);
            if (statusValue != null && thermostatMode.CurrentValue != null && comfortEnumValue != null && awayEnumValue != null)
            {
                if (isFloorCoolingEnabled)
                {
                    thermostatMode.NewValue = comfortEnumValue;
                }
                else if (heatingOffset.CurrentValue.Value >= 0)
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

        var airTemperatureForecastValues = await pointValueStoreAdapter.Get(AirTemperatureForecastPoint.Id, date, date.AddDays(1), fiveMinResolution: true);

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
            newHeatDistributionMode = HeatDistributionMode.Fixed;
            if (upcomingMeanTemperature > 5)
            {
                if (isFloorCoolingEnabled)
                {
                    if (statusValue == null || (int)statusValue != coolingEnumValue
                        || heatPumpSupplyTemperature == null || !configPointModel.IsFloorCoolingAllowed)
                    {
                        newHeatDistributionValue = 0;
                    }
                    else
                    {
                        newHeatDistributionMode = HeatDistributionMode.Setpoint;
                        newHeatDistributionSetpoint = (double)decimal.Clamp(configPointModel.FloorCoolingSetpoint, 19, 40);
                        if (heatPumpSupplyTemperature.Value > 14)
                        {
                            if (newHeatDistributionSetpoint < 23.0)
                                newHeatDistributionSetpoint = 23.0;
                        }
                        else if (heatPumpSupplyTemperature.Value > 13)
                        {
                            if (newHeatDistributionSetpoint < 22.0)
                                newHeatDistributionSetpoint = 22.0;
                        }
                    }
                }
                else if (currentCoolingDegreeMinutes == null || currentCoolingDegreeMinutes.Value <= 0)
                {
                    newHeatDistributionValue = 100;
                }
                else if (statusValue != null)
                {
                    if ((int)statusValue == heatingEnumValue)
                    {
                        newHeatDistributionValue = 80;
                    }
                    else if ((int)statusValue == coolingEnumValue)
                    {
                        if (heatingDegreeMinutes.CurrentValue != null && heatingDegreeMinutes.CurrentValue.Value >= 0)
                        {
                            newHeatDistributionValue = 0;
                        }
                        else
                        {
                            newHeatDistributionValue = 20;
                        }
                    }
                    else
                    {
                        newHeatDistributionValue = 50;
                    }
                }
                else
                {
                    newHeatDistributionValue = 100;
                }
            }
            else if (statusValue != null)
            {
                if ((int)statusValue == heatingEnumValue)
                {
                    newHeatDistributionValue = 96;
                }
                else if ((int)statusValue != defrostingEnumValue)
                {
                    newHeatDistributionValue = 100;
                }
            }
        }

        await SendHeatPumpCommands(heatPumpAddress, HeatingOffsetPoint, HotWaterModePoint, HeatingDegreeMinutesPoint);

        await SendLivingRoomThermostatCommands(livingRoomThermostat, comfortEnumValue);

        await SendHeatDistributionCommands(heatDistributionControllerDevice);

        return null;
    }

    [MemberNotNull(nameof(heatPumpDevice), nameof(heatPumpAddress), nameof(livingRoomThermostat), nameof(heatDistributionControllerDevice))]
    private void EnsureDevicesInitialized()
    {
        if (heatPumpDevice is null) throw new InvalidOperationException("Heat pump device not found");
        if (livingRoomThermostat is null) throw new InvalidOperationException("Living room thermostat device not found");
        if (heatDistributionControllerDevice is null) throw new InvalidOperationException("Heat distribution controller device not found");
        if (heatPumpAddress is null) throw new InvalidOperationException("Heat pump device address is invalid");
    }

    private async Task<List<Device>> InitializeDevices()
    {
        var devices = await DbContext.Devices.AsNoTrackingWithIdentityResolution()
            .Include(x => x.DevicePoints)
            .ThenInclude(x => x.EnumMembers)
            .Where(x => x.Type == "heat_pump"
                || x.Type == "airobot_thermostat"
                || x.Type == "yrno_weather_forecast"
                || (x.Type == "shelly" && x.SubType == "heat-distribution-controller")
                || x.Type == "ventilation")
            .ToListAsync();

        heatPumpDevice = devices.FirstOrDefault(x => x.Type == "heat_pump")
            ?? throw new InvalidOperationException("Heat pump device not found");

        livingRoomThermostat = devices.FirstOrDefault(x => x.Type == "airobot_thermostat" && x.SubType == "living-room");

        heatDistributionControllerDevice = devices.FirstOrDefault(x => x.Type == "shelly" && x.SubType == "heat-distribution-controller");

        if (heatPumpDevice.Address is null)
            throw new InvalidOperationException("Heat pump device address is not set");

        heatPumpAddress = JsonSerializer.Deserialize<DeviceAddress>(heatPumpDevice.Address);
        if (heatPumpAddress is null || heatPumpAddress.DeviceId is null || heatPumpAddress.ClientId is null || heatPumpAddress.ClientSecret is null)
            throw new InvalidOperationException("Heat pump device address is invalid");

        return devices;
    }

    private async Task<double?> GetOutdoorTemperature(DateTime timestampLocal, DateOnly date, List<Device> devices)
    {
        var outdoorTemperaturePoints = GetPointsByType(devices.SelectMany(x => x.DevicePoints), "outdoor-temperature");
        var outdoorTemperatureValues = new double?[outdoorTemperaturePoints.Length];
        for (int i = 0; i < outdoorTemperaturePoints.Length; i++)
        {
            var values = await pointValueStoreAdapter.Get(outdoorTemperaturePoints[i].Id, date, fiveMinResolution: true);
            outdoorTemperatureValues[i] = PointValueStoreAdapter.GetCurrentValue(timestampLocal, values);
        }
        var outdoorTemperature = outdoorTemperatureValues.Where(x => x.HasValue).Min();

        return outdoorTemperature;
    }

    private async Task SendHeatDistributionCommands(Device? heatDistributionControllerDevice)
    {
        if ((newHeatDistributionMode == HeatDistributionMode.Setpoint && newHeatDistributionSetpoint.HasValue)
            || (newHeatDistributionMode == HeatDistributionMode.Fixed && newHeatDistributionValue.HasValue))
        {
            await shellyDeviceReader.SendHeatDistributionModeAsync(
                heatDistributionControllerDevice!,
                newHeatDistributionMode.Value,
                newHeatDistributionSetpoint ?? 23,
                newHeatDistributionValue ?? 0);
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

    private static DevicePoint[] GetPointsByType(IEnumerable<DevicePoint> devicePoints, string type)
    {
        var points = devicePoints.Where(x => x.Type == type).ToArray();
        if (points.Length == 0)
            throw new InvalidOperationException($"Device point of type '{type}' not found.");
        return points;
    }

    private async Task SendHeatPumpCommands(DeviceAddress address, DevicePoint heatingOffsetPoint, DevicePoint hotWaterModePoint, DevicePoint heatingDegreeMinutesPoint)
    {
        if (heatingOffset.HasChanged || hotWaterMode.HasChanged || heatingDegreeMinutes.HasChanged)
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
