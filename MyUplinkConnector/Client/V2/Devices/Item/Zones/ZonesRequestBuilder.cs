// <auto-generated/>
using Microsoft.Kiota.Abstractions.Extensions;
using Microsoft.Kiota.Abstractions;
using MyUplinkConnector.Client.V2.Devices.Item.Zones.Item;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System;
namespace MyUplinkConnector.Client.V2.Devices.Item.Zones
{
    /// <summary>
    /// Builds and executes requests for operations under \v2\devices\{deviceId}\zones
    /// </summary>
    [global::System.CodeDom.Compiler.GeneratedCode("Kiota", "1.16.0")]
    public partial class ZonesRequestBuilder : BaseRequestBuilder
    {
        /// <summary>Gets an item from the MyUplinkConnector.Client.v2.devices.item.zones.item collection</summary>
        /// <param name="position">Zone ID.</param>
        /// <returns>A <see cref="global::MyUplinkConnector.Client.V2.Devices.Item.Zones.Item.WithZoneItemRequestBuilder"/></returns>
        public global::MyUplinkConnector.Client.V2.Devices.Item.Zones.Item.WithZoneItemRequestBuilder this[int position]
        {
            get
            {
                var urlTplParams = new Dictionary<string, object>(PathParameters);
                urlTplParams.Add("zoneId", position);
                return new global::MyUplinkConnector.Client.V2.Devices.Item.Zones.Item.WithZoneItemRequestBuilder(urlTplParams, RequestAdapter);
            }
        }
        /// <summary>Gets an item from the MyUplinkConnector.Client.v2.devices.item.zones.item collection</summary>
        /// <param name="position">Zone ID.</param>
        /// <returns>A <see cref="global::MyUplinkConnector.Client.V2.Devices.Item.Zones.Item.WithZoneItemRequestBuilder"/></returns>
        [Obsolete("This indexer is deprecated and will be removed in the next major version. Use the one with the typed parameter instead.")]
        public global::MyUplinkConnector.Client.V2.Devices.Item.Zones.Item.WithZoneItemRequestBuilder this[string position]
        {
            get
            {
                var urlTplParams = new Dictionary<string, object>(PathParameters);
                if (!string.IsNullOrWhiteSpace(position)) urlTplParams.Add("zoneId", position);
                return new global::MyUplinkConnector.Client.V2.Devices.Item.Zones.Item.WithZoneItemRequestBuilder(urlTplParams, RequestAdapter);
            }
        }
        /// <summary>
        /// Instantiates a new <see cref="global::MyUplinkConnector.Client.V2.Devices.Item.Zones.ZonesRequestBuilder"/> and sets the default values.
        /// </summary>
        /// <param name="pathParameters">Path parameters for the request</param>
        /// <param name="requestAdapter">The request adapter to use to execute the requests.</param>
        public ZonesRequestBuilder(Dictionary<string, object> pathParameters, IRequestAdapter requestAdapter) : base(requestAdapter, "{+baseurl}/v2/devices/{deviceId}/zones", pathParameters)
        {
        }
        /// <summary>
        /// Instantiates a new <see cref="global::MyUplinkConnector.Client.V2.Devices.Item.Zones.ZonesRequestBuilder"/> and sets the default values.
        /// </summary>
        /// <param name="rawUrl">The raw URL to use for the request builder.</param>
        /// <param name="requestAdapter">The request adapter to use to execute the requests.</param>
        public ZonesRequestBuilder(string rawUrl, IRequestAdapter requestAdapter) : base(requestAdapter, "{+baseurl}/v2/devices/{deviceId}/zones", rawUrl)
        {
        }
    }
}