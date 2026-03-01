using Domain;
using Microsoft.EntityFrameworkCore;

namespace ValueReaderService.Services;

public class ConsumptionCalculatorRunner(ILogger<DeviceReader> logger, HomeSystemContext dbContext, PointValueStoreAdapter pointValueStoreAdapter) : DeviceReader(logger)
{
    private const double MinSolarElevation = -1.0;

    public override bool StorePointsWithReplace => true;

    protected async override Task<IList<PointValue>?> ExecuteAsyncInternal(Device device, DateTime timestamp, ICollection<DevicePoint> devicePoints)
    {
#if !DEBUG
        await Task.Delay(TimeSpan.FromSeconds(10));
#endif
        var result = new List<PointValue>();
        result.AddRange(await CalculateDayPVEnergyUsingPower(timestamp, devicePoints) ?? []);
        result.AddRange(await CalculateElectricityCosts(timestamp, devicePoints) ?? []);
        return result;
    }

    private async Task<IList<PointValue>?> CalculateElectricityCosts(DateTime timestamp, ICollection<DevicePoint> devicePoints)
    {
        var estfeedDevice = await dbContext.Devices.Include(x => x.DevicePoints).FirstOrDefaultAsync(x => x.Type == "estfeed")
            ?? throw new InvalidOperationException("Estfeed device not found");

        var priceDevice = await dbContext.Devices.Include(x => x.DevicePoints).FirstOrDefaultAsync(x => x.Type == "electricity_price")
            ?? throw new InvalidOperationException("Electricity price device not found");

        var consPoint = estfeedDevice.DevicePoints.FirstOrDefault(x => x.Type == "15-min-consumption")
            ?? throw new InvalidOperationException("Point '15-min-consumption' not found on estfeed device");
        var prodPoint = estfeedDevice.DevicePoints.FirstOrDefault(x => x.Type == "15-min-production")
            ?? throw new InvalidOperationException("Point '15-min-production' not found on estfeed device");
        var buyPricePoint = priceDevice.DevicePoints.FirstOrDefault(x => x.Type == "total-buy-price")
            ?? throw new InvalidOperationException("Point 'total-buy-price' not found on electricity price device");
        var sellPricePoint = priceDevice.DevicePoints.FirstOrDefault(x => x.Type == "total-sell-price")
            ?? throw new InvalidOperationException("Point 'total-sell-price' not found on electricity price device");

        var dayCostPoint = devicePoints.FirstOrDefault(x => x.Type == "day-electricity-cost");
        var monthCostPoint = devicePoints.FirstOrDefault(x => x.Type == "month-electricity-cost");

        if (dayCostPoint == null && monthCostPoint == null)
        {
            return null;
        }

        var timestampLocal = timestamp.ToLocalTime();
        var firstOfThisMonth = new DateOnly(timestampLocal.Year, timestampLocal.Month, 1);
        var startDate = timestampLocal.Day <= 10 ? firstOfThisMonth.AddMonths(-1) : firstOfThisMonth;
        var endDate = DateOnly.FromDateTime(timestampLocal);

        var consValues = (await pointValueStoreAdapter.Get(consPoint.Id, startDate, endDate, fiveMinResolution: true)).Values!;
        var prodValues = (await pointValueStoreAdapter.Get(prodPoint.Id, startDate, endDate, fiveMinResolution: true)).Values!;
        var buyPriceValues = (await pointValueStoreAdapter.Get(buyPricePoint.Id, startDate, endDate, fiveMinResolution: true)).Values!;
        var sellPriceValues = (await pointValueStoreAdapter.Get(sellPricePoint.Id, startDate, endDate, fiveMinResolution: true)).Values!;

        var prodLookup = prodValues.ToDictionary(v => v.Timestamp, v => v.Value);
        var buyPriceLookup = buyPriceValues.ToDictionary(v => v.Timestamp, v => v.Value);
        var sellPriceLookup = sellPriceValues.ToDictionary(v => v.Timestamp, v => v.Value);

        var result = new List<PointValue>();
        double monthAccumulatedCost = 0;
        double dayAccumulatedCost = 0;

        foreach (var consReading in consValues)
        {
            var ts = consReading.Timestamp;
            if (ts > timestampLocal.AddSeconds(1)) break;

            var intervalCons = (consReading.Value ?? 0) / 3.0; // Assume 15-min window is spread over three 5-min intervals
            var intervalProd = (prodLookup.GetValueOrDefault(ts) ?? 0) / 3.0;
            var buyPrice = buyPriceLookup.GetValueOrDefault(ts) ?? 0;
            var sellPrice = sellPriceLookup.GetValueOrDefault(ts) ?? 0;

            var intervalCost = (intervalCons * buyPrice) - (intervalProd * sellPrice);

            dayAccumulatedCost += intervalCost;
            monthAccumulatedCost += intervalCost;

            if (dayCostPoint != null)
            {
                result.Add(new PointValue(dayCostPoint, dayAccumulatedCost.ToString("0.00", InvariantCulture), ts.UtcDateTime));
            }

            if (monthCostPoint != null)
            {
                result.Add(new PointValue(monthCostPoint, monthAccumulatedCost.ToString("0.00", InvariantCulture), ts.UtcDateTime));
            }

            if (ts.Hour == 0 && ts.Minute == 0 && ts.Second == 0)
            {
                dayAccumulatedCost = 0;
                if (ts.Day == 1)
                {
                    monthAccumulatedCost = 0;
                }
            }
        }

        return result;
    }

    private async Task<IList<PointValue>?> CalculateDayPVEnergyUsingPower(DateTime timestamp, ICollection<DevicePoint> devicePoints)
    {
        var inverterDevice = await dbContext.Devices.Include(x => x.DevicePoints).FirstOrDefaultAsync(x => x.Type == "deye_inverter")
            ?? throw new InvalidOperationException("Inverter device not found");

        var solarModelDevice = await dbContext.Devices.Include(x => x.DevicePoints).FirstOrDefaultAsync(x => x.Type == "solar_model")
            ?? throw new InvalidOperationException("Solar model device not found");

        var pvInputPowerPoint = inverterDevice.DevicePoints.FirstOrDefault(x => x.Type == "pv-input-power")
            ?? throw new InvalidOperationException("Point 'pv-input-power' not found on inverter device");

        var solarElevationPoint = solarModelDevice.DevicePoints.FirstOrDefault(x => x.Type == "solar-elevation")
            ?? throw new InvalidOperationException("Point 'solar-elevation' not found on solar model device");

        var dayPvEnergyPoint = devicePoints.FirstOrDefault(x => x.Type == "day-pv-energy");
        if (dayPvEnergyPoint == null)
        {
            return null;
        }

        var timestampLocal = timestamp.ToLocalTime();
        var date = DateOnly.FromDateTime(timestampLocal);

        var pvInputPowerValues = (await pointValueStoreAdapter.Get(pvInputPowerPoint.Id, date, fiveMinResolution: true)).Values;
        var elevationValues = (await pointValueStoreAdapter.Get(solarElevationPoint.Id, date, fiveMinResolution: true)).Values;

        var elevationLookup = elevationValues.ToDictionary(v => v.Timestamp, v => v.Value);

        var result = new List<PointValue>();
        double accumulatedEnergyKWh = 0;

        foreach (var pvReading in pvInputPowerValues)
        {
            if (pvReading.Timestamp > timestampLocal.AddSeconds(1)) break;

            if (pvReading.Value.HasValue && elevationLookup.TryGetValue(pvReading.Timestamp, out var elevation) && elevation >= MinSolarElevation)
            {
                accumulatedEnergyKWh += pvReading.Value.Value / 12000.0;
            }

            result.Add(new PointValue(dayPvEnergyPoint, accumulatedEnergyKWh.ToString("0.00", InvariantCulture), pvReading.Timestamp.UtcDateTime));
        }

        return result;
    }

    private async Task<IList<PointValue>?> CalculateUsingEnergy(DateTime timestamp, ICollection<DevicePoint> devicePoints)
    {
        var inverterDevice = await dbContext.Devices.Include(x => x.DevicePoints).FirstOrDefaultAsync(x => x.Type == "deye_inverter")
                    ?? throw new InvalidOperationException("Inverter device not found");

        var solarModelDevice = await dbContext.Devices.Include(x => x.DevicePoints).FirstOrDefaultAsync(x => x.Type == "solar_model")
            ?? throw new InvalidOperationException("Solar model device not found");

        var totalPvEnergyPoint = inverterDevice.DevicePoints.FirstOrDefault(x => x.Type == "total-pv-energy")
            ?? throw new InvalidOperationException("Point 'total-pv-energy' not found on inverter device");

        var solarElevationPoint = solarModelDevice.DevicePoints.FirstOrDefault(x => x.Type == "solar-elevation")
            ?? throw new InvalidOperationException("Point 'solar-elevation' not found on solar model device");

        var dayPvEnergyPoint = devicePoints.FirstOrDefault(x => x.Type == "day-pv-energy");
        if (dayPvEnergyPoint == null)
        {
            return null;
        }

        var timestampLocal = timestamp.ToLocalTime();
        var date = DateOnly.FromDateTime(timestampLocal);

        var totalPvEnergyValues = (await pointValueStoreAdapter.Get(totalPvEnergyPoint.Id, date, fiveMinResolution: true)).Values;
        var elevationValues = (await pointValueStoreAdapter.Get(solarElevationPoint.Id, date, fiveMinResolution: true)).Values;

        var elevationLookup = elevationValues.ToDictionary(v => v.Timestamp, v => v.Value);

        var anchorPoints = new List<int>();
        for (int i = 0; i < totalPvEnergyValues.Count; i++)
        {
            if (totalPvEnergyValues[i].Timestamp > timestampLocal) break;

            var value = totalPvEnergyValues[i].Value;
            var hasElevation = elevationLookup.TryGetValue(totalPvEnergyValues[i].Timestamp, out var elevation);

            if (value.HasValue && hasElevation && elevation >= MinSolarElevation)
            {
                if (anchorPoints.Count == 0 || value.Value != totalPvEnergyValues[anchorPoints[^1]].Value!.Value)
                {
                    anchorPoints.Add(i);
                }
            }
        }

        var result = new List<PointValue>();

        if (anchorPoints.Count == 0)
        {
            for (int k = 0; k < totalPvEnergyValues.Count; k++)
            {
                if (totalPvEnergyValues[k].Timestamp > timestampLocal)
                    break;

                result.Add(new PointValue(dayPvEnergyPoint, "0.00", totalPvEnergyValues[k].Timestamp.UtcDateTime));
            }
        }
        else
        {
            var firstValValue = totalPvEnergyValues[anchorPoints[0]].Value!.Value;

            int firstAnchorIdx = anchorPoints[0];
            for (int k = 0; k < firstAnchorIdx; k++)
            {
                result.Add(new PointValue(dayPvEnergyPoint, "0.00", totalPvEnergyValues[k].Timestamp.UtcDateTime));
            }

            for (int i = 0; i < anchorPoints.Count - 1; i++)
            {
                int startIdx = anchorPoints[i];
                int endIdx = anchorPoints[i + 1];

                var startPoint = totalPvEnergyValues[startIdx];
                var endPoint = totalPvEnergyValues[endIdx];

                double startVal = startPoint.Value!.Value - firstValValue;
                double endVal = endPoint.Value!.Value - firstValValue;

                for (int k = startIdx; k < endIdx; k++)
                {
                    var currentPoint = totalPvEnergyValues[k];
                    double interpVal;
                    if (startPoint.Timestamp == endPoint.Timestamp)
                    {
                        interpVal = startVal;
                    }
                    else
                    {
                        interpVal = startVal + (endVal - startVal) * (currentPoint.Timestamp - startPoint.Timestamp).TotalSeconds / (endPoint.Timestamp - startPoint.Timestamp).TotalSeconds;
                    }
                    result.Add(new PointValue(dayPvEnergyPoint, interpVal.ToString("0.00", InvariantCulture), currentPoint.Timestamp.UtcDateTime));
                }
            }

            int lastAnchorIdx = anchorPoints[^1];
            double lastVal = totalPvEnergyValues[lastAnchorIdx].Value!.Value - firstValValue;
            for (int k = lastAnchorIdx; k < totalPvEnergyValues.Count; k++)
            {
                if (totalPvEnergyValues[k].Timestamp > timestampLocal)
                    break;

                result.Add(new PointValue(dayPvEnergyPoint, lastVal.ToString("0.00", InvariantCulture), totalPvEnergyValues[k].Timestamp.UtcDateTime));
            }
        }

        return result;
    }
}
