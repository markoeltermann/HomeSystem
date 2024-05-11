using Domain;
using SharedServices;
using System.IO.BACnet;
using System.IO.BACnet.Serialize;
using System.Net;

namespace ValueReaderService.Services
{
    public class BacnetDeviceReader : DeviceReader
    {
        private readonly PointValueStore pointValueStore;

        public BacnetDeviceReader(HomeSystemContext dbContext, ILogger<BacnetDeviceReader> logger, PointValueStore pointValueStore) : base(dbContext, logger)
        {
            this.pointValueStore = pointValueStore;
        }

        protected override Task<bool> ExecuteAsyncInternal(Device device, DateTime timestamp)
        {
            var address = CreateAddress();
            if (address == null)
                return Task.FromResult(false);

            using var bacnetClient = new BacnetClient(new BacnetIpUdpProtocolTransport(0xBAC0), 10000);
            bacnetClient.Start();

            var points = device.DevicePoints.ToArray();

            var readCommands = points.Select(p =>
            {
                var objectId = BacnetObjectId.Parse(p.Address);

                return new BacnetReadAccessSpecification
                {
                    objectIdentifier = objectId,
                    propertyReferences = new[] { new BacnetPropertyReference((uint)BacnetPropertyIds.PROP_PRESENT_VALUE, ASN1.BACNET_ARRAY_ALL) }
                };
            }).ToList();

            bacnetClient.ReadPropertyMultipleRequest(address, readCommands, out var values);

            if (values == null)
            {
                logger.LogError("Bacnet property read failed.");
                return Task.FromResult(false);
            }

            for (int i = 0; i < points.Length; i++)
            {
                var point = points[i];
                var bacnetValue = values[i];
                if (bacnetValue.values.Count > 0 && bacnetValue.values[0].value.Count > 0)
                {
                    var value = bacnetValue.values[0].value[0].Value.ToString() ?? "";
                    if (point.DataType.Name == "Enum")
                    {
                        if (int.TryParse(value, out var valueInt) && valueInt > 0)
                        {
                            var enumValue = point.EnumMembers.FirstOrDefault(em => em.Value == valueInt);
                            if (enumValue?.Name != null)
                                value = enumValue.Name;
                        }
                    }
                    pointValueStore.StoreValue(device.Id, point.Id, timestamp, value);
                }
            }

            return Task.FromResult(true);
        }

        private BacnetAddress? CreateAddress()
        {
            IPHostEntry hostEntry;

            hostEntry = Dns.GetHostEntry("tew-752dru");

            IPAddress? ipAddress;

            if (hostEntry.AddressList.Length > 0)
                ipAddress = hostEntry.AddressList[0];
            else
            {
                logger.LogError("Could not resolve Bacnet unit IP address.");
                return null;
            }

            return new BacnetAddress(BacnetAddressTypes.IP, ipAddress.ToString());
        }
    }
}