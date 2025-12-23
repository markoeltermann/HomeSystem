using CommonLibrary.Extensions;
using Domain;
using Microsoft.AspNetCore.WebUtilities;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace ValueReaderService.Services;

public class ElectricityPriceReader(
    ILogger<ElectricityPriceReader> logger,
    PointValueStoreAdapter pointValueStoreAdapter,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ConfigModel configModel) : DeviceReader(logger)
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

        if (existingValues.Values!.Count(x => x.Timestamp.Date == today && x.Value != null) > 20
            && (existingValues.Values!.Count(x => x.Timestamp.Date == tomorrow && x.Value != null) > 20 || (timestampLocal - timestampLocal.Date) < new TimeSpan(14, 12, 0)))
        {
            return null;
        }

        using var httpClient = httpClientFactory.CreateClient(nameof(ElectricityPriceReader));

        var eleringUrl = "https://dashboard.elering.ee/api/nps/price";
        eleringUrl = QueryHelpers.AddQueryString(eleringUrl, [KeyValuePair.Create("start", (string?)today.ToUniversalTime().ToString("O")),
            KeyValuePair.Create("end", (string?)tomorrow.AddDays(2).ToUniversalTime().ToString("O"))]);

        var npsResponse = await httpClient.GetFromJsonAsync<NpsPriceResponse?>(eleringUrl);
        if (npsResponse == null || npsResponse.Success != true || npsResponse.Data == null || npsResponse.Data.EE == null)
        {
            return null;
        }

        var validPricePoints = npsResponse.Data.EE.Where(x => x.UnixTimestamp.HasValue && x.Price.HasValue)
            .ToArray();

        if (validPricePoints.Count(x => x.Timestamp > tomorrow) < 20)
        {
            var entsoeUrl = "https://web-api.tp.entsoe.eu/api";
            entsoeUrl = QueryHelpers.AddQueryString(entsoeUrl, [
                Q("documentType", "A44"),
                Q("in_Domain", "10Y1001A1001A39I"),
                Q("out_Domain", "10Y1001A1001A39I"),
                Q("periodStart", today.ToUniversalTime().ToString("yyyyMMddHHmm")),
                Q("periodEnd", today.ToUniversalTime().AddHours(49).ToString("yyyyMMddHHmm")),
                Q("securityToken", configModel.EntsoeSecurityToken())
                ]);

            var entsoeRawResponse = await httpClient.GetStringAsync(entsoeUrl);
            var entsoeResponse = DeserializeEntsoeResponse(entsoeRawResponse);

            var entsoePrices = ConvertEntsoeResponse(entsoeResponse);
            if (entsoePrices != null)
            {
                validPricePoints = entsoePrices;
            }
        }

        var result = new List<PointValue>();

        foreach (var (item, next) in validPricePoints.Zip(validPricePoints.Skip(1).Append(null)))
        {
            var npsTimestamp = new DateTime(item.Timestamp.Year, item.Timestamp.Month, item.Timestamp.Day, item.Timestamp.Hour, item.Timestamp.Minute / 15 * 15, 0, DateTimeKind.Utc);
            var value = item.Price!.Value / 1000m;

            var numOfPointsToGenerate = npsTimestamp.Minute == 0 && (next == null || next.Timestamp.Minute == 0) ? 12 : 3;

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

    private static NpsPrice[]? ConvertEntsoeResponse(PublicationMarketDocument? entsoeResponse)
    {
        if (entsoeResponse?.TimeSeries == null)
            return null;

        var result = new List<NpsPrice>();

        var formats = new[] { "yyyy-MM-dd'T'HH:mm'Z'", "yyyy-MM-dd'T'HH:mm:ss'Z'", "o" };

        foreach (var ts in entsoeResponse.TimeSeries)
        {
            var period = ts?.Period;
            if (period?.TimeInterval == null || period.Points == null || period.TimeInterval.Start.IsNullOrEmpty())
                continue;

            if (!DateTime.TryParseExact(
                period.TimeInterval.Start,
                formats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var startUtc))
            {
                continue;
            }

            if (period.Points == null || period.Points.Count > 24 * 4)
                continue;

            var points = period.Points.OrderBy(x => x.Position).ToList();
            if (points[0].Position != 1)
                continue;

            Point? lastPoint = null;

            foreach (var pt in points)
            {
                if (lastPoint != null && pt.Position != lastPoint.Position + 1)
                {
                    for (int missingPos = lastPoint.Position + 1; missingPos < pt.Position; missingPos++)
                    {
                        var missingTimestamp = startUtc.AddMinutes(15 * (missingPos - 1));
                        var missingNps = new NpsPrice
                        {
                            Timestamp = missingTimestamp,
                            Price = lastPoint.PriceAmount
                        };
                        result.Add(missingNps);
                    }
                }

                var timestamp = startUtc.AddMinutes(15 * (pt.Position - 1));

                var nps = new NpsPrice
                {
                    Timestamp = timestamp,
                    Price = pt.PriceAmount
                };

                lastPoint = pt;

                result.Add(nps);
            }

            if (points[^1].Position < 24 * 4)
            {
                for (int missingPos = points[^1].Position + 1; missingPos <= 24 * 4; missingPos++)
                {
                    var missingTimestamp = startUtc.AddMinutes(15 * (missingPos - 1));
                    var missingNps = new NpsPrice
                    {
                        Timestamp = missingTimestamp,
                        Price = points[^1].PriceAmount
                    };
                    result.Add(missingNps);
                }
            }
        }

        return [.. result];
    }

    private static PublicationMarketDocument? DeserializeEntsoeResponse(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml)) return null;

        var serializer = new XmlSerializer(typeof(PublicationMarketDocument));
        using var reader = new StringReader(xml);
        return serializer.Deserialize(reader) as PublicationMarketDocument;
    }

    private static KeyValuePair<string, string?> Q(string key, string? value) => new(key, value);

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
            if (timestamp.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday || time < TimeSpan.FromHours(7) || time >= TimeSpan.FromHours(22) || IsNationalHoliday(timestamp))
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

    private static bool IsNationalHoliday(DateTime timestamp)
    {
        return (timestamp.Month, timestamp.Day) switch
        {
            (1, 1) => true,
            (2, 24) => true,
            (5, 1) => true,
            (6, 23) => true,
            (6, 24) => true,
            (8, 20) => true,
            (12, 24) => true,
            (12, 25) => true,
            (12, 26) => true,
            _ => false
        };
        throw new NotImplementedException();
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
        private DateTime? timestamp;

        [JsonPropertyName("timestamp")]
        public long? UnixTimestamp { get; set; }

        [JsonIgnore]
        public DateTime Timestamp
        {
            get
            {
                if (timestamp.HasValue)
                {
                    return timestamp.Value;
                }
                if (UnixTimestamp.HasValue)
                {
                    timestamp = DateTimeOffset.FromUnixTimeSeconds(UnixTimestamp.Value).UtcDateTime;
                    return timestamp.Value;
                }

                return default;
            }
            set => timestamp = value;
        }

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
