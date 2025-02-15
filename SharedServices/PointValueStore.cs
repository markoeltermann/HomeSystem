using CommonLibrary.Extensions;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace SharedServices;

public class PointValueStore(ILogger<PointValueStore> logger)
{
    private const string StoreLocation = @"C:\HomeSystem\ValueStore\";

    private readonly object SyncRoot = new();

    public void StoreValue(int deviceId, int devicePointId, DateTime timestamp, string value)
    {
        var retryCount = 0;
        while (retryCount < 5)
        {
            try
            {
                lock (SyncRoot)
                {
                    var (location, path) = GetLocationAndPath(deviceId, devicePointId, timestamp, false);
                    Directory.CreateDirectory(location);
                    using var sw = new StreamWriter(path, true);
                    sw.WriteLine($"{timestamp:ddTHH:mm}\t{value}");
                }
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

    public void StoreValuesWithReplace(int deviceId, int devicePointId, (DateTime timestamp, string? value)[] values)
    {
        if (values == null || values.Length == 0)
            return;

        var retryCount = 0;
        while (retryCount < 5)
        {
            try
            {
                lock (SyncRoot)
                {
                    var valuesAndLocations = values.Select(x =>
                    {
                        var (location, path) = GetLocationAndPath(deviceId, devicePointId, x.timestamp, false);
                        return (location, path, x.timestamp, x.value);
                    }).ToArray();

                    var location = valuesAndLocations[0].location;
                    Directory.CreateDirectory(location);

                    foreach (var group in valuesAndLocations.GroupBy(x => x.path))
                    {
                        Dictionary<string, string> lines;
                        if (File.Exists(group.Key))
                        {
                            lines = File.ReadAllLines(group.Key).Where(x => !x.IsNullOrEmpty()).Select(x => x.Split('\t')).ToDictionary(x => x[0], x => x[1]);
                        }
                        else
                        {
                            lines = [];
                        }

                        foreach (var (_, _, timestamp, value) in group)
                        {
                            if (value != null)
                                lines[$"{timestamp:ddTHH:mm}"] = value;
                            else
                                lines.Remove($"{timestamp:ddTHH:mm}");
                        }

                        using var sw = new StreamWriter(group.Key, new FileStreamOptions { Mode = FileMode.Create, Access = FileAccess.ReadWrite });
                        foreach (var line in lines.OrderBy(x => x.Key).Select(x => $"{x.Key}\t{x.Value}"))
                        {
                            sw.WriteLine(line);
                        }
                    }

                }
                return;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Storing value failed. Retry {Retry}", retryCount);
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


        lock (SyncRoot)
        {
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
                                if (double.TryParse(rawValue, out var value))
                                {
                                    result[timestamp.ToLocalTime()] = value;
                                }
                                else if (bool.TryParse(rawValue, out var b))
                                {
                                    result[timestamp.ToLocalTime()] = b ? 1 : 0;
                                }
                            }
                        }
                    }
                }

                d = d.AddMonths(1);
            }
        }

        return result.Select(x => (x.Key, x.Value)).ToList();
    }
}
