using Domain;
using System.Text.Json;

namespace ValueReaderService.Services;

public class ShellyDeviceReader(
    ILogger<DeviceReader> logger,
    IHttpClientFactory httpClientFactory)
    : DeviceReader(logger)
{
    protected override async Task<IList<PointValue>?> ExecuteAsyncInternal(Device device, DateTime timestamp, ICollection<DevicePoint> devicePoints)
    {
        var url = GetUrl(device);

        url += "/rpc/Shelly.GetStatus";

        using var httpClient = httpClientFactory.CreateClient(nameof(ShellyDeviceReader));
        var response = await httpClient.GetAsync(url);
        var responseText = await response.Content.ReadAsStringAsync();

        var jDoc = JsonDocument.Parse(responseText);

        var result = new List<PointValue>(devicePoints.Count);
        foreach (var point in devicePoints)
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

    private static string GetUrl(Device device)
    {
        if (device.Address is null)
            throw new InvalidOperationException("Device address is missing.");

        var address = JsonSerializer.Deserialize<DeviceAddress>(device.Address);
        if (address?.IP is null)
            throw new InvalidOperationException("Device IP address is missing.");

        var url = $"http://{address.IP}";
        if (address.Port is not null)
            url += ":" + address.Port;

        return url;
    }

    public async Task SendCommandAsync(Device device, string command, object body)
    {
        try
        {
            var url = GetUrl(device);

            url += $"/rpc/{command}";

            using var httpClient = httpClientFactory.CreateClient(nameof(ShellyDeviceReader));
            var json = JsonSerializer.Serialize(body);
            var response = await httpClient.PostAsync(url, new StringContent(json));

            if (!response.IsSuccessStatusCode)
            {
                Logger.LogError("Failed to send command {Command} to Shelly device {DeviceId}. Status code: {StatusCode}", device.Id, command, response.StatusCode);
            }
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Error sending command {Command} to Shelly device {DeviceId}.", command, device.Id);
        }
    }

    public Task SendLightSetCommandAsync(Device device, int id, bool on, int brightness)
    {
        return SendCommandAsync(device, "Light.Set", new { id, on, brightness });
    }
}
