using Microsoft.Extensions.Logging;
using System.Globalization;

namespace SharedServices;

public class PointValueStore(ILogger<PointValueStore> logger)
{
    private const string StoreLocation = @"C:\HomeSystem\ValueStore\";

    //public PointValueStore()
    //{
    //    Directory.CreateDirectory(StoreLocation);
    //}

    public void StoreValue(int deviceId, int devicePointId, DateTime timestamp, string value)
    {
        var retryCount = 0;
        while (retryCount < 5)
        {
            try
            {
                var (location, path) = GetLocationAndPath(deviceId, devicePointId, timestamp, false);
                Directory.CreateDirectory(location);
                using var sw = new StreamWriter(path, true);
                sw.WriteLine($"{timestamp:ddTHH:mm}\t{value}");
                return;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Storing value failed. Retry {Retry}", retryCount);
                retryCount++;
            }
        }
    }

    public void StoreFrequentValue(int deviceId, int devicePointId, DateTime timestamp, string value)
    {
        var now = DateTime.UtcNow;

        var retryCount = 0;
        while (retryCount < 5)
        {
            try
            {
                var (location, path) = GetLocationAndPath(deviceId, devicePointId, timestamp, true);
                Directory.CreateDirectory(location);

                var existingFrequentFiles = Directory.GetFiles(location, "frequent*.txt");
                foreach (var frequentFile in existingFrequentFiles)
                {
                    var fileName = Path.GetFileNameWithoutExtension(frequentFile).Replace("frequent_", "");
                    if (DateTime.TryParseExact(
                        fileName,
                        "yyyy-MM-ddTHH",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                        out var fileTimestamp))
                    {
                        if (now - fileTimestamp > TimeSpan.FromDays(1))
                        {
                            File.Delete(frequentFile);
                        }
                    }
                }

                using var sw = new StreamWriter(path, true);
                sw.WriteLine($"{timestamp:mm:ss}\t{value}");
                return;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Storing frequent value failed. Retry {Retry}", retryCount);
                retryCount++;
            }
        }
    }

    private static (string location, string path) GetLocationAndPath(int deviceId, int devicePointId, DateTime date, bool frequent)
    {
        var location = Path.Combine(StoreLocation, deviceId.ToString(), devicePointId.ToString());
        var format = frequent ? "yyyy-MM-ddTHH" : "yyyy-MM";
        var fileName = date.ToString(format) + ".txt";
        if (frequent)
        {
            fileName = "frequent_" + fileName;
        }
        var path = Path.Combine(location, fileName);
        return (location, path);
    }

    public List<(DateTime, double?)> ReadNumericValues(int deviceId, int devicePointId, DateOnly from, DateOnly upTo)
    {
        var result = new SortedDictionary<DateTime, double?>();

        var fromD = from.ToDateTime(new TimeOnly()).ToUniversalTime();
        var upToD = upTo.ToDateTime(new TimeOnly()).AddDays(1).ToUniversalTime();
        var upToDLocal = upToD.ToLocalTime();

        var t = fromD.ToLocalTime();
        while (t <= upToDLocal)
        {
            result[t] = null;
            t = t.AddMinutes(10);
        }

        var d = new DateTime(fromD.Year, fromD.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        while (d <= upToD)
        {
            var (_, path) = GetLocationAndPath(deviceId, devicePointId, d, false);
            if (File.Exists(path))
            {
                var fileContents = File.ReadAllLines(path);
                foreach (var line in fileContents)
                {
                    var rawTokens = line.Split('\t');
                    if (rawTokens.Length == 2)
                    {
                        var rawTimestamp = rawTokens[0];
                        var rawValue = rawTokens[1];
                        var delta = TimeSpan.ParseExact(rawTimestamp, "%d'T'%h':'%m", CultureInfo.InvariantCulture);
                        var timestamp = d.AddDays(-1) + delta;
                        if (timestamp >= fromD && timestamp <= upToD)
                        {
                            var value = double.Parse(rawValue);
                            result[timestamp.ToLocalTime()] = value;
                        }
                    }
                }
            }

            d = d.AddMonths(1);
        }

        return result.Select(x => (x.Key, x.Value)).ToList();
    }
}
