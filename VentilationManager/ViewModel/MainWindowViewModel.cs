using System;
using System.Collections.Generic;
using System.IO.BACnet;
using System.IO.BACnet.Serialize;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VentilationManager.ViewModel
{
    public class MainWindowViewModel
    {
        private BacnetClient bacnetClient;

        public async Task Start()
        {
            bacnetClient = new BacnetClient(new BacnetIpUdpProtocolTransport(0xBAC0));


            bacnetClient.Start();    // go

            // Send WhoIs in order to get back all the Iam responses :  
            //bacnetClient.OnIam += BacnetClient_OnIam;

            //bacnetClient.RegisterAsForeignDevice("192.168.1.179", 60);

            //bacnetClient.RemoteWhoIs("192.168.1.179");



            //bacnetClient.WhoIs();

            var address = new BacnetAddress(BacnetAddressTypes.IP, "192.168.1.179");

            var deviceObjId = new BacnetObjectId(BacnetObjectTypes.OBJECT_DEVICE, 60);
            var analogValueId = new BacnetObjectId(BacnetObjectTypes.OBJECT_ANALOG_VALUE, 0);
            var objectIdList = await bacnetClient.ReadPropertyAsync(address, deviceObjId, BacnetPropertyIds.PROP_OBJECT_LIST);

            //var propertyReferences = new List<BacnetPropertyReference>
            //{
            //    new BacnetPropertyReference((uint)BacnetPropertyIds.PROP_ALL, ASN1.BACNET_ARRAY_ALL)
            //};

            //bacnetClient.ReadPropertyMultipleRequest(address, analogValueId, propertyReferences, out var values);

            foreach (var objectId in objectIdList.Select(oid => (BacnetObjectId)oid.Value))
            {
                ////bacnetClient.ReadPropertyAsync(address, deviceObjId, BacnetPropertyIds.PROP_ALL);
                //var propertyReferences = new List<BacnetPropertyReference>
                //{
                //    new BacnetPropertyReference((uint)BacnetPropertyIds.PROP_PRESENT_VALUE, ASN1.BACNET_ARRAY_ALL)
                //};
                //////bacnetClient.read

                //bacnetClient.ReadPropertyMultipleRequest(address, analogValueId, propertyReferences, out var values);
                var name = await bacnetClient.ReadPropertyAsync(address, objectId, BacnetPropertyIds.PROP_OBJECT_NAME);
            }

        }

        private void BacnetClient_OnIam(BacnetClient sender, BacnetAddress adr, uint deviceId, uint maxAPDU, BacnetSegmentations segmentation, ushort vendorId)
        {

        }
    }
}
