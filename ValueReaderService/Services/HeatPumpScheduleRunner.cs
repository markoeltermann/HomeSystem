using Domain;
using Microsoft.EntityFrameworkCore;
using MyUplinkConnector;
using MyUplinkConnector.Client.V2.Devices.Item.Points;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace ValueReaderService.Services;

public class HeatPumpScheduleRunner(
    ILogger<DeviceReader> logger,
    HomeSystemContext dbContext,
    PointValueStoreAdapter pointValueStoreAdapter,
    IHttpClientFactory httpClientFactory) : DeviceReader(logger)
{
    protected override async Task<IList<PointValue>?> ExecuteAsyncInternal(Device device, DateTime timestamp, ICollection<DevicePoint> devicePoints)
    {
#if !DEBUG
        await Task.Delay(TimeSpan.FromSeconds(10));
#endif

        var timestampLocal = timestamp.ToLocalTime().AddSeconds(30);
        var date = DateOnly.FromDateTime(timestampLocal);

        var heatPumpDevice = await dbContext.Devices.AsNoTrackingWithIdentityResolution().Include(x => x.DevicePoints).FirstOrDefaultAsync(x => x.Type == "heat_pump")
            ?? throw new InvalidOperationException("Heat pump device not found");

        if (heatPumpDevice.Address is null)
            throw new InvalidOperationException("Heat pump device address is not set");

        var address = JsonSerializer.Deserialize<DeviceAddress>(heatPumpDevice.Address);
        if (address is null || address.DeviceId is null || address.ClientId is null || address.ClientSecret is null)
            throw new InvalidOperationException("Heat pump device address is invalid");

        var heatingOffset = new DeviceValue(-10, 10);
        var heatingDegreeMinutes = new DeviceValue(-300, 300);
        var hotWaterMode = new DeviceValue(0, 2);
        //var activeCoolingStart = new DeviceValue(10, 300);

        var actualHeatingOffsetPoint = heatPumpDevice.DevicePoints.FirstOrDefault(x => x.Type == "heating-offset");
        var actualHotWaterModePoint = heatPumpDevice.DevicePoints.FirstOrDefault(x => x.Type == "hot-water-mode");
        var heatingDegreeMinutesPoint = heatPumpDevice.DevicePoints.FirstOrDefault(x => x.Type == "heating-degree-minutes");
        //var activeCoolingStartPoint = heatPumpDevice.DevicePoints.FirstOrDefault(x => x.Type == "active-cooling-start");
        //var calculatedCoolingSupplyTemperaturePoint = heatPumpDevice.DevicePoints.FirstOrDefault(x => x.Type == "calculated-cooling-supply-temperature");

        if (actualHeatingOffsetPoint != null && actualHotWaterModePoint != null && heatingDegreeMinutesPoint != null)
        {
            var heatingOffsetPoint = devicePoints.FirstOrDefault(x => x.Address == "heating-offset");
            var hotWaterModePoint = devicePoints.FirstOrDefault(x => x.Address == "hot-water-mode");
            if (heatingOffsetPoint != null && hotWaterModePoint != null)
            {
                var actualHeatingOffsetValues = await pointValueStoreAdapter.Get(actualHeatingOffsetPoint.Id, date);
                var actualHotWaterModeValues = await pointValueStoreAdapter.Get(actualHotWaterModePoint.Id, date);
                var heatingDegreeMinutesValues = await pointValueStoreAdapter.Get(heatingDegreeMinutesPoint.Id, date);
                var heatingOffsetValues = await pointValueStoreAdapter.Get(heatingOffsetPoint.Id, date);
                var hotWaterModeValues = await pointValueStoreAdapter.Get(hotWaterModePoint.Id, date);

                heatingOffset.CurrentValue = PointValueStoreAdapter.GetCurrentValue(timestampLocal, actualHeatingOffsetValues);
                heatingOffset.NewValue = PointValueStoreAdapter.GetCurrentValue(timestampLocal, heatingOffsetValues);

                hotWaterMode.CurrentValue = PointValueStoreAdapter.GetCurrentValue(timestampLocal, actualHotWaterModeValues);
                hotWaterMode.NewValue = PointValueStoreAdapter.GetCurrentValue(timestampLocal, hotWaterModeValues);

                if (heatingOffset.HasChanged && heatingOffset.NewValue.Value >= 0 && heatingOffset.CurrentValue.Value < 0)
                {
                    heatingDegreeMinutes.CurrentValue = PointValueStoreAdapter.GetCurrentValue(timestampLocal, heatingDegreeMinutesValues);
                    heatingDegreeMinutes.NewValue = 0;
                }
            }
        }

        //if (activeCoolingStartPoint != null && calculatedCoolingSupplyTemperaturePoint != null)
        //{
        //    var coolingControllerDevice = await dbContext.Devices.AsNoTrackingWithIdentityResolution()
        //        .Include(x => x.DevicePoints)
        //        .FirstOrDefaultAsync(x => x.SubType == "cooling-controller")
        //            ?? throw new InvalidOperationException("Cooling controller device not found");

        //    var coolingTankTemperatureLowerPoint = coolingControllerDevice.DevicePoints.FirstOrDefault(x => x.Type == "cooling-tank-temperature-lower");
        //    if (coolingTankTemperatureLowerPoint != null)
        //    {
        //        var coolingTankTemperatureLowerValues = await pointValueStoreAdapter.Get(coolingTankTemperatureLowerPoint.Id, date, fiveMinResolution: true);
        //        var coolingSupplyTemperatureValues = await pointValueStoreAdapter.Get(calculatedCoolingSupplyTemperaturePoint.Id, date, fiveMinResolution: true);
        //        var activeCoolingStartValues = await pointValueStoreAdapter.Get(activeCoolingStartPoint.Id, date, fiveMinResolution: true);

        //        var coolingTankTemperatureLower = PointValueStoreAdapter.GetCurrentValue(timestampLocal, coolingTankTemperatureLowerValues);
        //        var coolingSupplyTemperature = PointValueStoreAdapter.GetCurrentValue(timestampLocal, coolingSupplyTemperatureValues);
        //        var actualActiveCoolingStart = PointValueStoreAdapter.GetCurrentValue(timestampLocal, activeCoolingStartValues);

        //        if (coolingTankTemperatureLower.HasValue && coolingSupplyTemperature.HasValue && actualActiveCoolingStart.HasValue)
        //        {
        //            //var coolingStartTemperatureThreshold = coolingSupplyTemperature.Value + 5;

        //            //activeCoolingStart.CurrentValue = actualActiveCoolingStart;
        //            //activeCoolingStart.NewValue = coolingTankTemperatureLower < coolingStartTemperatureThreshold ? 300 : 30;
        //        }
        //    }

        //}

        if (!heatingOffset.HasChanged && !hotWaterMode.HasChanged)
        {
            return null;
        }

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
