using Domain;
using EstfeedConnector;
using EstfeedConnector.Client.Api.Public.V1.MeteringData;
using EstfeedConnector.Client.Models;
using System.Text.Json;

namespace ValueReaderService.Services;

public class EstfeedReader(ILogger<DeviceReader> logger, IHttpClientFactory httpClientFactory) : DeviceReader(logger)
{
    public override bool StorePointsWithReplace => true;

    protected override async Task<IList<PointValue>?> ExecuteAsyncInternal(Device device, DateTime timestamp, ICollection<DevicePoint> devicePoints)
    {
        if (device.Address is null)
            return null;

        var address = JsonSerializer.Deserialize<DeviceAddress>(device.Address);
        if (address is null || address.DeviceId is null || address.ClientId is null || address.ClientSecret is null)
            return null;

        using var httpClient = httpClientFactory.CreateClient(nameof(EstfeedReader));
        var client = await EstfeedClientFactory.Create(httpClient, address.ClientId, address.ClientSecret);
        if (client == null)
            return null;

        var localTime = timestamp.ToLocalTime();

        var response = await client.Api.Public.V1.MeteringData.GetAsync(x =>
        {
            x.QueryParameters.StartDateTime = localTime.Date.AddDays(-5);
            x.QueryParameters.EndDateTime = localTime.Date.AddDays(1);
            x.QueryParameters.ResolutionAsGetResolutionQueryParameterType = GetResolutionQueryParameterType.Fifteen_minutes;
            x.QueryParameters.MeteringPointEics = [address.DeviceId];
        });

        if (response == null || response.Count != 1 || response[0].Error != null)
        {
            Logger.LogWarning("Estfeed response is empty, contains multiple metering points or has an error for device {DeviceId}", address.DeviceId);
            return null;
        }

        var meteringData = response[0];
        if (meteringData.AccountingIntervals == null || meteringData.AccountingIntervals.Count == 0)
            return null;

        var consumption15MinutePoint = devicePoints.FirstOrDefault(x => x.Type == "15-min-consumption");
        var production15MinutePoint = devicePoints.FirstOrDefault(x => x.Type == "15-min-production");
        var consumptionPowerPoint = devicePoints.FirstOrDefault(x => x.Type == "consumption-power");
        var productionPowerPoint = devicePoints.FirstOrDefault(x => x.Type == "production-power");
        var netPowerPoint = devicePoints.FirstOrDefault(x => x.Type == "net-power");

        if (consumption15MinutePoint == null || production15MinutePoint == null || consumptionPowerPoint == null || productionPowerPoint == null || netPowerPoint == null)
        {
            throw new DeviceRunException("One or more required device points are missing.");
        }

        var result = new List<PointValue>();

        var intervals = new Dictionary<DateTime, ApiKeyMeterDataAccountingIntervalDto>();
        foreach (var interval in meteringData.AccountingIntervals)
        {
            var intervalTimestamp = interval.PeriodStart.UtcDateTime;
            if (!intervals.TryGetValue(intervalTimestamp, out var current))
            {
                intervals[intervalTimestamp] = interval;
            }
            else
            {
                if (current.ConsumptionKwh == null)
                    intervals[intervalTimestamp] = interval;
            }
        }

        foreach (var (utcStart, interval) in intervals.OrderBy(x => x.Key))
        {
            var consumptionKwh = interval.ConsumptionKwh;
            var productionKwh = interval.ProductionKwh;

            // Power values (kWh per 15 min -> avg W: consumptionKwh * (60/15) * 1000 = consumptionKwh * 4000)
            // Splitting 15 min interval into 3 x 5 min points to maintain high resolution data
            var consumptionPower = consumptionKwh * 4000;
            var productionPower = productionKwh * 4000;
            var netPower = consumptionPower - productionPower;

            for (int i = 0; i < 3; i++)
            {
                var pointTime = utcStart.AddMinutes(i * 5);

                // 15-min values (repeated for each 5-min point in the window)
                result.Add(new(consumption15MinutePoint, consumptionKwh?.ToString(InvariantCulture), pointTime));
                result.Add(new(production15MinutePoint, productionKwh?.ToString(InvariantCulture), pointTime));

                result.Add(new(consumptionPowerPoint, consumptionPower?.ToString(InvariantCulture), pointTime));
                result.Add(new(productionPowerPoint, productionPower?.ToString(InvariantCulture), pointTime));
                result.Add(new(netPowerPoint, netPower?.ToString(InvariantCulture), pointTime));
            }
        }

        return result;
    }
}
