using Domain;

namespace ValueReaderService.Services;

public abstract class DeviceReader(ILogger<DeviceReader> logger)
{
    public Task<IList<PointValue>?> ExecuteAsync(Device device, DateTime timestamp)
    {
        try
        {
            return ExecuteAsyncInternal(device, timestamp);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Device reader execution has failed");
            return Task.FromResult<IList<PointValue>?>(null);
        }
    }

    protected abstract Task<IList<PointValue>?> ExecuteAsyncInternal(Device device, DateTime timestamp);
}
