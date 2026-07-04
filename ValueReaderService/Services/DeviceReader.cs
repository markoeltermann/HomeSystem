using Domain;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Reflection;

namespace ValueReaderService.Services;

public abstract class DeviceReader(ILogger<DeviceReader> logger, HomeSystemContext dbContext)
{
    protected static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;

    protected ILogger Logger => logger;

    protected HomeSystemContext DbContext { get; } = dbContext;

    public async Task<IList<PointValue>?> ExecuteAsync(Device device, DateTime timestamp, ICollection<DevicePoint> devicePoints)
    {
        try
        {
            await AutoLoadDevicePointsAsync();
            return await ExecuteAsyncInternal(device, timestamp, devicePoints);
        }
        catch (MissingConfigKeyException mex)
        {
            logger.LogError(mex, "Config key {ConfigKey} is missing from appSettings or its value is invalid", mex.ConfigKey);
            return null;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Device reader execution has failed");
            return null;
        }
    }

    protected abstract Task<IList<PointValue>?> ExecuteAsyncInternal(Device device, DateTime timestamp, ICollection<DevicePoint> devicePoints);

    public virtual bool StorePointsWithReplace => false;

    private async Task AutoLoadDevicePointsAsync()
    {
        var autoLoadMembers = GetAutoLoadMembers().ToArray();
        if (autoLoadMembers.Length == 0)
        {
            return;
        }

        var descriptors = autoLoadMembers
            .Select(member => (member, attribute: member.GetCustomAttribute<DevicePointAutoLoadAttribute>()!))
            .ToArray();

        var deviceTypes = descriptors.Select(x => x.attribute.DeviceType).Distinct().ToArray();

        var matchingDevices = await DbContext.Devices
            .AsNoTrackingWithIdentityResolution()
            .Where(x => deviceTypes.Contains(x.Type))
            .Include(x => x.DevicePoints)
                .ThenInclude(x => x.DataType)
            .Include(x => x.DevicePoints)
                .ThenInclude(x => x.Unit)
            .AsSplitQuery()
            .ToListAsync();

        foreach (var (member, attribute) in descriptors)
        {
            var targetDevice = matchingDevices.FirstOrDefault(x =>
                x.Type == attribute.DeviceType
                && (attribute.DeviceSubType is null || x.SubType == attribute.DeviceSubType))
                ?? throw new InvalidOperationException($"Unable to auto-load device '{attribute.DeviceType}' ({attribute.DeviceSubType}) from the database.");

            var matchingPoint = targetDevice.DevicePoints.FirstOrDefault(x => x.Type == attribute.DevicePointType)
                ?? throw new InvalidOperationException($"Unable to auto-load device point '{attribute.DevicePointType}' for device '{attribute.DeviceType}' ({attribute.DeviceSubType}).");

            AssignAutoLoadedPoint(member, matchingPoint);
        }
    }

    private void AssignAutoLoadedPoint(MemberInfo member, DevicePoint matchingPoint)
    {
        switch (member)
        {
            case PropertyInfo propertyInfo:
                if (!propertyInfo.CanWrite)
                {
                    throw new InvalidOperationException($"Property '{propertyInfo.Name}' must be writable for auto-loading.");
                }

                propertyInfo.SetValue(this, matchingPoint);
                break;
            case FieldInfo fieldInfo:
                fieldInfo.SetValue(this, matchingPoint);
                break;
            default:
                throw new InvalidOperationException($"Unsupported auto-load member '{member.Name}'.");
        }
    }

    private IEnumerable<MemberInfo> GetAutoLoadMembers()
    {
        return GetType()
            .GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy)
            .Where(x => x.MemberType is MemberTypes.Property or MemberTypes.Field)
            .Where(x => x.GetCustomAttribute<DevicePointAutoLoadAttribute>() is not null);
    }
}
