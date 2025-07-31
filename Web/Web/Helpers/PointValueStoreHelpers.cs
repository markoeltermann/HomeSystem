using CommonLibrary.Extensions;
using CommonLibrary.Helpers;
using Domain;
using Web.Client.DTOs;
using WebCommonLibrary;

namespace Web.Helpers;

public static class PointValueStoreHelpers
{
    public static async Task UpdatePoints(DevicePoint point, ValueContainerDto values, string? baseUrl, HttpClient httpClient)
    {
        if (baseUrl.IsNullOrEmpty())
        {
            throw new BadRequestException("Point value store connector url has not been set up.");
        }

        var url = UrlHelpers.GetUrl(baseUrl, $"points/{point.Id}/values", null);
        var response = await httpClient.PutAsJsonAsync(url, values);
        if (!response.IsSuccessStatusCode)
        {
            throw new BadRequestException("The request to point value store failed with code " + response.StatusCode);
        }
    }

    public static void FillHour(ValueContainerDto valueContainer, int i, DateTime time, double? value)
    {
        valueContainer.Values[i * 6] = new NumericValueDto { Timestamp = time, Value = value };
        valueContainer.Values[i * 6 + 1] = new NumericValueDto { Timestamp = time.AddMinutes(10), Value = value };
        valueContainer.Values[i * 6 + 2] = new NumericValueDto { Timestamp = time.AddMinutes(20), Value = value };
        valueContainer.Values[i * 6 + 3] = new NumericValueDto { Timestamp = time.AddMinutes(30), Value = value };
        valueContainer.Values[i * 6 + 4] = new NumericValueDto { Timestamp = time.AddMinutes(40), Value = value };
        valueContainer.Values[i * 6 + 5] = new NumericValueDto { Timestamp = time.AddMinutes(50), Value = value };
    }

    public static string GetPointValueRequestUrl(int pointId, DateOnly from, DateOnly upTo, string? baseUrl, int? resolution)
    {
        if (baseUrl.IsNullOrEmpty())
        {
            throw new BadRequestException("Point value store connector url has not been set up.");
        }

        if (resolution == null)
        {
            resolution = 10;
            if (from >= new DateOnly(2025, 7, 31) && (upTo.DayNumber - from.DayNumber) <= 2)
            {
                resolution = 5;
            }
        }

        var url = UrlHelpers.GetUrl(baseUrl, $"points/{pointId}/values",
            [KeyValuePair.Create("from", (string?)from.ToString("yyyy-MM-dd")),
            KeyValuePair.Create("upTo", (string?)upTo.ToString("yyyy-MM-dd")),
            KeyValuePair.Create("resolution", resolution.ToString())
        ]);

        return url;
    }

    public static async Task<ValueContainerDto> GetPointValues(DevicePoint[] points, string address, DateOnly date, HttpClient httpClient, string? baseUrl)
    {
        var point = points.FirstOrDefault(x => x.Address == address) ?? throw new BadRequestException("Schedule points have not been configured");

        var values = await httpClient.GetFromJsonAsync<ValueContainerDto>(GetPointValueRequestUrl(point.Id, date, date, baseUrl, 10));

        if (values == null || values.Values == null || values.Values.Length != 24 * 6 + 1)
        {
            throw new BadRequestException($"{address} values could not be retrieved");
        }

        return values;
    }
}
