using System.Globalization;

namespace SharedServices
{
    public class PointValueStore
    {
        private const string StoreLocation = @"C:\HomeSystem\ValueStore\";

        //public PointValueStore()
        //{
        //    Directory.CreateDirectory(StoreLocation);
        //}

        public void StoreValue(int deviceId, int devicePointId, DateTime timestamp, string value)
        {
            var (location, path) = GetLocationAndPath(deviceId, devicePointId, timestamp);
            Directory.CreateDirectory(location);
            //using var file = File.Open(location + timestamp.ToString("yyyy-MM") + ".txt", FileMode.OpenOrCreate);
            using var sw = new StreamWriter(path, true);
            sw.WriteLine($"{timestamp:ddTHH:mm}\t{value}");
        }

        private static (string location, string path) GetLocationAndPath(int deviceId, int devicePointId, DateTime date)
        {
            var location = StoreLocation + deviceId + "\\" + devicePointId + "\\";
            var path = location + date.ToString("yyyy-MM") + ".txt";
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
                var (_, path) = GetLocationAndPath(deviceId, devicePointId, d);
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
}
