using System.IO.BACnet;
using System.IO.BACnet.Serialize;
using System.Net;

namespace BacnetConnector;

public class BacnetService(ILogger<BacnetService> logger, IConfiguration configuration)
{
    private readonly object syncRoot = new();
    private BacnetClient? bacnetClient;
    private DateTime bacnetClientCreationTime;

    public List<PointValueDto>? ReadValues(IList<string> addresses)
    {
        if (addresses == null)
            return null;

        if (!addresses.Any())
            return [];

        lock (syncRoot)
        {
            var readCommands = addresses.Select(p =>
            {
                var objectId = BacnetObjectId.Parse(p);

                return new BacnetReadAccessSpecification
                {
                    objectIdentifier = objectId,
                    propertyReferences = [new BacnetPropertyReference((uint)BacnetPropertyIds.PROP_PRESENT_VALUE, ASN1.BACNET_ARRAY_ALL)]
                };
            }).ToList();

            var bacnetAddress = CreateAddress();
            if (bacnetAddress == null)
                return null;

            IList<BacnetReadAccessResult>? values = null;

            PerformBacnetOperation(c => c.ReadPropertyMultipleRequest(bacnetAddress, readCommands, out values));

            if (values == null)
            {
                logger.LogError("Bacnet property read failed.");
                return null;
            }

            var result = new List<PointValueDto>();

            for (int i = 0; i < addresses.Count; i++)
            {
                var pointAddress = addresses[i];
                var bacnetValue = values[i];
                if (bacnetValue.values.Count > 0 && bacnetValue.values[0].value.Count > 0)
                {
                    var value = bacnetValue.values[0].value[0].Value.ToString() ?? "";
                    result.Add(new PointValueDto(pointAddress, value));
                }
            }

            return result;
        }
    }

    private void PerformBacnetOperation(Action<BacnetClient> action)
    {
        try
        {
            if (bacnetClient == null)
            {
                bacnetClientCreationTime = DateTime.UtcNow;
                bacnetClient = new BacnetClient(new BacnetIpUdpProtocolTransport(0xBAC0), 10000);
                bacnetClient.Start();
            }

            action(bacnetClient);
        }
        catch
        {
            try
            {
                bacnetClient?.Dispose();
            }
            catch { }
            bacnetClient = null;
            throw;
        }
        finally
        {
            var clientLifetime = configuration.GetValue<int>("BacnetClientLifetime");
            if (DateTime.UtcNow - bacnetClientCreationTime > TimeSpan.FromSeconds(clientLifetime))
            {
                try
                {
                    bacnetClient?.Dispose();
                }
                catch { }
                bacnetClient = null;
            }
        }
    }

    public List<PointDto>? ReadAllPoints()
    {
        lock (syncRoot)
        {
            var bacnetAddress = CreateAddress();
            if (bacnetAddress == null)
                return null;

            var deviceObjId = new BacnetObjectId(BacnetObjectTypes.OBJECT_DEVICE, 60);

            List<PointDto>? result = null;
            PerformBacnetOperation(c =>
            {
                c.ReadPropertyRequest(bacnetAddress, deviceObjId, BacnetPropertyIds.PROP_OBJECT_LIST, out var objectList);
                if (objectList == null)
                    return;

                result = new List<PointDto>(objectList.Count);

                foreach (var objectId in objectList.Select(oid => (BacnetObjectId)oid.Value).Skip(1))
                {
                    var propertyReferences = new List<BacnetPropertyReference>
                    {
                        new((uint)BacnetPropertyIds.PROP_OBJECT_NAME, ASN1.BACNET_ARRAY_ALL),
                        new((uint)BacnetPropertyIds.PROP_OBJECT_TYPE, ASN1.BACNET_ARRAY_ALL),
                        new((uint)BacnetPropertyIds.PROP_PRESENT_VALUE, ASN1.BACNET_ARRAY_ALL),
                        new((uint)BacnetPropertyIds.PROP_STATE_TEXT, ASN1.BACNET_ARRAY_ALL),
                    };

                    c.ReadPropertyMultipleRequest(bacnetAddress, objectId, propertyReferences, out var values);

                    var name = values[0].values[0].value[0].Value.ToString();
                    var objType = (uint)values[0].values[1].value[0].Value;
                    var presentValue = values[0].values[2].value[0].Value.ToString() ?? string.Empty;

                    EnumMemberDto[]? enumMemberDtos = null;

                    if (objType == 19)
                    {
                        var enumMembers = values[0].values[3].value;
                        enumMemberDtos = new EnumMemberDto[enumMembers.Count];

                        for (int i = 0; i < enumMembers.Count; i++)
                        {
                            var member = enumMembers[i];
                            enumMemberDtos[i] = new EnumMemberDto
                            {
                                Name = member.Value.ToString() ?? string.Empty,
                                Value = i + 1
                            };
                        }
                    }

                    result.Add(new PointDto
                    {
                        Address = objectId.ToString(),
                        Name = name,
                        PresentValue = presentValue,
                        PossibleValues = enumMemberDtos
                    });
                }
            });

            return result;
        }
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
