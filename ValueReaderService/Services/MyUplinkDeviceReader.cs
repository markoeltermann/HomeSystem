using Domain;
using Microsoft.Kiota.Abstractions.Serialization;
using MyUplinkConnector;
using System.Globalization;
using System.Text.Json;

namespace ValueReaderService.Services;

public class MyUplinkDeviceReader(
    ILogger<DeviceReader> logger,
    IHttpClientFactory httpClientFactory)
    : DeviceReader(logger)
{
    protected override async Task<IList<PointValue>?> ExecuteAsyncInternal(Device device, DateTime timestamp, ICollection<DevicePoint> devicePoints)
    {
        if (device.Address is null)
            return null;

        var address = JsonSerializer.Deserialize<DeviceAddress>(device.Address);
        if (address is null || address.DeviceId is null || address.ClientId is null || address.ClientSecret is null)
            return null;

        using var httpClient = httpClientFactory.CreateClient(nameof(MyUplinkDeviceReader));
        var client = await MyUplinkClientFactory.Create(httpClient, address.ClientId, address.ClientSecret);
        if (client == null)
            return null;

        var parameterIds = string.Join(',', devicePoints.Select(x => x.Address));

        var uplinkPoints = await client.V2.Devices[address.DeviceId].Points.GetAsync(x => x.QueryParameters.Parameters = parameterIds);
        if (uplinkPoints == null || uplinkPoints.Count == 0)
            return null;

        var result = new List<PointValue>(devicePoints.Count);
        foreach (var point in devicePoints)
        {
            var uplinkPoint = uplinkPoints.FirstOrDefault(x => x.ParameterId == point.Address);
            if (uplinkPoint != null && uplinkPoint.Value is UntypedDecimal d)
            {
                result.Add(new PointValue(point, d.GetValue().ToString(CultureInfo.InvariantCulture)));
            }
        }

        return result;
    }
}
