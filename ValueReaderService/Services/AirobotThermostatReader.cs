using Domain;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ValueReaderService.Services;

public class AirobotThermostatReader(
    ILogger<DeviceReader> logger,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration)
    : DeviceReader(logger)
{
    protected override async Task<IList<PointValue>?> ExecuteAsyncInternal(Device device, DateTime timestamp, ICollection<DevicePoint> devicePoints)
    {
        var authenticationId = configuration["AuthenticationId"];
        var authenticationSecret = configuration["AuthenticationSecret"];
        if (string.IsNullOrEmpty(authenticationId) || string.IsNullOrEmpty(authenticationSecret))
            return null;

        if (device.Address is null)
            return null;

        var address = JsonSerializer.Deserialize<DeviceAddress>(device.Address);
        if (address?.IP is null)
            return null;

        var url = $"http://{address.IP}";
        if (address.Port is not null)
            url += ":" + address.Port;

        url += "/api/thermostat/getStatuses";

        using var httpClient = httpClientFactory.CreateClient(nameof(AirobotThermostatReader));

        var authenticationString = $"{authenticationId}:{authenticationSecret}";
        var base64EncodedAuthenticationString = Convert.ToBase64String(Encoding.ASCII.GetBytes(authenticationString));
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64EncodedAuthenticationString);

        var response = await httpClient.GetAsync(url);
        var responseText = await response.Content.ReadAsStringAsync();

        var jDoc = JsonDocument.Parse(responseText);

        var result = new List<PointValue>(devicePoints.Count);
        foreach (var point in devicePoints)
        {
            var addressParts = point.Address.Split('*');
            if (addressParts.Length is 1 or 2)
            {
                JsonElement? element = jDoc.RootElement;
                var addressPathParts = addressParts[0].Split('.');
                foreach (var pathSegment in addressPathParts)
                {
                    if (element != null)
                    {
                        var propertyName = pathSegment;
                        var arrayIndex = (int?)null;
                        if (pathSegment.EndsWith(']'))
                        {
                            var pt = pathSegment.TrimEnd(']').Split('[');
                            propertyName = pt[0];
                            arrayIndex = int.Parse(pt[1]);
                        }

                        if (element.Value.TryGetProperty(propertyName, out var t))
                        {
                            element = t;
                            if (arrayIndex != null)
                                element = element.Value[arrayIndex.Value];
                        }
                        else
                            element = null;
                    }
                }

                if (element != null)
                {
                    if (point.DataType.Name == "Float" && element.Value.TryGetDecimal(out var value))
                    {
                        var multiplier = addressParts.Length == 2 ? decimal.Parse(addressParts[1], InvariantCulture) : 1.0m;
                        result.Add(new(point, (value * multiplier).ToString(InvariantCulture)));
                    }
                    else if (point.DataType.Name == "Boolean")
                    {
                        try
                        {
                            var b = element.Value.GetInt32();
                            result.Add(new(point, (b > 0).ToString()));
                        }
                        catch { }
                    }
                }
            }
        }

        return result;
    }
}
