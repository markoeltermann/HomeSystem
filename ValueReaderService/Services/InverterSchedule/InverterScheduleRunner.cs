using Domain;
using Microsoft.EntityFrameworkCore;
using PointValueStoreClient.Models;
using SolarmanV5Client.Models;
using System.Diagnostics.CodeAnalysis;

namespace ValueReaderService.Services.InverterSchedule;

public class InverterScheduleRunner(
    ILogger<DeviceReader> logger,
    HomeSystemContext dbContext,
    PointValueStoreAdapter pointValueStoreAdapter,
    SolarmanV5Adapter solarmanV5Adapter,
    ConfigModel configModel) : DeviceReader(logger)
{
    protected override async Task<IList<PointValue>?> ExecuteAsyncInternal(Device device, DateTime timestamp, ICollection<DevicePoint> devicePoints)
    {
#if !DEBUG
        await Task.Delay(TimeSpan.FromSeconds(10));
#endif
        var saleMargin = configModel.ElectricitySaleMargin();

        var batteryLevelPoint = devicePoints.FirstOrDefault(x => x.Address == "battery-level");
        var batterySellLevelPoint = devicePoints.FirstOrDefault(x => x.Address == "battery-sell-level");
        var gridChargeEnablePoint = devicePoints.FirstOrDefault(x => x.Address == "grid-charge-enable");
        var adaptiveSellEnablePoint = devicePoints.FirstOrDefault(x => x.Address == "adaptive-sell-enable");

        if (batteryLevelPoint == null || gridChargeEnablePoint == null || adaptiveSellEnablePoint == null || batterySellLevelPoint == null)
            return null;

        var devices = await dbContext.Devices.AsNoTracking().Include(x => x.DevicePoints).Where(x => x.Type == "electricity_price" || x.Type == "deye_inverter").ToArrayAsync();

        var electricityPriceDevice = devices.FirstOrDefault(x => x.Type == "electricity_price");
        var electricityPricePoint = electricityPriceDevice?.DevicePoints.FirstOrDefault(x => x.Address == "nps-price-raw");
        if (electricityPricePoint == null)
            return null;

        var inverterDevice = devices.FirstOrDefault(x => x.Type == "deye_inverter");
        var actualBatteryLevelPoint = inverterDevice?.DevicePoints.FirstOrDefault(x => x.Type == "battery-level");
        if (actualBatteryLevelPoint == null)
            return null;

        var timestampLocal = timestamp.ToLocalTime().AddSeconds(30);
        var date = DateOnly.FromDateTime(timestampLocal);
        var batteryLevelValues = await pointValueStoreAdapter.Get(batteryLevelPoint.Id, date, fiveMinResolution: true);
        var batterySellLevelValues = await pointValueStoreAdapter.Get(batterySellLevelPoint.Id, date, fiveMinResolution: true);
        var gridChargeEnableValues = await pointValueStoreAdapter.Get(gridChargeEnablePoint.Id, date, fiveMinResolution: true);
        var electricityPrices = await pointValueStoreAdapter.Get(electricityPricePoint.Id, date, fiveMinResolution: true);
        var actualBatteryLevelValues = await pointValueStoreAdapter.Get(actualBatteryLevelPoint.Id, date, fiveMinResolution: true);
        var adaptiveSellEnableValues = await pointValueStoreAdapter.Get(adaptiveSellEnablePoint.Id, date, fiveMinResolution: true);

        var currentActualBatteryLevel = GetCurrentValue(timestampLocal, actualBatteryLevelValues);
        var currentBatterySellLevel = GetCurrentValue(timestampLocal, batterySellLevelValues);

        if (!ValidateValues(batteryLevelValues))
            return null;

        if (ValidateValues(gridChargeEnableValues))
        {
            var changePoints = GetChangePoints(batteryLevelValues, gridChargeEnableValues);
            if (changePoints == null)
                return null;

            var currentHour = timestampLocal.Hour;
            if (currentHour > 0)
                currentHour--;

            var schedule = InverterScheduleHelpers.GetCurrentSchedule(changePoints, currentHour);

            foreach (var item in schedule.GetItems())
            {
                item.IsGridSellEnabled = currentActualBatteryLevel > currentBatterySellLevel;
            }

            await solarmanV5Adapter.Client.Schedule.PutAsync(schedule);
#if !DEBUG
            await Task.Delay(50);
#endif
        }

        var currentPrice = GetCurrentValue(timestampLocal, electricityPrices);
        var settings = new InverterSettingsUpdateDto
        {
            IsSolarSellEnabled = (decimal)currentPrice > saleMargin
        };
        await UpdateInverterSettings(settings);
#if !DEBUG
        await Task.Delay(50);
#endif

        var inverterSettings = await dbContext.InverterSettings.AsNoTracking().FirstOrDefaultAsync();
        if (inverterSettings != null)
        {
            var isAdaptiveSellEnabled = GetCurrentValue(timestampLocal, adaptiveSellEnableValues) > 0.0;
            var currentBatteryLevel = GetCurrentValue(timestampLocal, batteryLevelValues);

            var maxDischargeCurrent = inverterSettings.BatteryDischargeCurrent;
            if (currentActualBatteryLevel <= 20)
                maxDischargeCurrent = inverterSettings.BatteryDischargeCurrentBelow20;
            else if (currentActualBatteryLevel <= 30)
                maxDischargeCurrent = inverterSettings.BatteryDischargeCurrentBelow30;

            settings = new InverterSettingsUpdateDto
            {
                MaxChargeCurrent = isAdaptiveSellEnabled && (currentActualBatteryLevel - currentBatteryLevel >= 5.0) ? 0 : inverterSettings.BatteryChargeCurrent,
                MaxDischargeCurrent = maxDischargeCurrent,
            };
            await UpdateInverterSettings(settings);
#if !DEBUG
            await Task.Delay(50);
#endif
        }

        return null;
    }

    private async Task UpdateInverterSettings(InverterSettingsUpdateDto settings)
    {
        await solarmanV5Adapter.Client.InverterSettings.PutAsync(settings);
    }

    private static double GetCurrentValue(DateTime timestampLocal, ResponseValueContainerDto values)
    {
        double? value = null;
        for (int i = 0; i < values.Values!.Count - 1; i++)
        {
            var price = values.Values[i];
            var nextPrice = values.Values[i + 1];
            if (price.Timestamp <= timestampLocal && nextPrice.Timestamp > timestampLocal)
            {
                value = price.Value;
                break;
            }
        }

        if (value == null)
            throw new DeviceRunException("Could not find current value for point");

        return value.Value;
    }

    private static bool ValidateValues([NotNullWhen(true)] ResponseValueContainerDto? batteryLevelValues)
    {
        if (batteryLevelValues != null && batteryLevelValues.Values != null && batteryLevelValues.Values.Count == 24 * 12 + 1)
        {
            return batteryLevelValues.Values[0].Value != null;
        }

        return false;
    }

    private static int TruncateBatteryLevel(int level)
    {
        if (level < 0)
            return 0;
        if (level > 100)
            return 100;
        return level;
    }

    private static List<ScheduleItemDto>? GetChangePoints(ResponseValueContainerDto chargeValues, ResponseValueContainerDto gridValues)
    {
        var hourlyPoints = new List<ScheduleItemDto>();
        for (int i = 0; i < 96; i++)
        {
            var chargeValue = chargeValues.Values![i * 3];
            var gridValue = gridValues.Values![i * 3];

            if (chargeValue.Value.HasValue)
            {
                var hourlyPoint = new ScheduleItemDto
                {
                    Time = new TimeOnly(i / 4, (i % 4) * 15),
                    MaxBatteryPower = 10000,
                    BatteryChargeLevel = TruncateBatteryLevel((int)chargeValue.Value.Value),
                    IsGridChargeEnabled = (gridValue.Value ?? 0) > 0.0
                };
                hourlyPoints.Add(hourlyPoint);
            }
        }
        if (hourlyPoints.Count == 0 || hourlyPoints[0].Time!.Hour != 0)
            return null;

        var changePoints = new List<ScheduleItemDto> { hourlyPoints[0] };
        var previous = hourlyPoints[0];
        for (var i = 1; i < hourlyPoints.Count; i++)
        {
            var h = hourlyPoints[i];
            if (h.BatteryChargeLevel != previous.BatteryChargeLevel || h.IsGridChargeEnabled != previous.IsGridChargeEnabled)
            {
                previous = h;
                changePoints.Add(h);
            }
        }

        return changePoints;
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
}
