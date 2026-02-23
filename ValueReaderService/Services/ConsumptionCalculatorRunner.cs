using Domain;
using Microsoft.EntityFrameworkCore;

namespace ValueReaderService.Services;

public class ConsumptionCalculatorRunner(ILogger<DeviceReader> logger, HomeSystemContext dbContext, PointValueStoreAdapter pointValueStoreAdapter) : DeviceReader(logger)
{
    public override bool StorePointsWithReplace => true;

    protected async override Task<IList<PointValue>?> ExecuteAsyncInternal(Device device, DateTime timestamp, ICollection<DevicePoint> devicePoints)
    {
#if !DEBUG
        await Task.Delay(TimeSpan.FromSeconds(10));
#endif

        var inverterDevice = await dbContext.Devices.Include(x => x.DevicePoints).FirstOrDefaultAsync(x => x.Type == "deye_inverter")
            ?? throw new InvalidOperationException("Inverter device not found");

        var totalPvEnergyPoint = inverterDevice.DevicePoints.FirstOrDefault(x => x.Type == "total-pv-energy")
            ?? throw new InvalidOperationException("Point 'total-pv-energy' not found on inverter device");

        var dayPvEnergyPoint = devicePoints.FirstOrDefault(x => x.Type == "day-pv-energy");
        if (dayPvEnergyPoint == null)
        {
            return null;
        }

        var timestampLocal = timestamp.ToLocalTime();
        var date = DateOnly.FromDateTime(timestampLocal);

        var response = await pointValueStoreAdapter.Get(totalPvEnergyPoint.Id, date, fiveMinResolution: true);
        if (response?.Values == null || response.Values.Count == 0)
        {
            return null;
        }

        var values = response.Values;
        double? firstValValue = values.FirstOrDefault(v => v.Value.HasValue)?.Value;

        if (firstValValue == null)
        {
            return null;
        }

        var anchorPoints = new List<int>();
        for (int i = 0; i < values.Count; i++)
        {
            if (values[i].Timestamp > timestampLocal) break;

            var value = values[i].Value;

            if (value.HasValue)
            {
                if (anchorPoints.Count == 0 || value.Value != values[anchorPoints[^1]].Value!.Value)
                {
                    anchorPoints.Add(i);
                }
            }
        }

        var result = new List<PointValue>();

        if (anchorPoints.Count == 0)
        {
            for (int k = 0; k < values.Count; k++)
            {
                if (values[k].Timestamp > timestampLocal)
                    break;

                result.Add(new PointValue(dayPvEnergyPoint, "0.00", values[k].Timestamp.UtcDateTime));
            }
        }
        else
        {

            int firstAnchorIdx = anchorPoints[0];
            for (int k = 0; k < firstAnchorIdx; k++)
            {
                result.Add(new PointValue(dayPvEnergyPoint, "0.00", values[k].Timestamp.UtcDateTime));
            }

            for (int i = 0; i < anchorPoints.Count - 1; i++)
            {
                int startIdx = anchorPoints[i];
                int endIdx = anchorPoints[i + 1];

                var startPoint = values[startIdx];
                var endPoint = values[endIdx];

                double startVal = startPoint.Value!.Value - firstValValue.Value;
                double endVal = endPoint.Value!.Value - firstValValue.Value;

                for (int k = startIdx; k < endIdx; k++)
                {
                    var currentPoint = values[k];
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
            double lastVal = values[lastAnchorIdx].Value!.Value - firstValValue.Value;
            for (int k = lastAnchorIdx; k < values.Count; k++)
            {
                if (values[k].Timestamp > timestampLocal)
                    break;

                result.Add(new PointValue(dayPvEnergyPoint, lastVal.ToString("0.00", InvariantCulture), values[k].Timestamp.UtcDateTime));
            }
        }

        return result;
    }
}
