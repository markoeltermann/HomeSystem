using com.clusterrr.TuyaNet;
using Domain;
using SharedServices;
using System.Text.Json;

namespace ValueReaderService.Services.ChineseRoomController
{
    public class ChineseRoomControllerReader : DeviceReader
    {
        private readonly PointValueStore pointValueStore;

        public ChineseRoomControllerReader(HomeSystemContext dbContext, ILogger<DeviceReader> logger, PointValueStore pointValueStore) : base(dbContext, logger)
        {
            this.pointValueStore = pointValueStore;
        }

        protected override async Task<bool> ExecuteAsyncInternal(Device device, DateTime timestamp)
        {
            if (device.Address is null)
                return false;

            var address = JsonSerializer.Deserialize<DeviceAddress>(device.Address);
            if (address is null)
                return false;

            Dictionary<int, object> dps;

            using (var tuyaDevice = new TuyaDevice(address.IP, address.LocalKey, address.DeviceId, TuyaProtocolVersion.V33, address.Port!.Value, 500))
            {
                dps = await tuyaDevice.GetDpsAsync();
            }

            foreach (var devicePoint in device.DevicePoints)
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
                    pointValueStore.StoreValue(device.Id, devicePoint.Id, timestamp, value);
            }

            return true;
        }
    }
}