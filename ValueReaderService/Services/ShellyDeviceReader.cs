using Domain;
using SharedServices;
using System.Text.Json;

namespace ValueReaderService.Services;

public class ShellyDeviceReader(
    HomeSystemContext dbContext,
    ILogger<DeviceReader> logger,
    HttpClient httpClient,
    PointValueStore pointValueStore)
    : DeviceReader(dbContext, logger)
{
    protected override async Task<bool> ExecuteAsyncInternal(Device device, DateTime timestamp)
    {
        if (device.Address is null)
            return false;

        var address = JsonSerializer.Deserialize<DeviceAddress>(device.Address);
        if (address?.IP is null)
            return false;

        var url = $"http://{address.IP}";
        if (address.Port is not null)
            url += ":" + address.Port;

        url += "/rpc/Shelly.GetStatus";

        var response = await httpClient.GetAsync(url);
        var responseText = await response.Content.ReadAsStringAsync();

        var jDoc = JsonDocument.Parse(responseText);

        foreach (var point in device.DevicePoints)
        {
            var addressParts = point.Address.Split('.');
            if (addressParts.Length == 2 && jDoc.RootElement.TryGetProperty(addressParts[0], out var element))
            {
                if (point.DataType.Name == "Float" && element.TryGetProperty(addressParts[1], out var valueElement) && valueElement.TryGetDouble(out var value))
                {
                    pointValueStore.StoreValue(device.Id, point.Id, timestamp, value.ToString());
                }
                else if (point.DataType.Name == "Boolean" && element.TryGetProperty(addressParts[1], out valueElement))
                {
                    try
                    {
                        var b = valueElement.GetBoolean();
                        pointValueStore.StoreValue(device.Id, point.Id, timestamp, b.ToString());
                    }
                    catch { }
                }
            }
        }

        return true;
    }
}
