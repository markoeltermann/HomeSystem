using Domain;
using Microsoft.EntityFrameworkCore;
using PointValueStoreClient.Models;
using SolarmanV5Client.Models;

namespace ValueReaderService.Services.InverterSchedule;

public class InverterScheduleRunner(
    ILogger<DeviceReader> logger,
    HomeSystemContext dbContext,
    PointValueStoreAdapter pointValueStoreAdapter,
    SolarmanV5Adapter solarmanV5Adapter) : DeviceReader(logger, dbContext)
{

    [DevicePointAutoLoad("consumption_calculator", null, "electricity-sell-price")]
    private DevicePoint ElectricitySellPricePoint { get; set; } = null!;

    [DevicePointAutoLoad("deye_inverter", null, "battery-level")]
    private DevicePoint BatteryLevelPoint { get; set; } = null!;

    protected override async Task<IList<PointValue>?> ExecuteAsyncInternal(Device device, DateTime timestamp, ICollection<DevicePoint> devicePoints)
    {
#if !DEBUG
        await Task.Delay(TimeSpan.FromSeconds(10));
#endif
        var batteryLevelPoint = devicePoints.FirstOrDefault(x => x.Address == "battery-level");
        var batterySellLevelPoint = devicePoints.FirstOrDefault(x => x.Address == "battery-sell-level");
        var gridChargeEnablePoint = devicePoints.FirstOrDefault(x => x.Address == "grid-charge-enable");
        var adaptiveSellEnablePoint = devicePoints.FirstOrDefault(x => x.Address == "adaptive-sell-enable");

        if (batteryLevelPoint == null || gridChargeEnablePoint == null || adaptiveSellEnablePoint == null || batterySellLevelPoint == null)
            return null;

        var timestampLocal = timestamp.ToLocalTime().AddSeconds(30);
        var date = DateOnly.FromDateTime(timestampLocal);
        var batteryLevelValues = (await pointValueStoreAdapter.Get(batteryLevelPoint.Id, date, fiveMinResolution: true)).Values!;
        var batterySellLevelValues = (await pointValueStoreAdapter.Get(batterySellLevelPoint.Id, date, fiveMinResolution: true)).Values!;
        var gridChargeEnableValues = (await pointValueStoreAdapter.Get(gridChargeEnablePoint.Id, date, fiveMinResolution: true)).Values!;
        var electricitySellPrices = (await pointValueStoreAdapter.Get(ElectricitySellPricePoint.Id, date, fiveMinResolution: true)).Values!;
        var actualBatteryLevelValues = (await pointValueStoreAdapter.Get(BatteryLevelPoint.Id, date, fiveMinResolution: true)).Values!;
        var adaptiveSellEnableValues = (await pointValueStoreAdapter.Get(adaptiveSellEnablePoint.Id, date, fiveMinResolution: true)).Values!;

        var currentActualBatteryLevel = GetCurrentValue(timestampLocal, actualBatteryLevelValues);
        var currentBatterySellLevel = GetCurrentValue(timestampLocal, batterySellLevelValues);
        var currentGridChargeEnable = GetCurrentValue(timestampLocal, gridChargeEnableValues);

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

        var currentSellPrice = GetCurrentValue(timestampLocal, electricitySellPrices);
        var settings = new InverterSettingsUpdateDto
        {
            IsSolarSellEnabled = currentSellPrice > 0,
        };
        await UpdateInverterSettings(settings);
#if !DEBUG
        await Task.Delay(50);
#endif

        var inverterSettings = await DbContext.InverterSettings.AsNoTracking().FirstOrDefaultAsync();
        if (inverterSettings != null)
        {
            var isAdaptiveSellEnabled = GetCurrentValue(timestampLocal, adaptiveSellEnableValues) > 0.0;
            var currentBatteryLevel = GetCurrentValue(timestampLocal, batteryLevelValues);

            var maxDischargeCurrent = inverterSettings.BatteryDischargeCurrent;
            if (currentActualBatteryLevel <= 20)
                maxDischargeCurrent = inverterSettings.BatteryDischargeCurrentBelow20;
            else if (currentActualBatteryLevel <= 30)
                maxDischargeCurrent = inverterSettings.BatteryDischargeCurrentBelow30;

            var maxChargeCurrent = isAdaptiveSellEnabled && (currentActualBatteryLevel - currentBatteryLevel >= 5.0) ? 0 : inverterSettings.BatteryChargeCurrent;
            if (maxChargeCurrent > 2 && currentActualBatteryLevel >= 99)
                maxChargeCurrent = 2;

            settings = new InverterSettingsUpdateDto
            {
                MaxChargeCurrent = maxChargeCurrent,
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

    private static double GetCurrentValue(DateTime timestampLocal, List<NumericValueDto> values)
    {
        double? value = null;
        for (int i = 0; i < values.Count - 1; i++)
        {
            var price = values[i];
            var nextPrice = values[i + 1];
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

    private static int TruncateBatteryLevel(int level)
    {
        if (level < 0)
            return 0;
        if (level > 100)
            return 100;
        return level;
    }

    private static List<ScheduleItemDto>? GetChangePoints(List<NumericValueDto> chargeValues, List<NumericValueDto> gridValues)
    {
        var hourlyPoints = new List<ScheduleItemDto>();
        for (int i = 0; i < 96; i++)
        {
            var chargeValue = chargeValues[i * 3];
            var gridValue = gridValues[i * 3];

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
}
