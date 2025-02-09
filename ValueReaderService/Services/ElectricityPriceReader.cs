using CommonLibrary.Extensions;
using Domain;
using Microsoft.AspNetCore.WebUtilities;
using SharedServices;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace ValueReaderService.Services;

public class ElectricityPriceReader(
    ILogger<ElectricityPriceReader> logger,
    PointValueStore pointValueStore,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration) : DeviceReader(logger)
{
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
        var existingValues = pointValueStore.ReadNumericValues(device.Id, npsPriceRawPoint.Id, todayDate, todayDate.AddDays(1));

        if (existingValues.Count(x => x.Item1.Date == today && x.Item2 != null) > 10
            && (existingValues.Count(x => x.Item1.Date == tomorrow && x.Item2 != null) > 10 || (timestampLocal - timestampLocal.Date) < new TimeSpan(14, 45, 0)))
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
                {
                    result.Add(new(npsPriceRawPoint, value.ToString(InvariantCulture), npsTimestamp));
                    var gridPriceRaw = CalculateRawGridPrice(npsTimestamp);
                    result.Add(new(gridPriceRawPoint, gridPriceRaw.ToString(InvariantCulture), npsTimestamp));

                    var totalBuyPrice = gridPriceRaw * vat + (value <= 0m ? value : value * vat);
                    result.Add(new(totalBuyPriceRawPoint, totalBuyPrice.ToString(InvariantCulture), npsTimestamp));
                }

                npsTimestamp = npsTimestamp.AddMinutes(10);
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
