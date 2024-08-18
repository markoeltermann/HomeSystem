// <auto-generated/>
using Microsoft.Kiota.Abstractions.Extensions;
using Microsoft.Kiota.Abstractions;
using MyUplinkConnector.Client.V2.Brands;
using MyUplinkConnector.Client.V2.Devices;
using MyUplinkConnector.Client.V2.Firmware;
using MyUplinkConnector.Client.V2.Internal;
using MyUplinkConnector.Client.V2.Ping;
using MyUplinkConnector.Client.V2.Productregistration;
using MyUplinkConnector.Client.V2.ProtectedPing;
using MyUplinkConnector.Client.V2.Spotprices;
using MyUplinkConnector.Client.V2.Systems;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System;
namespace MyUplinkConnector.Client.V2
{
    /// <summary>
    /// Builds and executes requests for operations under \v2
    /// </summary>
    [global::System.CodeDom.Compiler.GeneratedCode("Kiota", "1.16.0")]
    public partial class V2RequestBuilder : BaseRequestBuilder
    {
        /// <summary>The brands property</summary>
        public global::MyUplinkConnector.Client.V2.Brands.BrandsRequestBuilder Brands
        {
            get => new global::MyUplinkConnector.Client.V2.Brands.BrandsRequestBuilder(PathParameters, RequestAdapter);
        }
        /// <summary>The devices property</summary>
        public global::MyUplinkConnector.Client.V2.Devices.DevicesRequestBuilder Devices
        {
            get => new global::MyUplinkConnector.Client.V2.Devices.DevicesRequestBuilder(PathParameters, RequestAdapter);
        }
        /// <summary>The firmware property</summary>
        public global::MyUplinkConnector.Client.V2.Firmware.FirmwareRequestBuilder Firmware
        {
            get => new global::MyUplinkConnector.Client.V2.Firmware.FirmwareRequestBuilder(PathParameters, RequestAdapter);
        }
        /// <summary>The internal property</summary>
        public global::MyUplinkConnector.Client.V2.Internal.InternalRequestBuilder Internal
        {
            get => new global::MyUplinkConnector.Client.V2.Internal.InternalRequestBuilder(PathParameters, RequestAdapter);
        }
        /// <summary>The ping property</summary>
        public global::MyUplinkConnector.Client.V2.Ping.PingRequestBuilder Ping
        {
            get => new global::MyUplinkConnector.Client.V2.Ping.PingRequestBuilder(PathParameters, RequestAdapter);
        }
        /// <summary>The productregistration property</summary>
        public global::MyUplinkConnector.Client.V2.Productregistration.ProductregistrationRequestBuilder Productregistration
        {
            get => new global::MyUplinkConnector.Client.V2.Productregistration.ProductregistrationRequestBuilder(PathParameters, RequestAdapter);
        }
        /// <summary>The protectedPing property</summary>
        public global::MyUplinkConnector.Client.V2.ProtectedPing.ProtectedPingRequestBuilder ProtectedPing
        {
            get => new global::MyUplinkConnector.Client.V2.ProtectedPing.ProtectedPingRequestBuilder(PathParameters, RequestAdapter);
        }
        /// <summary>The spotprices property</summary>
        public global::MyUplinkConnector.Client.V2.Spotprices.SpotpricesRequestBuilder Spotprices
        {
            get => new global::MyUplinkConnector.Client.V2.Spotprices.SpotpricesRequestBuilder(PathParameters, RequestAdapter);
        }
        /// <summary>The systems property</summary>
        public global::MyUplinkConnector.Client.V2.Systems.SystemsRequestBuilder Systems
        {
            get => new global::MyUplinkConnector.Client.V2.Systems.SystemsRequestBuilder(PathParameters, RequestAdapter);
        }
        /// <summary>
        /// Instantiates a new <see cref="global::MyUplinkConnector.Client.V2.V2RequestBuilder"/> and sets the default values.
        /// </summary>
        /// <param name="pathParameters">Path parameters for the request</param>
        /// <param name="requestAdapter">The request adapter to use to execute the requests.</param>
        public V2RequestBuilder(Dictionary<string, object> pathParameters, IRequestAdapter requestAdapter) : base(requestAdapter, "{+baseurl}/v2", pathParameters)
        {
        }
        /// <summary>
        /// Instantiates a new <see cref="global::MyUplinkConnector.Client.V2.V2RequestBuilder"/> and sets the default values.
        /// </summary>
        /// <param name="rawUrl">The raw URL to use for the request builder.</param>
        /// <param name="requestAdapter">The request adapter to use to execute the requests.</param>
        public V2RequestBuilder(string rawUrl, IRequestAdapter requestAdapter) : base(requestAdapter, "{+baseurl}/v2", rawUrl)
        {
        }
    }
}
