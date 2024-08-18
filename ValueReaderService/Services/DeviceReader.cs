using Domain;

namespace ValueReaderService.Services;

public abstract class DeviceReader(HomeSystemContext dbContext, ILogger<DeviceReader> logger)
{
    public async Task ExecuteAsync(Device device, DateTime timestamp)
    {
        Job? job = null;
        try
        {
            job = new Job
            {
                Name = GetType().Name,
                StartTime = DateTime.UtcNow,
                Status = JobStatus.Running
            };
            dbContext.Jobs.Add(job);
            await dbContext.SaveChangesAsync();

            var isSuccess = await ExecuteAsyncInternal(device, timestamp);

            job.Status = isSuccess ? JobStatus.Completed : JobStatus.Failed;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Device reader execution has failed");
            if (job != null)
                job.Status = JobStatus.Failed;
        }
        finally
        {
            if (job != null)
            {
                try
                {
                    await dbContext.SaveChangesAsync();
                }
                catch (Exception)
                {
                }
            }
        }
    }

    protected abstract Task<bool> ExecuteAsyncInternal(Device device, DateTime timestamp);
}
