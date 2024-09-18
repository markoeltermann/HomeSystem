using com.clusterrr.TuyaNet;
using Domain;
using System.Text.Json;

namespace ValueReaderService.Services.ChineseRoomController;

public class ChineseRoomControllerReader(ILogger<DeviceReader> logger)
    : DeviceReader(logger)
{
    protected override async Task<IList<PointValue>?> ExecuteAsyncInternal(Device device, DateTime timestamp, ICollection<DevicePoint> devicePoints)
    {
        if (device.Address is null)
            return null;

        var address = JsonSerializer.Deserialize<DeviceAddress>(device.Address);
        if (address is null)
            return null;

        Dictionary<int, object> dps;

        using (var tuyaDevice = new TuyaDevice(address.IP, address.LocalKey, address.DeviceId, TuyaProtocolVersion.V33, address.Port!.Value, 500))
        {
            dps = await tuyaDevice.GetDpsAsync();
        }

        var result = new List<PointValue>(devicePoints.Count);
        foreach (var devicePoint in devicePoints)
        {
            var addressInt = int.Parse(devicePoint.Address);
            var rawValue = dps[addressInt];
            string? value = null;
            switch (addressInt)
            {
                case 2: // temp. setpoint
                case 3: // temperature
                case 102: // floor temperature
                    value = (((long)rawValue) / 2.0).ToString("0.0");
                    break;
                default:
                    break;
            }

            if (value != null)
            {
                result.Add(new(devicePoint, value));
            }
        }

        return result;
    }
}