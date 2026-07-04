using Domain;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ValueReaderService.Services;

namespace ValueReaderServiceTests;

public class DeviceReaderTests
{
    [Fact]
    public async Task ExecuteAsync_AutoLoadsDecoratedDevicePointFromDatabase()
    {
        var options = new DbContextOptionsBuilder<HomeSystemContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new HomeSystemContext(options);
        var sourceDevice = new Device
        {
            Name = "Source Device",
            Type = "shelly",
            SubType = "heat-distribution-controller",
            DevicePoints =
            [
                new DevicePoint
                {
                    Id = 1,
                    Name = "Temperature",
                    Address = "temp",
                    Type = "temperature",
                    Device = null!
                }
            ]
        };
        dbContext.Devices.Add(sourceDevice);
        await dbContext.SaveChangesAsync();

        var reader = new TestDeviceReader(dbContext);
        var device = new Device { Name = "Target Device", Type = "shelly", SubType = "heat-distribution-controller" };

        var result = await reader.ExecuteAsync(device, DateTime.UtcNow, Array.Empty<DevicePoint>());

        result.Should().NotBeNull();
        reader.LoadedPoint.Should().NotBeNull();
        reader.LoadedPoint!.Id.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_ThrowsWhenAutoLoadedDevicePointIsMissing()
    {
        var options = new DbContextOptionsBuilder<HomeSystemContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new HomeSystemContext(options);
        var reader = new TestDeviceReader(dbContext);
        var device = new Device { Type = "shelly", SubType = "heat-distribution-controller" };

        var act = () => reader.ExecuteAsync(device, DateTime.UtcNow, Array.Empty<DevicePoint>());

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    private sealed class TestDeviceReader(HomeSystemContext dbContext) : DeviceReader(NullLogger<DeviceReader>.Instance, dbContext)
    {
        [DevicePointAutoLoad("shelly", "heat-distribution-controller", "temperature")]
        public DevicePoint? LoadedPoint { get; set; }

        protected override Task<IList<PointValue>?> ExecuteAsyncInternal(Device device, DateTime timestamp, ICollection<DevicePoint> devicePoints)
        {
            return Task.FromResult<IList<PointValue>?>(new List<PointValue>());
        }
    }
}
