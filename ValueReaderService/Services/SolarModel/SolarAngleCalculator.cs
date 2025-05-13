using static System.Math;

namespace ValueReaderService.Services.SolarModel;

public class SolarAngleCalculator
{
    /// <summary>
    /// Calculate solar elevation and azimuth angles at a given location and time.
    /// </summary>
    /// <param name="latitude">Latitude in decimal degrees (positive for North, negative for South)</param>
    /// <param name="longitude">Longitude in decimal degrees (positive for East, negative for West)</param>
    /// <param name="utcDateTime">UTC datetime</param>
    /// <returns>Tuple containing (elevation angle, azimuth angle) in degrees</returns>
    public static (double ElevationAngle, double AzimuthAngle) CalculateSolarAngles(
        double latitude, double longitude, DateTime utcDateTime)
    {
        // Convert latitude to radians
        var latRad = DegreesToRadians(latitude);

        // Calculate day of year (DOY)
        var dayOfYear = utcDateTime.DayOfYear;

        // Get the actual number of days in the year (accounting for leap years)
        //var daysInYear = DateTime.IsLeapYear(utcDateTime.Year) ? 366 : 365;
        var daysInYear = 365.25;
        var decimalHour = (utcDateTime - utcDateTime.Date).TotalHours;

        // Calculate the fractional year (gamma) in radians
        var gamma = 2 * PI / daysInYear * (dayOfYear - 1 + (decimalHour - 12) / 24.0);

        // Calculate the equation of time (in minutes)
        var eot = 229.18 * (0.000075 + 0.001868 * Cos(gamma) - 0.032077 * Sin(gamma)
                 - 0.014615 * Cos(2 * gamma) - 0.040849 * Sin(2 * gamma));

        // Calculate the solar declination angle (in radians)
        var decl = 0.006918 - 0.399912 * Cos(gamma) + 0.070257 * Sin(gamma)
                 - 0.006758 * Cos(2 * gamma) + 0.000907 * Sin(2 * gamma)
                 - 0.002697 * Cos(3 * gamma) + 0.00148 * Sin(3 * gamma);

        // Calculate solar hour angle (in radians)
        // Solar noon is at trueSolarTime = 720 (12 hours * 60 minutes)
        var subsolarPointLongitudeDeg = -15 * (decimalHour - 12 + eot / 60);
        var subsolarPointLongitude = DegreesToRadians(subsolarPointLongitudeDeg - longitude);  // 4 minutes per degree

        var sx = Cos(decl) * Sin(subsolarPointLongitude);
        var sy = Cos(latRad) * Sin(decl) - Sin(latRad) * Cos(decl) * Cos(subsolarPointLongitude);
        var sz = Sin(latRad) * Sin(decl) + Cos(latRad) * Cos(decl) * Cos(subsolarPointLongitude);

        var elevationRad = -Acos(sz) + PI / 2;
        var azimuthRad = Atan2(-sx, -sy) + PI;

        // Convert to degrees
        var elevationDeg = RadiansToDegrees(elevationRad);
        var azimuthDeg = RadiansToDegrees(azimuthRad);

        return (elevationDeg, azimuthDeg);
    }

    private static double DegreesToRadians(double degrees)
    {
        return degrees * PI / 180.0;
    }

    private static double RadiansToDegrees(double radians)
    {
        return radians * 180.0 / PI;
    }
}
