using Domain;
using Microsoft.AspNetCore.WebUtilities;
using SharedServices;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace ValueReaderService.Services;

public class ElectricityPriceReader(ILogger<ElectricityPriceReader> logger, PointValueStore pointValueStore, IHttpClientFactory httpClientFactory) : DeviceReader(logger)
{
    protected override async Task<IList<PointValue>?> ExecuteAsyncInternal(Device device, DateTime timestamp, ICollection<DevicePoint> devicePoints)
    {
        var npsPriceRawPoint = devicePoints.FirstOrDefault(x => x.Address == "nps-price-raw");
        if (npsPriceRawPoint == null)
        {
            return null;
        }

        var today = timestamp.ToLocalTime().Date;
        var tomorrow = today.AddDays(1);
        var todayDate = DateOnly.FromDateTime(today);
        var existingValues = pointValueStore.ReadNumericValues(device.Id, npsPriceRawPoint.Id, todayDate, todayDate.AddDays(1));

        if (existingValues.Any(x => x.Item1.Date == today && x.Item2 != null) && existingValues.Any(x => x.Item1.Date == tomorrow && x.Item2 != null))
        {
            return null;
        }

        using var httpClient = httpClientFactory.CreateClient(nameof(ElectricityPriceReader));

        var url = "https://dashboard.elering.ee/api/nps/price";
        url = QueryHelpers.AddQueryString(url, [KeyValuePair.Create("start", (string?)today.ToUniversalTime().ToString("O")),
            KeyValuePair.Create("end", (string?)tomorrow.AddDays(1).ToUniversalTime().ToString("O"))]);

        var npsResponse = await httpClient.GetFromJsonAsync<NpsPriceResponse?>(url);
        if (npsResponse == null || npsResponse.Success != true || npsResponse.Data == null || npsResponse.Data.EE == null)
        {
            return null;
        }

        var existingValueDict = existingValues.ToDictionary(x => x.Item1.ToUniversalTime(), x => x.Item2);

        var result = new List<PointValue>();
        foreach (var item in npsResponse.Data.EE.Where(x => x.UnixTimestamp.HasValue && x.Price.HasValue))
        {
            var npsTimestamp = DateTimeOffset.FromUnixTimeSeconds(item.UnixTimestamp!.Value).UtcDateTime;
            npsTimestamp = new DateTime(npsTimestamp.Year, npsTimestamp.Month, npsTimestamp.Day, npsTimestamp.Hour, 0, 0, DateTimeKind.Utc);
            var value = item.Price!.Value / 1000m;

            for (int i = 0; i < 6; i++)
            {
                if (!existingValueDict.TryGetValue(npsTimestamp, out var existingValue) || existingValue == null)
                    result.Add(new(npsPriceRawPoint, value.ToString(InvariantCulture), npsTimestamp));

                npsTimestamp = npsTimestamp.AddMinutes(10);
            }
        }

        return result;
    }

    private class ValueContainerDto
    {
        public NumericValueDto[] Values { get; set; } = null!;
        public string Unit { get; set; } = null!;
    }

    private class NumericValueDto
    {
        public DateTime Timestamp { get; set; }
        public double? Value { get; set; }
    }

    private class NpsPrice
    {
        [JsonPropertyName("timestamp")]
        public long? UnixTimestamp { get; set; }

        [JsonPropertyName("price")]
        public decimal? Price { get; set; }
    }

    private class NpsPriceResponse
    {
        [JsonPropertyName("data")]
        public NpsPriceData? Data { get; set; }

        [JsonPropertyName("success")]
        public bool? Success { get; set; }
    }

    private class NpsPriceData
    {
        [JsonPropertyName("ee")]
        public NpsPrice[]? EE { get; set; }
    }
}
