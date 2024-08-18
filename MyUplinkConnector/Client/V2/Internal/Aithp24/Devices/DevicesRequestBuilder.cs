// <auto-generated/>
using Microsoft.Kiota.Abstractions.Extensions;
using Microsoft.Kiota.Abstractions;
using MyUplinkConnector.Client.V2.Internal.Aithp24.Devices.Item;
using MyUplinkConnector.Client.V2.Internal.Aithp24.Devices.Paged;
using MyUplinkConnector.Client.V2.Internal.Aithp24.Devices.Registered;
using MyUplinkConnector.Client.V2.Internal.Aithp24.Devices.Sync;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System;
namespace MyUplinkConnector.Client.V2.Internal.Aithp24.Devices
{
    /// <summary>
    /// Builds and executes requests for operations under \v2\internal\aithp24\devices
    /// </summary>
    [global::System.CodeDom.Compiler.GeneratedCode("Kiota", "1.16.0")]
    public partial class DevicesRequestBuilder : BaseRequestBuilder
    {
        /// <summary>The paged property</summary>
        public global::MyUplinkConnector.Client.V2.Internal.Aithp24.Devices.Paged.PagedRequestBuilder Paged
        {
            get => new global::MyUplinkConnector.Client.V2.Internal.Aithp24.Devices.Paged.PagedRequestBuilder(PathParameters, RequestAdapter);
        }
        /// <summary>The registered property</summary>
        public global::MyUplinkConnector.Client.V2.Internal.Aithp24.Devices.Registered.RegisteredRequestBuilder Registered
        {
            get => new global::MyUplinkConnector.Client.V2.Internal.Aithp24.Devices.Registered.RegisteredRequestBuilder(PathParameters, RequestAdapter);
        }
        /// <summary>The sync property</summary>
        public global::MyUplinkConnector.Client.V2.Internal.Aithp24.Devices.Sync.SyncRequestBuilder Sync
        {
            get => new global::MyUplinkConnector.Client.V2.Internal.Aithp24.Devices.Sync.SyncRequestBuilder(PathParameters, RequestAdapter);
        }
        /// <summary>Gets an item from the MyUplinkConnector.Client.v2.internal.aithp24.devices.item collection</summary>
        /// <param name="position">Unique identifier of the item</param>
        /// <returns>A <see cref="global::MyUplinkConnector.Client.V2.Internal.Aithp24.Devices.Item.WithDeviceItemRequestBuilder"/></returns>
        public global::MyUplinkConnector.Client.V2.Internal.Aithp24.Devices.Item.WithDeviceItemRequestBuilder this[string position]
        {
            get
            {
                var urlTplParams = new Dictionary<string, object>(PathParameters);
                urlTplParams.Add("deviceId", position);
                return new global::MyUplinkConnector.Client.V2.Internal.Aithp24.Devices.Item.WithDeviceItemRequestBuilder(urlTplParams, RequestAdapter);
            }
        }
        /// <summary>
        /// Instantiates a new <see cref="global::MyUplinkConnector.Client.V2.Internal.Aithp24.Devices.DevicesRequestBuilder"/> and sets the default values.
        /// </summary>
        /// <param name="pathParameters">Path parameters for the request</param>
        /// <param name="requestAdapter">The request adapter to use to execute the requests.</param>
        public DevicesRequestBuilder(Dictionary<string, object> pathParameters, IRequestAdapter requestAdapter) : base(requestAdapter, "{+baseurl}/v2/internal/aithp24/devices", pathParameters)
        {
        }
        /// <summary>
        /// Instantiates a new <see cref="global::MyUplinkConnector.Client.V2.Internal.Aithp24.Devices.DevicesRequestBuilder"/> and sets the default values.
        /// </summary>
        /// <param name="rawUrl">The raw URL to use for the request builder.</param>
        /// <param name="requestAdapter">The request adapter to use to execute the requests.</param>
        public DevicesRequestBuilder(string rawUrl, IRequestAdapter requestAdapter) : base(requestAdapter, "{+baseurl}/v2/internal/aithp24/devices", rawUrl)
        {
        }
    }
}
