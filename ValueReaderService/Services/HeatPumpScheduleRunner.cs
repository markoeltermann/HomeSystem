using Domain;
using Microsoft.EntityFrameworkCore;
using MyUplinkConnector;
using MyUplinkConnector.Client.V2.Devices.Item.Points;
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

        var actualHeatingOffsetPoint = heatPumpDevice.DevicePoints.FirstOrDefault(x => x.Type == "heating-offset");
        var actualHotWaterModePoint = heatPumpDevice.DevicePoints.FirstOrDefault(x => x.Type == "hot-water-mode");
        if (actualHeatingOffsetPoint == null || actualHotWaterModePoint == null)
        {
            return null;
        }

        var heatingOffsetPoint = devicePoints.FirstOrDefault(x => x.Address == "heating-offset");
        var hotWaterModePoint = devicePoints.FirstOrDefault(x => x.Address == "hot-water-mode");
        if (heatingOffsetPoint == null || hotWaterModePoint == null)
        {
            return null;
        }

        var actualHeatingOffsetValues = await pointValueStoreAdapter.Get(actualHeatingOffsetPoint.Id, date);
        var actualHotWaterModeValues = await pointValueStoreAdapter.Get(actualHotWaterModePoint.Id, date);
        var heatingOffsetValues = await pointValueStoreAdapter.Get(heatingOffsetPoint.Id, date);
        var hotWaterModeValues = await pointValueStoreAdapter.Get(hotWaterModePoint.Id, date);

        var actualHeatingOffset = PointValueStoreAdapter.GetCurrentValue(timestampLocal, actualHeatingOffsetValues);
        var actualHotWaterMode = PointValueStoreAdapter.GetCurrentValue(timestampLocal, actualHotWaterModeValues);
        var heatingOffset = PointValueStoreAdapter.GetCurrentValue(timestampLocal, heatingOffsetValues);
        var hotWaterMode = PointValueStoreAdapter.GetCurrentValue(timestampLocal, hotWaterModeValues);

        if (actualHeatingOffset == null || actualHotWaterMode == null || heatingOffset == null || hotWaterMode == null)
        {
            return null;
        }

        heatingOffset = Math.Clamp(heatingOffset.Value, -10, 10);
        hotWaterMode = Math.Clamp(hotWaterMode.Value, 0, 2);

        if (actualHeatingOffset.Value != heatingOffset.Value || actualHotWaterMode.Value != hotWaterMode.Value)
        {
            using var httpClient = httpClientFactory.CreateClient(nameof(MyUplinkDeviceReader));
            var client = await MyUplinkClientFactory.Create(httpClient, address.ClientId, address.ClientSecret)
                ?? throw new InvalidOperationException("MyUplink client could not be created");

            var body = new PointsPatchRequestBody();
            if (actualHeatingOffset.Value != heatingOffset.Value)
            {
                body.AdditionalData.Add(actualHeatingOffsetPoint.Address, (int)heatingOffset.Value);
            }
            if (actualHotWaterMode.Value != hotWaterMode.Value)
            {
                body.AdditionalData.Add(actualHotWaterModePoint.Address, (int)hotWaterMode.Value);
            }

            await client.V2.Devices[address.DeviceId].Points.PatchAsync(body);
        }

        return null;
    }
}
