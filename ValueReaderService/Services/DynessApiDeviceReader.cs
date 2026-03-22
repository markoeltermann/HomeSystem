using Domain;
using DynessConnector;
using DynessConnector.Client.Models;
using System.Globalization;
using System.Text.Json;

namespace ValueReaderService.Services;

public class DynessApiDeviceReader(
    ILogger<DeviceReader> logger,
    IHttpClientFactory httpClientFactory)
    : DeviceReader(logger)
{
    private const string TimestampPointId = "T";
    private const string TimestampFormat = "yyyy-MM-dd HH:mm:ss";
    private static readonly TimeSpan MaxDataAge = TimeSpan.FromMinutes(6);

    protected override async Task<IList<PointValue>?> ExecuteAsyncInternal(Device device, DateTime timestamp, ICollection<DevicePoint> devicePoints)
    {
        if (device.Address is null)
            return null;

        var address = JsonSerializer.Deserialize<DeviceAddress>(device.Address);
        if (address is null || address.DeviceId is null || address.ClientId is null || address.ClientSecret is null)
            return null;

        using var httpClient = httpClientFactory.CreateClient(nameof(DynessApiDeviceReader));
        var client = DynessClientFactory.Create(httpClient, address.ClientId, address.ClientSecret);

        var response = await client.V1.Device.RealTime.Data.PostAsync(new RequestOpenApiPointDto { DeviceSn = address.DeviceId });
        if (response?.Data is null || response.Data.Count == 0)
            return null;

        var timestampPoint = response.Data.FirstOrDefault(x => x.PointId == TimestampPointId);
        if (timestampPoint?.PointValue is null)
            return null;

        if (!DateTime.TryParseExact(timestampPoint.PointValue, TimestampFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var dataTimestamp))
            return null;

        if (timestamp.ToUniversalTime() - dataTimestamp > MaxDataAge)
            return null;

        var result = new List<PointValue>(devicePoints.Count);
        foreach (var point in devicePoints)
        {
            var apiPoint = response.Data.FirstOrDefault(x => x.PointId == point.Address);
            if (apiPoint?.PointValue is null)
                continue;

            string value;
            if (point.DataType.Name == "Boolean")
            {
                var boolValue = apiPoint.PointValue == "1";
                value = boolValue.ToString();
            }
            else
            {
                value = apiPoint.PointValue;
            }

            result.Add(new PointValue(point, value));
        }

        return result;
    }
}
