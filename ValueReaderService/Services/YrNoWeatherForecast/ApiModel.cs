using System.Text.Json.Serialization;

namespace ValueReaderService.Services.YrNoWeatherForecast;

public class METJSONForecast
{
    [JsonPropertyName("geometry")]
    public PointGeometry? Geometry { get; set; }
    [JsonPropertyName("properties")]
    public Forecast? Properties { get; set; }
    [JsonPropertyName("type")]
    public string? Type { get; set; }
}

public class PointGeometry
{
    [JsonPropertyName("coordinates")]
    public decimal[]? Coordinates { get; set; }
    [JsonPropertyName("type")]
    public string? Type { get; set; }
}

public class Forecast
{
    [JsonPropertyName("meta")]
    public InlineModel? Meta { get; set; }
    [JsonPropertyName("timeseries")]
    public ForecastTimeStep[]? Timeseries { get; set; }
}

public class InlineModel
{
    [JsonPropertyName("units")]
    public ForecastUnits? Units { get; set; }
    [JsonPropertyName("updated_at")]
    public string? UpdatedAt { get; set; }
}

public class ForecastTimeStep
{
    [JsonPropertyName("data")]
    public InlineModel0? Data { get; set; }
    [JsonPropertyName("time")]
    public DateTime? Time { get; set; }
}

public class ForecastUnits
{
    [JsonPropertyName("air_pressure_at_sea_level")]
    public string? AirPressureAtSeaLevel { get; set; }
    [JsonPropertyName("air_temperature")]
    public string? AirTemperature { get; set; }
    [JsonPropertyName("air_temperature_max")]
    public string? AirTemperatureMax { get; set; }
    [JsonPropertyName("air_temperature_min")]
    public string? AirTemperatureMin { get; set; }
    [JsonPropertyName("cloud_area_fraction")]
    public string? CloudAreaFraction { get; set; }
    [JsonPropertyName("cloud_area_fraction_high")]
    public string? CloudAreaFractionHigh { get; set; }
    [JsonPropertyName("cloud_area_fraction_low")]
    public string? CloudAreaFractionLow { get; set; }
    [JsonPropertyName("cloud_area_fraction_medium")]
    public string? CloudAreaFractionMedium { get; set; }
    [JsonPropertyName("dew_point_temperature")]
    public string? DewPointTemperature { get; set; }
    [JsonPropertyName("fog_area_fraction")]
    public string? FogAreaFraction { get; set; }
    [JsonPropertyName("precipitation_amount")]
    public string? PrecipitationAmount { get; set; }
    [JsonPropertyName("precipitation_amount_max")]
    public string? PrecipitationAmountMax { get; set; }
    [JsonPropertyName("precipitation_amount_min")]
    public string? PrecipitationAmountMin { get; set; }
    [JsonPropertyName("probability_of_precipitation")]
    public string? ProbabilityOfPrecipitation { get; set; }
    [JsonPropertyName("probability_of_thunder")]
    public string? ProbabilityOfThunder { get; set; }
    [JsonPropertyName("relative_humidity")]
    public string? RelativeHumidity { get; set; }
    [JsonPropertyName("ultraviolet_index_clear_sky_max")]
    public string? UltravioletIndexClearSkyMax { get; set; }
    [JsonPropertyName("wind_from_direction")]
    public string? WindFromDirection { get; set; }
    [JsonPropertyName("wind_speed")]
    public string? WindSpeed { get; set; }
    [JsonPropertyName("wind_speed_of_gust")]
    public string? WindSpeedOfGust { get; set; }
}

public class InlineModel0
{
    [JsonPropertyName("instant")]
    public InlineModel1? Instant { get; set; }
    [JsonPropertyName("next_12_hours")]
    public InlineModel2? Next12Hours { get; set; }
    [JsonPropertyName("next_1_hours")]
    public InlineModel3? Next1Hours { get; set; }
    [JsonPropertyName("next_6_hours")]
    public InlineModel4? Next6Hours { get; set; }
}

public class InlineModel1
{
    [JsonPropertyName("details")]
    public ForecastTimeInstant? Details { get; set; }
}

public class InlineModel2
{
    [JsonPropertyName("details")]
    public ForecastTimePeriod? Details { get; set; }
    [JsonPropertyName("summary")]
    public ForecastSummary? Summary { get; set; }
}

public class InlineModel3
{
    [JsonPropertyName("details")]
    public ForecastTimePeriod? Details { get; set; }
    [JsonPropertyName("summary")]
    public ForecastSummary? Summary { get; set; }
}

public class InlineModel4
{
    [JsonPropertyName("details")]
    public ForecastTimePeriod? Details { get; set; }
    [JsonPropertyName("summary")]
    public ForecastSummary? Summary { get; set; }
}

public class ForecastTimeInstant
{
    [JsonPropertyName("air_pressure_at_sea_level")]
    public decimal? AirPressureAtSeaLevel { get; set; }
    [JsonPropertyName("air_temperature")]
    public decimal? AirTemperature { get; set; }
    [JsonPropertyName("cloud_area_fraction")]
    public decimal? CloudAreaFraction { get; set; }
    [JsonPropertyName("cloud_area_fraction_high")]
    public decimal? CloudAreaFractionHigh { get; set; }
    [JsonPropertyName("cloud_area_fraction_low")]
    public decimal? CloudAreaFractionLow { get; set; }
    [JsonPropertyName("cloud_area_fraction_medium")]
    public decimal? CloudAreaFractionMedium { get; set; }
    [JsonPropertyName("dew_point_temperature")]
    public decimal? DewPointTemperature { get; set; }
    [JsonPropertyName("fog_area_fraction")]
    public decimal? FogAreaFraction { get; set; }
    [JsonPropertyName("relative_humidity")]
    public decimal? RelativeHumidity { get; set; }
    [JsonPropertyName("wind_from_direction")]
    public decimal? WindFromDirection { get; set; }
    [JsonPropertyName("wind_speed")]
    public decimal? WindSpeed { get; set; }
    [JsonPropertyName("wind_speed_of_gust")]
    public decimal? WindSpeedOfGust { get; set; }
}

public class ForecastTimePeriod
{
    [JsonPropertyName("air_temperature_max")]
    public decimal? AirTemperatureMax { get; set; }
    [JsonPropertyName("air_temperature_min")]
    public decimal? AirTemperatureMin { get; set; }
    [JsonPropertyName("precipitation_amount")]
    public decimal? PrecipitationAmount { get; set; }
    [JsonPropertyName("precipitation_amount_max")]
    public decimal? PrecipitationAmountMax { get; set; }
    [JsonPropertyName("precipitation_amount_min")]
    public decimal? PrecipitationAmountMin { get; set; }
    [JsonPropertyName("probability_of_precipitation")]
    public decimal? ProbabilityOfPrecipitation { get; set; }
    [JsonPropertyName("probability_of_thunder")]
    public decimal? ProbabilityOfThunder { get; set; }
    [JsonPropertyName("ultraviolet_index_clear_sky_max")]
    public decimal? UltravioletIndexClearSkyMax { get; set; }
}

public class ForecastSummary
{
    [JsonPropertyName("symbol_code")]
    public string? SymbolCode { get; set; }
}