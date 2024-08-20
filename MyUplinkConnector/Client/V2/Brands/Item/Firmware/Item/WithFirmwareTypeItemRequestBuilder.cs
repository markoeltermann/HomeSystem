// <auto-generated/>
using Microsoft.Kiota.Abstractions.Extensions;
using Microsoft.Kiota.Abstractions;
using MyUplinkConnector.Client.V2.Brands.Item.Firmware.Item.VersionNamespace;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System;
namespace MyUplinkConnector.Client.V2.Brands.Item.Firmware.Item
{
    /// <summary>
    /// Builds and executes requests for operations under \v2\brands\{brandId}\firmware\{firmwareTypeId}
    /// </summary>
    [global::System.CodeDom.Compiler.GeneratedCode("Kiota", "1.16.0")]
    public partial class WithFirmwareTypeItemRequestBuilder : BaseRequestBuilder
    {
        /// <summary>The version property</summary>
        public global::MyUplinkConnector.Client.V2.Brands.Item.Firmware.Item.VersionNamespace.VersionRequestBuilder Version
        {
            get => new global::MyUplinkConnector.Client.V2.Brands.Item.Firmware.Item.VersionNamespace.VersionRequestBuilder(PathParameters, RequestAdapter);
        }
        /// <summary>
        /// Instantiates a new <see cref="global::MyUplinkConnector.Client.V2.Brands.Item.Firmware.Item.WithFirmwareTypeItemRequestBuilder"/> and sets the default values.
        /// </summary>
        /// <param name="pathParameters">Path parameters for the request</param>
        /// <param name="requestAdapter">The request adapter to use to execute the requests.</param>
        public WithFirmwareTypeItemRequestBuilder(Dictionary<string, object> pathParameters, IRequestAdapter requestAdapter) : base(requestAdapter, "{+baseurl}/v2/brands/{brandId}/firmware/{firmwareTypeId}", pathParameters)
        {
        }
        /// <summary>
        /// Instantiates a new <see cref="global::MyUplinkConnector.Client.V2.Brands.Item.Firmware.Item.WithFirmwareTypeItemRequestBuilder"/> and sets the default values.
        /// </summary>
        /// <param name="rawUrl">The raw URL to use for the request builder.</param>
        /// <param name="requestAdapter">The request adapter to use to execute the requests.</param>
        public WithFirmwareTypeItemRequestBuilder(string rawUrl, IRequestAdapter requestAdapter) : base(requestAdapter, "{+baseurl}/v2/brands/{brandId}/firmware/{firmwareTypeId}", rawUrl)
        {
        }
    }
}