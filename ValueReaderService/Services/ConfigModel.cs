using CommonLibrary.Extensions;
using System.Globalization;

namespace ValueReaderService.Services;
public class ConfigModel(IConfiguration configuration)
{
    private static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;

    public decimal? WeatherForecastLatitude() => GetDecimal("WeatherForecastLatitude");
    public decimal? WeatherForecastLongitude() => GetDecimal("WeatherForecastLongitude");
    public decimal? WeatherForecastAltitude() => GetDecimal("WeatherForecastAltitude");
    public string? WeatherForecastContactEmail() => configuration["WeatherForecastContactEmail"];

    private decimal? GetDecimal(string key)
    {
        var value = configuration[key];
        if (value.IsNullOrEmpty() || !decimal.TryParse(value, InvariantCulture, out var d))
            return null;

        return d;
    }
}
