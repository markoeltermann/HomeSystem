using Domain;
using Microsoft.EntityFrameworkCore;

namespace ValueReaderService.Services;

public class ConsumptionCalculatorRunner(ILogger<DeviceReader> logger, HomeSystemContext dbContext, PointValueStoreAdapter pointValueStoreAdapter, ConfigModel configModel) : DeviceReader(logger)
{
    private const double MinSolarElevation = -1.0;

    public override bool StorePointsWithReplace => true;

    protected async override Task<IList<PointValue>?> ExecuteAsyncInternal(Device device, DateTime timestamp, ICollection<DevicePoint> devicePoints)
    {
#if !DEBUG
        await Task.Delay(TimeSpan.FromSeconds(10));
#endif
        var result = new List<PointValue>();
        result.AddRange(await CalculatePrices(timestamp, devicePoints) ?? []);
        //result.AddRange(await CalculateDayPVEnergyUsingPower(timestamp, devicePoints) ?? []);
        result.AddRange(await CalculateElectricityCosts(timestamp, devicePoints) ?? []);
        return result;
    }

    private async Task<IList<PointValue>?> CalculateElectricityCosts(DateTime timestamp, ICollection<DevicePoint> devicePoints)
    {
        var estfeedDevice = await dbContext.Devices.Include(x => x.DevicePoints).FirstOrDefaultAsync(x => x.Type == "estfeed")
            ?? throw new InvalidOperationException("Estfeed device not found");

        var consumptionPoint = estfeedDevice.DevicePoints.FirstOrDefault(x => x.Type == "15-min-consumption")
            ?? throw new InvalidOperationException("Point '15-min-consumption' not found on estfeed device");
        var productionPoint = estfeedDevice.DevicePoints.FirstOrDefault(x => x.Type == "15-min-production")
            ?? throw new InvalidOperationException("Point '15-min-production' not found on estfeed device");
        var totalElectricityBuyPricePoint = devicePoints.FirstOrDefault(x => x.Type == "total-electricity-buy-price")
            ?? throw new InvalidOperationException("Point 'total-electricity-buy-price' not found on current device");
        var electricityBuyPricePoint = devicePoints.FirstOrDefault(x => x.Type == "electricity-buy-price")
            ?? throw new InvalidOperationException("Point 'electricity-buy-price' not found on current device");
        var gridBuyPricePoint = devicePoints.FirstOrDefault(x => x.Type == "grid-buy-price")
            ?? throw new InvalidOperationException("Point 'grid-buy-price' not found on current device");
        var electricitySellPricePoint = devicePoints.FirstOrDefault(x => x.Type == "electricity-sell-price")
            ?? throw new InvalidOperationException("Point 'electricity-sell-price' not found on current device");

        var dayCostPoint = devicePoints.FirstOrDefault(x => x.Type == "day-electricity-cost");
        var monthTotalCostPoint = devicePoints.FirstOrDefault(x => x.Type == "month-total-electricity-cost");
        var monthElectricityCostPoint = devicePoints.FirstOrDefault(x => x.Type == "month-electricity-cost");
        var monthGridCostPoint = devicePoints.FirstOrDefault(x => x.Type == "month-grid-cost");

        if (dayCostPoint == null || monthTotalCostPoint == null || monthElectricityCostPoint == null || monthGridCostPoint == null)
        {
            return null;
        }

        var timestampLocal = timestamp.ToLocalTime();
        var firstOfThisMonth = new DateOnly(timestampLocal.Year, timestampLocal.Month, 1);
        //var startDate = timestampLocal.Day <= 10 ? firstOfThisMonth.AddMonths(-1) : firstOfThisMonth;
        var startDate = firstOfThisMonth.AddMonths(-1);
        var endDate = DateOnly.FromDateTime(timestampLocal);

        var consumptionValues = (await pointValueStoreAdapter.Get(consumptionPoint.Id, startDate, endDate, fiveMinResolution: true, utc: true)).Values!;
        var productionValues = (await pointValueStoreAdapter.Get(productionPoint.Id, startDate, endDate, fiveMinResolution: true, utc: true)).Values!;
        var totalElectricityBuyPriceValues = (await pointValueStoreAdapter.Get(totalElectricityBuyPricePoint.Id, startDate, endDate, fiveMinResolution: true, utc: true)).Values!;
        var electricityBuyPriceValues = (await pointValueStoreAdapter.Get(electricityBuyPricePoint.Id, startDate, endDate, fiveMinResolution: true, utc: true)).Values!;
        var gridBuyPriceValues = (await pointValueStoreAdapter.Get(gridBuyPricePoint.Id, startDate, endDate, fiveMinResolution: true, utc: true)).Values!;
        var electricitySellPriceValues = (await pointValueStoreAdapter.Get(electricitySellPricePoint.Id, startDate, endDate, fiveMinResolution: true, utc: true)).Values!;

        var productionLookup = productionValues.ToDictionary(v => v.Timestamp, v => v.Value);
        var totalElectricityBuyPriceLookup = totalElectricityBuyPriceValues.ToDictionary(v => v.Timestamp, v => v.Value);
        var electricityBuyPriceLookup = electricityBuyPriceValues.ToDictionary(v => v.Timestamp, v => v.Value);
        var gridBuyPriceLookup = gridBuyPriceValues.ToDictionary(v => v.Timestamp, v => v.Value);
        var electricitySellPriceLookup = electricitySellPriceValues.ToDictionary(v => v.Timestamp, v => v.Value);

        var result = new List<PointValue>();
        var monthAccumulatedTotalCost = 0.0;
        var monthAccumulatedElectricityCost = 0.0;
        var monthAccumulatedGridCost = 0.0;
        var dayAccumulatedCost = 0.0;
        var isDayDataMissing = false;

        foreach (var consumptionReading in consumptionValues)
        {
            var readingTimestamp = consumptionReading.Timestamp;
            if (readingTimestamp > timestampLocal.AddSeconds(1)) break;

            var consumption = consumptionReading.Value;
            var production = productionLookup.GetValueOrDefault(readingTimestamp);

            if (consumption == null || production == null)
            {
                isDayDataMissing = true;
            }

            var intervalConsumption = (consumption ?? 0) / 3.0;
            var intervalProduction = (production ?? 0) / 3.0;
            var totalElectricityBuyPrice = totalElectricityBuyPriceLookup.GetValueOrDefault(readingTimestamp) ?? 0;
            var electricityBuyPrice = electricityBuyPriceLookup.GetValueOrDefault(readingTimestamp) ?? 0;
            var gridBuyPrice = gridBuyPriceLookup.GetValueOrDefault(readingTimestamp) ?? 0;
            var electricitySellPrice = electricitySellPriceLookup.GetValueOrDefault(readingTimestamp) ?? 0;

            var intervalTotalCost = (intervalConsumption * totalElectricityBuyPrice) - (intervalProduction * electricitySellPrice);
            var intervalElectricityCost = (intervalConsumption * electricityBuyPrice) - (intervalProduction * electricitySellPrice);
            var intervalGridCost = intervalConsumption * gridBuyPrice;

            dayAccumulatedCost += intervalTotalCost;
            monthAccumulatedTotalCost += intervalTotalCost;
            monthAccumulatedElectricityCost += intervalElectricityCost;
            monthAccumulatedGridCost += intervalGridCost;

            result.Add(new PointValue(dayCostPoint, isDayDataMissing ? null : dayAccumulatedCost.ToString("0.00", InvariantCulture), readingTimestamp.UtcDateTime));
            result.Add(new PointValue(monthTotalCostPoint, monthAccumulatedTotalCost.ToString("0.00", InvariantCulture), readingTimestamp.UtcDateTime));
            result.Add(new PointValue(monthElectricityCostPoint, monthAccumulatedElectricityCost.ToString("0.00", InvariantCulture), readingTimestamp.UtcDateTime));
            result.Add(new PointValue(monthGridCostPoint, monthAccumulatedGridCost.ToString("0.00", InvariantCulture), readingTimestamp.UtcDateTime));

            var readingTimestampLocal = readingTimestamp.ToLocalTime();
            if (readingTimestampLocal.Hour == 0 && readingTimestampLocal.Minute == 0 && readingTimestampLocal.Second == 0)
            {
                dayAccumulatedCost = 0;
                isDayDataMissing = false;
                if (readingTimestampLocal.Day == 1)
                {
                    monthAccumulatedTotalCost = 0;
                    monthAccumulatedElectricityCost = 0;
                    monthAccumulatedGridCost = 0;
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

        var pvInputPowerValues = (await pointValueStoreAdapter.Get(pvInputPowerPoint.Id, date, fiveMinResolution: true)).Values!;
        var elevationValues = (await pointValueStoreAdapter.Get(solarElevationPoint.Id, date, fiveMinResolution: true)).Values!;

        var elevationLookup = elevationValues.ToDictionary(v => v.Timestamp, v => v.Value);

        var result = new List<PointValue>();
        var accumulatedEnergyKWh = 0.0;

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

        var totalPvEnergyValues = (await pointValueStoreAdapter.Get(totalPvEnergyPoint.Id, date, fiveMinResolution: true)).Values!;
        var elevationValues = (await pointValueStoreAdapter.Get(solarElevationPoint.Id, date, fiveMinResolution: true)).Values!;

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
            var firstValue = totalPvEnergyValues[anchorPoints[0]].Value!.Value;

            int firstAnchorIndex = anchorPoints[0];
            for (int k = 0; k < firstAnchorIndex; k++)
            {
                result.Add(new PointValue(dayPvEnergyPoint, "0.00", totalPvEnergyValues[k].Timestamp.UtcDateTime));
            }

            for (int i = 0; i < anchorPoints.Count - 1; i++)
            {
                int startIdx = anchorPoints[i];
                int endIdx = anchorPoints[i + 1];

                var startPoint = totalPvEnergyValues[startIdx];
                var endPoint = totalPvEnergyValues[endIdx];

                var startValue = startPoint.Value!.Value - firstValue;
                var endValue = endPoint.Value!.Value - firstValue;

                for (int k = startIdx; k < endIdx; k++)
                {
                    var currentPoint = totalPvEnergyValues[k];
                    var interpolatedValue = 0.0;
                    if (startPoint.Timestamp == endPoint.Timestamp)
                    {
                        interpolatedValue = startValue;
                    }
                    else
                    {
                        interpolatedValue = startValue + (endValue - startValue) * (currentPoint.Timestamp - startPoint.Timestamp).TotalSeconds / (endPoint.Timestamp - startPoint.Timestamp).TotalSeconds;
                    }
                    result.Add(new PointValue(dayPvEnergyPoint, interpolatedValue.ToString("0.00", InvariantCulture), currentPoint.Timestamp.UtcDateTime));
                }
            }

            int lastAnchorIndex = anchorPoints[^1];
            var lastValue = totalPvEnergyValues[lastAnchorIndex].Value!.Value - firstValue;
            for (int k = lastAnchorIndex; k < totalPvEnergyValues.Count; k++)
            {
                if (totalPvEnergyValues[k].Timestamp > timestampLocal)
                    break;

                result.Add(new PointValue(dayPvEnergyPoint, lastValue.ToString("0.00", InvariantCulture), totalPvEnergyValues[k].Timestamp.UtcDateTime));
            }
        }

        return result;
    }

    private async Task<IList<PointValue>?> CalculatePrices(DateTime timestamp, ICollection<DevicePoint> devicePoints)
    {
        var priceDevice = await dbContext.Devices.Include(x => x.DevicePoints).FirstOrDefaultAsync(x => x.Type == "electricity_price")
            ?? throw new InvalidOperationException("Electricity price device not found");

        var gridPriceRawPoint = priceDevice.DevicePoints.FirstOrDefault(x => x.Type == "grid-price-raw")
            ?? throw new InvalidOperationException("Point 'grid-price-raw' not found");
        var npsPriceRawPoint = priceDevice.DevicePoints.FirstOrDefault(x => x.Type == "nps-price-raw")
            ?? throw new InvalidOperationException("Point 'nps-price-raw' not found");

        var gridBuyPricePoint = devicePoints.FirstOrDefault(x => x.Type == "grid-buy-price");
        var electricityBuyPricePoint = devicePoints.FirstOrDefault(x => x.Type == "electricity-buy-price");
        var electricitySellPricePoint = devicePoints.FirstOrDefault(x => x.Type == "electricity-sell-price");
        var totalBuyPricePoint = devicePoints.FirstOrDefault(x => x.Type == "total-electricity-buy-price");

        if (gridBuyPricePoint == null || electricityBuyPricePoint == null || electricitySellPricePoint == null || totalBuyPricePoint == null)
        {
            return null;
        }

        var timestampLocal = timestamp.ToLocalTime();

        var firstOfThisMonth = new DateOnly(timestampLocal.Year, timestampLocal.Month, 1).AddMonths(-1);
        var startDate = timestampLocal.Day <= 10 ? firstOfThisMonth.AddMonths(-1) : firstOfThisMonth;
        var endDate = DateOnly.FromDateTime(timestampLocal.AddDays(1));

        //var startDate = DateOnly.FromDateTime(timestampLocal.AddDays(-1));
        //var endDate = startDate.AddDays(2);

        var gridPrices = (await pointValueStoreAdapter.Get(gridPriceRawPoint.Id, startDate, endDate, fiveMinResolution: true)).Values!;
        var npsPrices = (await pointValueStoreAdapter.Get(npsPriceRawPoint.Id, startDate, endDate, fiveMinResolution: true)).Values!;

        var npsLookup = npsPrices.ToDictionary(v => v.Timestamp, v => v.Value);

        var result = new List<PointValue>();
        var vat = (double)configModel.ValueAddedTax();
        var vatMultiplier = 1 + (vat / 100.0);
        var electricitySaleMargin = (double)configModel.ElectricitySaleMargin();
        var electricityPurchaseMargin = (double)configModel.ElectricityPurchaseMargin();
        var gridPurchaseMargin = (double)configModel.GridPurchaseMargin();

        foreach (var gridReading in gridPrices)
        {
            var readingTimestamp = gridReading.Timestamp;
            var rawGrid = gridReading.Value;
            var rawNps = npsLookup.GetValueOrDefault(readingTimestamp);

            if (rawGrid.HasValue && rawNps.HasValue)
            {
                var gridBuyPrice = (rawGrid.Value + gridPurchaseMargin) * vatMultiplier;
                var electricityBuyPrice = (rawNps.Value + electricityPurchaseMargin) * vatMultiplier;
                var electricitySellPrice = rawNps.Value + electricitySaleMargin;
                var totalBuyPrice = gridBuyPrice + electricityBuyPrice;

                result.Add(new PointValue(gridBuyPricePoint, gridBuyPrice.ToString("0.00000", InvariantCulture), readingTimestamp.UtcDateTime));
                result.Add(new PointValue(electricityBuyPricePoint, electricityBuyPrice.ToString("0.00000", InvariantCulture), readingTimestamp.UtcDateTime));
                result.Add(new PointValue(electricitySellPricePoint, electricitySellPrice.ToString("0.00000", InvariantCulture), readingTimestamp.UtcDateTime));
                result.Add(new PointValue(totalBuyPricePoint, totalBuyPrice.ToString("0.00000", InvariantCulture), readingTimestamp.UtcDateTime));
            }
        }

        return result;
    }
}

