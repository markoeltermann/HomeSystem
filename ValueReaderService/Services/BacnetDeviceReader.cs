using Domain;
using Microsoft.AspNetCore.WebUtilities;
using SharedServices;
using System.Text.Json;

namespace ValueReaderService.Services;

public class BacnetDeviceReader(
    HomeSystemContext dbContext,
    ILogger<BacnetDeviceReader> logger,
    PointValueStore pointValueStore,
    IConfiguration configuration,
    HttpClient httpClient) : DeviceReader(dbContext, logger)
{
    private readonly JsonSerializerOptions jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    protected override async Task<bool> ExecuteAsyncInternal(Device device, DateTime timestamp)
    {
        var baseUrl = configuration["BacnetConnectorUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
            return false;

        var url = baseUrl;
        if (!url.EndsWith('/'))
            url += '/';
        url += "values";
        url = QueryHelpers.AddQueryString(url, device.DevicePoints.Select(x => KeyValuePair.Create("a", (string?)x.Address)));
        var response = await httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
            return false;
        var responseText = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrEmpty(responseText))
            return false;

        PointValueDto[]? pointValues;
        try
        {
            pointValues = JsonSerializer.Deserialize<PointValueDto[]>(responseText, jsonOptions);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Deserializing Bacnet connector response failed.");
            return false;
        }
        if (pointValues == null)
            return false;
        if (pointValues.Length == 0)
            return true;

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
                pointValueStore.StoreValue(device.Id, point.Id, timestamp, value);
            }
        }

        return true;
    }

    protected record PointValueDto(string Address, string Value) { }
}