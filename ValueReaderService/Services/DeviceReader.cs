using Domain;

namespace ValueReaderService.Services;

public abstract class DeviceReader(ILogger<DeviceReader> logger)
{
    public async Task<IList<PointValue>?> ExecuteAsync(Device device, DateTime timestamp, ICollection<DevicePoint> devicePoints)
    {
        try
        {
            return await ExecuteAsyncInternal(device, timestamp, devicePoints);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Device reader execution has failed");
            return null;
        }
    }

    protected abstract Task<IList<PointValue>?> ExecuteAsyncInternal(Device device, DateTime timestamp, ICollection<DevicePoint> devicePoints);
}
