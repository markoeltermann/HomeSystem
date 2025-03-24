using CommonLibrary.Extensions;
using Domain;
using Microsoft.AspNetCore.WebUtilities;
using System.Globalization;
using System.Text.Json;

namespace ValueReaderService.Services;
public class ModbusDeviceReader(
    ILogger<BacnetDeviceReader> logger,
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory) : DeviceReader(logger)
{
    private readonly JsonSerializerOptions jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    protected override async Task<IList<PointValue>?> ExecuteAsyncInternal(Device device, DateTime timestamp, ICollection<DevicePoint> devicePoints)
    {
        var baseUrl = configuration["ModbusConnectorUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
            return null;

        var addresses = GetAddresses(devicePoints);
        if (addresses == null)
        {
            return null;
        }

        var url = baseUrl;
        if (!url.EndsWith('/'))
            url += '/';
        url += "values";
        url = QueryHelpers.AddQueryString(url, addresses.Select(x => KeyValuePair.Create("a", (string?)x.ToString())));

        using var httpClient = httpClientFactory.CreateClient(nameof(ModbusDeviceReader));
        var response = await httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Modbus connector http response was not successful, code {StatusCode}", response.StatusCode);
            return null;
        }
        var responseText = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrEmpty(responseText))
        {
            logger.LogWarning("Modbus connector http response was empty.");
            return null;
        }

        PointValueDto[]? pointValues;
        try
        {
            pointValues = JsonSerializer.Deserialize<PointValueDto[]>(responseText, jsonOptions);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Deserializing Modbus connector response failed.");
            return null;
        }
        if (pointValues == null)
        {
            logger.LogWarning("Modbus connector http response was null.");
            return null;
        }
        if (pointValues.Length == 0)
        {
            logger.LogWarning("Modbus connector http response was an empty list.");
            return Array.Empty<PointValue>();
        }

        var valueDict = pointValues.ToDictionary(x => x.Address, x => x.Value);

        var result = new List<PointValue>(devicePoints.Count);

        foreach (var point in devicePoints)
        {
            if (!point.Address.IsNullOrEmpty())
            {
                var value = GetPointValue(valueDict, point);

                if (value != null)
                {
                    if (point.DataType.Name == "Integer")
                        result.Add(new(point, ((int)Math.Round(value.Value)).ToString(CultureInfo.InvariantCulture)));
                    else if (point.DataType.Name == "Boolean")
                        result.Add(new(point, (value.Value > 0.0).ToString()));
                    else
                        result.Add(new(point, Math.Round(value.Value, 2).ToString(CultureInfo.InvariantCulture)));
                }
            }
        }

        return result;
    }

    private static double? GetPointValue(Dictionary<ushort, ushort> valueDict, DevicePoint point)
    {
        double? value = 0.0;
        var isMultiRegister = point.Address.Count(x => x == '{') > 1;
        foreach (var term in point.Address.Replace("-", "+-").Split('+').Select(x => x.Trim()))
        {
            double? termValue = 1.0;
            foreach (var factor in term.Split('*').Select(x => x.Trim()))
            {
                if (factor.StartsWith('{') && factor.EndsWith('}'))
                {
                    var address = ushort.Parse(factor[1..^1]);
                    if (valueDict.TryGetValue(address, out var addressValue))
                    {
                        int v = addressValue;
                        if (!isMultiRegister)
                        {
                            if (v > 0x7fff)
                            {
                                v -= 0x10000;
                            }
                        }
                        termValue = termValue.Value * v;
                    }
                    else
                    {
                        termValue = null;
                        break;
                    }
                }
                else
                {
                    if (double.TryParse(factor, out var d))
                    {
                        termValue = termValue.Value * d;
                    }
                    else
                    {
                        termValue = null;
                        break;
                    }
                }
            }

            if (termValue == null)
            {
                value = null;
                break;
            }
            else
            {
                value = value.Value + termValue;
            }
        }

        return value;
    }

    private ICollection<ushort>? GetAddresses(ICollection<DevicePoint> devicePoints)
    {
        HashSet<ushort> addresses = [];
        foreach (var point in devicePoints)
        {
            if (!point.Address.IsNullOrEmpty())
            {
                var startIndex = 0;
                while (true)
                {
                    var start = point.Address.IndexOf('{', startIndex);
                    if (start != -1)
                    {
                        var end = point.Address.IndexOf('}', startIndex);
                        if (end == -1)
                        {
                            logger.LogError("DevicePoint {Id} contains an invalid address.", point.Id);
                            return null;
                        }

                        if (ushort.TryParse(point.Address[(start + 1)..end], out var address))
                        {
                            addresses.Add(address);
                            startIndex = end + 1;
                        }
                        else
                        {
                            logger.LogError("DevicePoint {Id} contains an invalid address.", point.Id);
                            return null;
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        return addresses;
    }

    protected record PointValueDto(ushort Address, ushort Value) { }
}
