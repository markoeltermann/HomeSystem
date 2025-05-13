using Domain;
using System.Net.Http.Json;

namespace ValueReaderService.Services.YrNoWeatherForecast;
public class YrNoWeatherForecastReader(ILogger<DeviceReader> logger, ConfigModel configModel, IHttpClientFactory httpClientFactory) : DeviceReader(logger)
{
    public override bool StorePointsWithReplace => true;

    protected override async Task<IList<PointValue>?> ExecuteAsyncInternal(Device device, DateTime timestamp, ICollection<DevicePoint> devicePoints)
    {
        var localTimestamp = timestamp.ToLocalTime().AddSeconds(30);

#if !DEBUG
        if (localTimestamp.Hour is not 12 and not 13 and not 14 || localTimestamp.Minute is >= 20 or < 10)
            return null;
        var secondsToWait = Random.Shared.Next(60);
        await Task.Delay(TimeSpan.FromSeconds(secondsToWait));
#endif

        var lat = configModel.WeatherForecastLatitude();
        var lon = configModel.WeatherForecastLongitude();
        var altitude = configModel.WeatherForecastAltitude();
        var email = configModel.WeatherForecastContactEmail();

        var url = "https://api.met.no/weatherapi/locationforecast/2.0/complete?" +
            $"altitude={altitude.ToString(InvariantCulture)}&lat={lat.ToString(InvariantCulture)}&lon={lon.ToString(InvariantCulture)}";

        using var httpClient = httpClientFactory.CreateClient(nameof(YrNoWeatherForecastReader));
        httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", $"HomeSystem {email}");

        var forecast = await httpClient.GetFromJsonAsync<METJSONForecast>(url);
        var timeseries = forecast?.Properties?.Timeseries;
        if (timeseries == null || timeseries.Length == 0)
        {
            Logger.LogWarning("Invalid weather forecast response received.");
            return null;
        }

        var maxDate = localTimestamp.Date.AddDays(3);

        timeseries = timeseries.Where(x => x.Time.HasValue && x.Time.Value < maxDate && x.Data?.Instant?.Details != null).ToArray();
        if (timeseries.Length == 0)
        {
            Logger.LogWarning("No valid time series received.");
            return null;
        }

        var result = new List<PointValue>();

        foreach (var item in timeseries)
        {
            var details = item.Data!.Instant!.Details!;

            decimal? value = null;
            foreach (var point in devicePoints)
            {
                value = point.Address switch
                {
                    "air_temperature" => details.AirTemperature,
                    "cloud_area_fraction" => details.CloudAreaFraction,
                    "cloud_area_fraction_high" => details.CloudAreaFractionHigh,
                    "cloud_area_fraction_low" => details.CloudAreaFractionLow,
                    "cloud_area_fraction_medium" => details.CloudAreaFractionMedium,
                    "dew_point_temperature" => details.DewPointTemperature,
                    "fog_area_fraction" => details.FogAreaFraction,
                    "relative_humidity" => details.RelativeHumidity,
                    "wind_from_direction" => details.WindFromDirection,
                    "wind_speed" => details.WindSpeed,
                    "wind_speed_of_gust" => details.WindSpeedOfGust,
                    _ => null,
                };
                if (value != null)
                {
                    var t = item.Time!.Value.ToUniversalTime();
                    result.Add(new(point, value.Value.ToString(InvariantCulture), t));
                    result.Add(new(point, value.Value.ToString(InvariantCulture), t.AddMinutes(10)));
                    result.Add(new(point, value.Value.ToString(InvariantCulture), t.AddMinutes(20)));
                    result.Add(new(point, value.Value.ToString(InvariantCulture), t.AddMinutes(30)));
                    result.Add(new(point, value.Value.ToString(InvariantCulture), t.AddMinutes(40)));
                    result.Add(new(point, value.Value.ToString(InvariantCulture), t.AddMinutes(50)));
                }
            }
        }

        return result;
    }
}
