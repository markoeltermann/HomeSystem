using Domain;

namespace ValueReaderService.Services.SolarModel;

public class SolarModelRunner(ILogger<DeviceReader> logger, ConfigModel configModel) : DeviceReader(logger)
{
    public override bool StorePointsWithReplace => true;

    protected override Task<IList<PointValue>?> ExecuteAsyncInternal(Device device, DateTime timestamp, ICollection<DevicePoint> devicePoints)
    {
        var azimuth = devicePoints.FirstOrDefault(p => p.Type == "solar-azimuth");
        var elevation = devicePoints.FirstOrDefault(p => p.Type == "solar-elevation");
        if (azimuth == null || elevation == null)
        {
            Logger.LogError("Device points for azimuth or elevation are not found");
            return Task.FromResult<IList<PointValue>?>(null);
        }

        var lat = configModel.WeatherForecastLatitude();
        var lon = configModel.WeatherForecastLongitude();

        var t0 = new DateTime(timestamp.Year, timestamp.Month, timestamp.Day, 0, 0, 0, DateTimeKind.Utc);
        var until = t0.AddDays(5);

        //var t0 = new DateTime(timestamp.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        //var until = t0.AddDays(130);

        var t = t0;
        var result = new List<PointValue>();
        while (t < until)
        {
            var (elevationAngle, azimuthAngle) = SolarAngleCalculator.CalculateSolarAngles((double)lat, (double)lon, t);
            result.Add(new PointValue(elevation, elevationAngle.ToString("0.00", InvariantCulture), t));
            result.Add(new PointValue(azimuth, azimuthAngle.ToString("0.00", InvariantCulture), t));
            t = t.AddMinutes(10);
        }

        return Task.FromResult<IList<PointValue>?>(result);
    }
}
