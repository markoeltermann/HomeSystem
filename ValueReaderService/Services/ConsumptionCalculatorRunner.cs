using Domain;
using Microsoft.EntityFrameworkCore;

namespace ValueReaderService.Services;

public class ConsumptionCalculatorRunner(ILogger<DeviceReader> logger, HomeSystemContext dbContext, PointValueStoreAdapter pointValueStoreAdapter) : DeviceReader(logger)
{
    private const double MinSolarElevation = -1.0;

    public override bool StorePointsWithReplace => true;

    protected async override Task<IList<PointValue>?> ExecuteAsyncInternal(Device device, DateTime timestamp, ICollection<DevicePoint> devicePoints)
    {
#if !DEBUG
        await Task.Delay(TimeSpan.FromSeconds(10));
#endif

        var inverterDevice = await dbContext.Devices.Include(x => x.DevicePoints).FirstOrDefaultAsync(x => x.Type == "deye_inverter")
            ?? throw new InvalidOperationException("Inverter device not found");

        var solarModelDevice = await dbContext.Devices.Include(x => x.DevicePoints).FirstOrDefaultAsync(x => x.Type == "solar_model")
            ?? throw new InvalidOperationException("Solar model device not found");

        var totalPvEnergyPoint = inverterDevice.DevicePoints.FirstOrDefault(x => x.Type == "total-pv-energy")
            ?? throw new InvalidOperationException("Point 'total-pv-energy' not found on inverter device");

        var solarElevationPoint = solarModelDevice.DevicePoints.FirstOrDefault(x => x.Type == "solar-elevation")
            ?? throw new InvalidOperationException("Point 'solar-elevation' not found on solar model device");

        var dayPvEnergyPoint = devicePoints.FirstOrDefault(x => x.Type == "day-pv-energy");
        if (dayPvEnergyPoint == null)
        {
            return null;
        }

        var timestampLocal = timestamp.ToLocalTime();
        var date = DateOnly.FromDateTime(timestampLocal);

        var totalPvEnergyValues = (await pointValueStoreAdapter.Get(totalPvEnergyPoint.Id, date, fiveMinResolution: true)).Values!;
        var elevationValues = (await pointValueStoreAdapter.Get(solarElevationPoint.Id, date, fiveMinResolution: true)).Values!;

        var elevationLookup = elevationValues.ToDictionary(v => v.Timestamp, v => v.Value);

        var anchorPoints = new List<int>();
        for (int i = 0; i < totalPvEnergyValues.Count; i++)
        {
            if (totalPvEnergyValues[i].Timestamp > timestampLocal) break;

            var value = totalPvEnergyValues[i].Value;
            var hasElevation = elevationLookup.TryGetValue(totalPvEnergyValues[i].Timestamp, out var elevation);

            if (value.HasValue && hasElevation && elevation >= MinSolarElevation)
            {
                if (anchorPoints.Count == 0 || value.Value != totalPvEnergyValues[anchorPoints[^1]].Value!.Value)
                {
                    anchorPoints.Add(i);
                }
            }
        }

        var result = new List<PointValue>();

        if (anchorPoints.Count == 0)
        {
            for (int k = 0; k < totalPvEnergyValues.Count; k++)
            {
                if (totalPvEnergyValues[k].Timestamp > timestampLocal)
                    break;

                result.Add(new PointValue(dayPvEnergyPoint, "0.00", totalPvEnergyValues[k].Timestamp.UtcDateTime));
            }
        }
        else
        {
            var firstValValue = totalPvEnergyValues[anchorPoints[0]].Value!.Value;

            int firstAnchorIdx = anchorPoints[0];
            for (int k = 0; k < firstAnchorIdx; k++)
            {
                result.Add(new PointValue(dayPvEnergyPoint, "0.00", totalPvEnergyValues[k].Timestamp.UtcDateTime));
            }

            for (int i = 0; i < anchorPoints.Count - 1; i++)
            {
                int startIdx = anchorPoints[i];
                int endIdx = anchorPoints[i + 1];

                var startPoint = totalPvEnergyValues[startIdx];
                var endPoint = totalPvEnergyValues[endIdx];

                double startVal = startPoint.Value!.Value - firstValValue;
                double endVal = endPoint.Value!.Value - firstValValue;

                for (int k = startIdx; k < endIdx; k++)
                {
                    var currentPoint = totalPvEnergyValues[k];
                    double interpVal;
                    if (startPoint.Timestamp == endPoint.Timestamp)
                    {
                        interpVal = startVal;
                    }
                    else
                    {
                        interpVal = startVal + (endVal - startVal) * (currentPoint.Timestamp - startPoint.Timestamp).TotalSeconds / (endPoint.Timestamp - startPoint.Timestamp).TotalSeconds;
                    }
                    result.Add(new PointValue(dayPvEnergyPoint, interpVal.ToString("0.00", InvariantCulture), currentPoint.Timestamp.UtcDateTime));
                }
            }

            int lastAnchorIdx = anchorPoints[^1];
            double lastVal = totalPvEnergyValues[lastAnchorIdx].Value!.Value - firstValValue;
            for (int k = lastAnchorIdx; k < totalPvEnergyValues.Count; k++)
            {
                if (totalPvEnergyValues[k].Timestamp > timestampLocal)
                    break;

                result.Add(new PointValue(dayPvEnergyPoint, lastVal.ToString("0.00", InvariantCulture), totalPvEnergyValues[k].Timestamp.UtcDateTime));
            }
        }

        return result;
    }
}
