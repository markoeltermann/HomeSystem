using Domain;
using System.Text.Json;

namespace ValueReaderService.Services;

public class ShellyDeviceReader(
    ILogger<DeviceReader> logger,
    HttpClient httpClient)
    : DeviceReader(logger)
{
    protected override async Task<IList<PointValue>?> ExecuteAsyncInternal(Device device, DateTime timestamp)
    {
        if (device.Address is null)
            return null;

        var address = JsonSerializer.Deserialize<DeviceAddress>(device.Address);
        if (address?.IP is null)
            return null;

        var url = $"http://{address.IP}";
        if (address.Port is not null)
            url += ":" + address.Port;

        url += "/rpc/Shelly.GetStatus";

        var response = await httpClient.GetAsync(url);
        var responseText = await response.Content.ReadAsStringAsync();

        var jDoc = JsonDocument.Parse(responseText);

        var result = new List<PointValue>(device.DevicePoints.Count);
        foreach (var point in device.DevicePoints)
        {
            var addressParts = point.Address.Split('.');
            if (addressParts.Length == 2 && jDoc.RootElement.TryGetProperty(addressParts[0], out var element))
            {
                if (point.DataType.Name == "Float" && element.TryGetProperty(addressParts[1], out var valueElement) && valueElement.TryGetDouble(out var value))
                {
                    result.Add(new(point, value.ToString()));
                }
                else if (point.DataType.Name == "Boolean" && element.TryGetProperty(addressParts[1], out valueElement))
                {
                    try
                    {
                        var b = valueElement.GetBoolean();
                        result.Add(new(point, b.ToString()));
                    }
                    catch { }
                }
            }
        }

        return result;
    }
}
