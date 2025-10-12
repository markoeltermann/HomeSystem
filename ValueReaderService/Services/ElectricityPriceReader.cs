using CommonLibrary.Extensions;
using Domain;
using Microsoft.AspNetCore.WebUtilities;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace ValueReaderService.Services;

public class ElectricityPriceReader(
    ILogger<ElectricityPriceReader> logger,
    PointValueStoreAdapter pointValueStoreAdapter,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration) : DeviceReader(logger)
{
    public override bool StorePointsWithReplace => true;

    protected override async Task<IList<PointValue>?> ExecuteAsyncInternal(Device device, DateTime timestamp, ICollection<DevicePoint> devicePoints)
    {
        var vat = GetVat(configuration);

        var npsPriceRawPoint = devicePoints.FirstOrDefault(x => x.Address == "nps-price-raw");
        var gridPriceRawPoint = devicePoints.FirstOrDefault(x => x.Address == "grid-price-raw");
        var totalBuyPriceRawPoint = devicePoints.FirstOrDefault(x => x.Address == "total-buy-price");
        if (npsPriceRawPoint == null || gridPriceRawPoint == null || totalBuyPriceRawPoint == null)
        {
            return null;
        }

        var timestampLocal = timestamp.ToLocalTime();
        var today = timestampLocal.Date;
        var tomorrow = today.AddDays(1);
        var todayDate = DateOnly.FromDateTime(today);
        //var existingValues = pointValueStore.ReadNumericValues(device.Id, npsPriceRawPoint.Id, todayDate, todayDate.AddDays(1), true);
        var existingValues = await pointValueStoreAdapter.Get(npsPriceRawPoint.Id, todayDate, todayDate.AddDays(2), true, true);

        if (existingValues?.Values == null)
        {
            return null;
        }

        if (existingValues.Values.Count(x => x.Timestamp.Date == today && x.Value != null) > 20
            && (existingValues.Values.Count(x => x.Timestamp.Date == tomorrow && x.Value != null) > 20 || (timestampLocal - timestampLocal.Date) < new TimeSpan(14, 45, 0)))
        {
            return null;
        }

        using var httpClient = httpClientFactory.CreateClient(nameof(ElectricityPriceReader));

        var url = "https://dashboard.elering.ee/api/nps/price";
        url = QueryHelpers.AddQueryString(url, [KeyValuePair.Create("start", (string?)today.ToUniversalTime().ToString("O")),
            KeyValuePair.Create("end", (string?)tomorrow.AddDays(2).ToUniversalTime().ToString("O"))]);

        var npsResponse = await httpClient.GetFromJsonAsync<NpsPriceResponse?>(url);
        if (npsResponse == null || npsResponse.Success != true || npsResponse.Data == null || npsResponse.Data.EE == null)
        {
            return null;
        }

        var result = new List<PointValue>();
        var validPricePoints = npsResponse.Data.EE.Where(x => x.UnixTimestamp.HasValue && x.Price.HasValue)
            .Select(x => (DateTimeOffset.FromUnixTimeSeconds(x.UnixTimestamp!.Value).UtcDateTime, x))
            .ToArray();

        foreach (var ((itemTimestamp, item), (nextTimestamp, next)) in validPricePoints.Zip(validPricePoints.Cast<(DateTime, NpsPrice?)>().Skip(1).Append((default, null))))
        {
            var npsTimestamp = new DateTime(itemTimestamp.Year, itemTimestamp.Month, itemTimestamp.Day, itemTimestamp.Hour, itemTimestamp.Minute / 15 * 15, 0, DateTimeKind.Utc);
            var value = item.Price!.Value / 1000m;

            var numOfPointsToGenerate = npsTimestamp.Minute == 0 && (next == null || nextTimestamp.Minute == 0) ? 12 : 3;

            for (int i = 0; i < numOfPointsToGenerate; i++)
            {
                result.Add(new(npsPriceRawPoint, value.ToString(InvariantCulture), npsTimestamp));
                var gridPriceRaw = CalculateRawGridPrice(npsTimestamp);
                result.Add(new(gridPriceRawPoint, gridPriceRaw.ToString(InvariantCulture), npsTimestamp));

                var totalBuyPrice = gridPriceRaw * vat + (value <= 0m ? value : value * vat);
                result.Add(new(totalBuyPriceRawPoint, totalBuyPrice.ToString(InvariantCulture), npsTimestamp));

                npsTimestamp = npsTimestamp.AddMinutes(5);
            }
        }

        return result;
    }

    private static decimal GetVat(IConfiguration configuration)
    {
        var vatRaw = configuration["ValueAddedTax"];
        if (vatRaw.IsNullOrEmpty())
        {
        }
        if (!decimal.TryParse(vatRaw, out var vat))
        {
            throw new InvalidOperationException("ValueAddedTax is not in correct format.");
        }
        vat = vat / 100m + 1m;
        return vat;
    }

    private static decimal CalculateRawGridPrice(DateTime timestamp)
    {
        timestamp = timestamp.ToLocalTime();
        var time = timestamp - timestamp.Date;

        const decimal dayPrice = 0.0529m;
        const decimal nightPrice = 0.0303m;
        const decimal dayPeakPrice = 0.0818m;
        const decimal weekendPeakPrice = 0.0474m;

        if (timestamp.Month is 11 or 12 or 1 or 2 or 3)
        {
            var isEveningPeak = time >= TimeSpan.FromHours(16) && time < TimeSpan.FromHours(20);
            if (timestamp.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday || time < TimeSpan.FromHours(7) || time >= TimeSpan.FromHours(22))
            {
                if (isEveningPeak)
                {
                    return weekendPeakPrice;
                }
                else
                {
                    return nightPrice;
                }
            }
            else
            {
                if (isEveningPeak || time >= TimeSpan.FromHours(9) && time < TimeSpan.FromHours(12))
                {
                    return dayPeakPrice;
                }
                else
                {
                    return dayPrice;
                }
            }
        }
        else
        {
            if (timestamp.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday || time < TimeSpan.FromHours(7) || time >= TimeSpan.FromHours(22))
            {
                return nightPrice;
            }
            else
            {
                return dayPrice;
            }
        }
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
