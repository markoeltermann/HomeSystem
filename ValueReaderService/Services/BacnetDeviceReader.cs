using Domain;
using Microsoft.AspNetCore.WebUtilities;
using System.Text.Json;

namespace ValueReaderService.Services;

public class BacnetDeviceReader(
    ILogger<BacnetDeviceReader> logger,
    IConfiguration configuration,
    HttpClient httpClient) : DeviceReader(logger)
{
    private readonly JsonSerializerOptions jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    protected override async Task<IList<PointValue>?> ExecuteAsyncInternal(Device device, DateTime timestamp)
    {
        var baseUrl = configuration["BacnetConnectorUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
            return null;

        var url = baseUrl;
        if (!url.EndsWith('/'))
            url += '/';
        url += "values";
        url = QueryHelpers.AddQueryString(url, device.DevicePoints.Select(x => KeyValuePair.Create("a", (string?)x.Address)));
        var response = await httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
            return null;
        var responseText = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrEmpty(responseText))
            return null;

        PointValueDto[]? pointValues;
        try
        {
            pointValues = JsonSerializer.Deserialize<PointValueDto[]>(responseText, jsonOptions);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Deserializing Bacnet connector response failed.");
            return null;
        }
        if (pointValues == null)
            return null;
        if (pointValues.Length == 0)
            return Array.Empty<PointValue>();

        var result = new List<PointValue>(device.DevicePoints.Count);

        foreach (var point in device.DevicePoints)
        {
            var value = pointValues.FirstOrDefault(x => string.Equals(point.Address, x.Address, StringComparison.OrdinalIgnoreCase))?.Value;
            if (value != null)
            {
                if (point.DataType.Name == "Enum")
                {
                    if (int.TryParse(value, out var valueInt) && valueInt > 0)
                    {
                        var enumValue = point.EnumMembers.FirstOrDefault(em => em.Value == valueInt);
                        if (enumValue?.Name != null)
                            value = enumValue.Name;
                    }
                }
                result.Add(new(point, value));
            }
        }

        return result;
    }

    protected record PointValueDto(string Address, string Value) { }
}