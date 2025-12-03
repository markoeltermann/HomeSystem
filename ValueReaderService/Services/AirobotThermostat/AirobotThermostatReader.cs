using Domain;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ValueReaderService.Services.AirobotThermostat;

public class AirobotThermostatReader(
    ILogger<DeviceReader> logger,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration)
    : DeviceReader(logger)
{
    protected override async Task<IList<PointValue>?> ExecuteAsyncInternal(Device device, DateTime timestamp, ICollection<DevicePoint> devicePoints)
    {
        using var httpClient = GetHttpClient();

        var (statusesUrl, settingsUrl) = GetUrls(device, true);

        var jDocStatuses = await GetJDoc(statusesUrl, httpClient);
        var jDocSettings = await GetJDoc(settingsUrl, httpClient);

        var result = new List<PointValue>(devicePoints.Count);
        foreach (var point in devicePoints)
        {
            var value = GetPointValue(jDocStatuses, point);
            if (value != null)
            {
                result.Add(value);
            }
            else
            {
                value = GetPointValue(jDocSettings, point);
                if (value != null)
                {
                    result.Add(value);
                }
            }
        }

        return result;
    }

    public async Task WriteMode(Device device, AirobotThermostatMode mode)
    {
        using var httpClient = GetHttpClient();
        var (_, getSettingsUrl) = GetUrls(device, true);
        var (_, setSettingsUrl) = GetUrls(device, false);

        //var settingsJson = await httpClient.GetStringAsync(getSettingsUrl);

        //var json = JsonSerializer.Serialize(new { MODE = (int)mode });
        var stringContent = new StringContent("{\"MODE\":\t" + (int)mode + "}", Encoding.UTF8, "application/json");

        using var response = await httpClient.PostAsync(setSettingsUrl, stringContent);

        if (!response.IsSuccessStatusCode)
        {
            Logger.LogError("Failed to set mode for Airobot Thermostat device {DeviceId}. Status code: {StatusCode}", device.Id, response.StatusCode);
        }
    }

    private static (string statusesUrl, string settingsUrl) GetUrls(Device device, bool get)
    {
        if (device.Address is null)
            throw new InvalidOperationException("Device address is missing.");

        var address = JsonSerializer.Deserialize<DeviceAddress>(device.Address);
        if (address?.IP is null)
            throw new InvalidOperationException("Device IP address is missing.");

        var baseUrl = $"http://{address.IP}";
        if (address.Port is not null)
            baseUrl += ":" + address.Port;

        var statusesUrl = baseUrl + "/api/thermostat/getStatuses";
        var settingsUrl = baseUrl + $"/api/thermostat/{(get ? "get" : "set")}Settings";

        return (statusesUrl, settingsUrl);
    }

    private HttpClient GetHttpClient()
    {
        var authenticationId = configuration["AuthenticationId"];
        var authenticationSecret = configuration["AuthenticationSecret"];
        if (string.IsNullOrEmpty(authenticationId) || string.IsNullOrEmpty(authenticationSecret))
            throw new InvalidOperationException("Authentication credentials are missing.");
        var httpClient = httpClientFactory.CreateClient(nameof(AirobotThermostatReader));

        var authenticationString = $"{authenticationId}:{authenticationSecret}";
        var base64EncodedAuthenticationString = Convert.ToBase64String(Encoding.ASCII.GetBytes(authenticationString));
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64EncodedAuthenticationString);
        return httpClient;
    }

    private static PointValue? GetPointValue(JsonDocument jDoc, DevicePoint point)
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
                    return new(point, (value * multiplier).ToString(InvariantCulture));
                }
                else if (point.DataType.Name == "Boolean")
                {
                    try
                    {
                        var b = element.Value.GetInt32();
                        return new(point, (b > 0).ToString());
                    }
                    catch { }
                }
                else if (point.DataType.Name == "Enum")
                {
                    try
                    {
                        var b = element.Value.GetInt32();
                        return new(point, b.ToString());
                    }
                    catch { }
                }
            }
        }

        return null;
    }

    private static async Task<JsonDocument> GetJDoc(string baseUrl, HttpClient httpClient)
    {
        using var response = await httpClient.GetAsync(baseUrl);
        var responseText = await response.Content.ReadAsStringAsync();
        var jDoc = JsonDocument.Parse(responseText);
        return jDoc;
    }
}
