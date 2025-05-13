using CommonLibrary.Helpers;
using Domain;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Json;

namespace ValueReaderService.Services.InverterSchedule;
public class InverterScheduleRunner(
    ILogger<DeviceReader> logger,
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory,
    HomeSystemContext dbContext,
    ConfigModel configModel) : DeviceReader(logger)
{
    protected override async Task<IList<PointValue>?> ExecuteAsyncInternal(Device device, DateTime timestamp, ICollection<DevicePoint> devicePoints)
    {
#if !DEBUG
        await Task.Delay(TimeSpan.FromSeconds(10));
#endif

        var inverterConnectorBaseUrl = configuration["ModbusConnectorUrl"];
        if (string.IsNullOrWhiteSpace(inverterConnectorBaseUrl))
            return null;

        var pointValueStoreConnectorBaseUrl = configuration["PointValueStoreConnectorUrl"];
        if (string.IsNullOrWhiteSpace(pointValueStoreConnectorBaseUrl))
            return null;

        var saleMargin = configModel.ElectricitySaleMargin();

        var batteryLevelPoint = devicePoints.FirstOrDefault(x => x.Address == "battery-level");
        var gridChargeEnablePoint = devicePoints.FirstOrDefault(x => x.Address == "grid-charge-enable");
        var adaptiveSellEnablePoint = devicePoints.FirstOrDefault(x => x.Address == "adaptive-sell-enable");

        if (batteryLevelPoint == null || gridChargeEnablePoint == null || adaptiveSellEnablePoint == null)
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

        using var httpClient = httpClientFactory.CreateClient(nameof(InverterScheduleRunner));

        var timestampLocal = timestamp.ToLocalTime().AddSeconds(30);
        var date = DateOnly.FromDateTime(timestampLocal);
        var batteryLevelValues = await httpClient.GetFromJsonAsync<ValueContainerDto>(GetPointValueRequestUrl(pointValueStoreConnectorBaseUrl, batteryLevelPoint.Id, date, date));
        var gridChargeEnableValues = await httpClient.GetFromJsonAsync<ValueContainerDto>(GetPointValueRequestUrl(pointValueStoreConnectorBaseUrl, gridChargeEnablePoint.Id, date, date));
        var electricityPrices = await httpClient.GetFromJsonAsync<ValueContainerDto>(GetPointValueRequestUrl(pointValueStoreConnectorBaseUrl, electricityPricePoint.Id, date, date));
        var actualBatteryLevelValues = await httpClient.GetFromJsonAsync<ValueContainerDto>(GetPointValueRequestUrl(pointValueStoreConnectorBaseUrl, actualBatteryLevelPoint.Id, date, date));
        var adaptiveSellEnableValues = await httpClient.GetFromJsonAsync<ValueContainerDto>(GetPointValueRequestUrl(pointValueStoreConnectorBaseUrl, adaptiveSellEnablePoint.Id, date, date));

        if (!ValidateValues(batteryLevelValues, true))
            return null;

        if (ValidateValues(gridChargeEnableValues, true))
        {
            var changePoints = GetChangePoints(batteryLevelValues, gridChargeEnableValues);
            if (changePoints == null)
                return null;

            var currentHour = timestampLocal.Hour;
            if (currentHour > 0)
                currentHour--;

            var schedule = InverterScheduleHelpers.GetCurrentSchedule(changePoints, currentHour);

            var scheduleUpdateUrl = UrlHelpers.GetUrl(inverterConnectorBaseUrl, "schedule", null);
            var result = await httpClient.PutAsJsonAsync(scheduleUpdateUrl, schedule);
            if (!result.IsSuccessStatusCode)
            {
                Logger.LogError("Schedule update request failed with status {ResponseStatus}", result.StatusCode);
            }
#if !DEBUG
            await Task.Delay(50);
#endif
        }


        if (ValidateValues(electricityPrices))
        {
            var currentPrice = GetCurrentValue(timestampLocal, electricityPrices);
            if (currentPrice != null)
            {
                var settings = new InverterSettingsUpdateDto
                {
                    IsSolarSellEnabled = (decimal)currentPrice.Value > saleMargin
                };
                await UpdateInverterSettings(inverterConnectorBaseUrl, httpClient, settings);
#if !DEBUG
                await Task.Delay(50);
#endif
            }
        }


        if (ValidateValues(actualBatteryLevelValues) && ValidateValues(adaptiveSellEnableValues))
        {
            var inverterSettings = await dbContext.InverterSettings.AsNoTracking().FirstOrDefaultAsync();
            if (inverterSettings != null)
            {
                var isAdaptiveSellEnabled = (GetCurrentValue(timestampLocal, adaptiveSellEnableValues) ?? 0.0) > 0.0;
                var currentActualBatteryLevel = GetCurrentValue(timestampLocal, actualBatteryLevelValues);
                var currentBatteryLevel = GetCurrentValue(timestampLocal, batteryLevelValues);

                if (currentBatteryLevel != null && currentActualBatteryLevel != null)
                {
                    var settings = new InverterSettingsUpdateDto
                    {
                        MaxChargeCurrent = isAdaptiveSellEnabled && (currentActualBatteryLevel.Value - currentBatteryLevel.Value >= 5.0) ? 0 : inverterSettings.BatteryChargeCurrent,
                    };
                    await UpdateInverterSettings(inverterConnectorBaseUrl, httpClient, settings);
#if !DEBUG
                    await Task.Delay(50);
#endif
                }
            }
        }


        return null;
    }

    private async Task UpdateInverterSettings(string inverterConnectorBaseUrl, HttpClient httpClient, InverterSettingsUpdateDto settings)
    {
        var settingsUpdateUrl = UrlHelpers.GetUrl(inverterConnectorBaseUrl, "inverter-settings", null);
        var result = await httpClient.PutAsJsonAsync(settingsUpdateUrl, settings);
        if (!result.IsSuccessStatusCode)
        {
            Logger.LogError("Settings update request failed with status {ResponseStatus}", result.StatusCode);
        }
    }

    private static double? GetCurrentValue(DateTime timestampLocal, ValueContainerDto values)
    {
        double? value = null;
        for (int i = 0; i < values.Values.Length - 1; i++)
        {
            var price = values.Values[i];
            var nextPrice = values.Values[i + 1];
            if (price.Timestamp <= timestampLocal && nextPrice.Timestamp > timestampLocal)
            {
                value = price.Value;
                break;
            }
        }

        return value;
    }

    private static bool ValidateValues([NotNullWhen(true)] ValueContainerDto? batteryLevelValues, bool firstMustBeSet = false)
    {
        if (batteryLevelValues != null && batteryLevelValues.Values != null && batteryLevelValues.Values.Length == 24 * 6 + 1)
        {
            return !firstMustBeSet || batteryLevelValues.Values[0].Value != null;
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

    private static List<ScheduleItemDto>? GetChangePoints(ValueContainerDto chargeValues, ValueContainerDto gridValues)
    {
        var hourlyPoints = new List<ScheduleItemDto>();
        for (int i = 0; i < 24; i++)
        {
            var chargeValue = chargeValues.Values[i * 6];
            var gridValue = gridValues.Values[i * 6];

            if (chargeValue.Value.HasValue)
            {
                var hourlyPoint = new ScheduleItemDto
                {
                    Time = new TimeOnly(i, 0),
                    MaxBatteryPower = 10000,
                    BatteryChargeLevel = TruncateBatteryLevel((int)chargeValue.Value.Value),
                    IsGridChargeEnabled = (gridValue.Value ?? 0) > 0.0
                };
                hourlyPoints.Add(hourlyPoint);
            }
        }
        if (hourlyPoints.Count == 0 || hourlyPoints[0].Time.Hour != 0)
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

    private static string? GetPointValueRequestUrl(string baseUrl, int pointId, DateOnly from, DateOnly upTo)
    {
        var url = UrlHelpers.GetUrl(baseUrl, $"points/{pointId}/values",
            [KeyValuePair.Create("from", (string?)from.ToString("yyyy-MM-dd")),
            KeyValuePair.Create("upTo", (string?)upTo.ToString("yyyy-MM-dd"))]);

        return url;
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
