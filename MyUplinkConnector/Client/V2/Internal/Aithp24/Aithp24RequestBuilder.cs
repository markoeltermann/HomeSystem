// <auto-generated/>
using Microsoft.Kiota.Abstractions.Extensions;
using Microsoft.Kiota.Abstractions;
using MyUplinkConnector.Client.V2.Internal.Aithp24.Devices;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System;
namespace MyUplinkConnector.Client.V2.Internal.Aithp24
{
    /// <summary>
    /// Builds and executes requests for operations under \v2\internal\aithp24
    /// </summary>
    [global::System.CodeDom.Compiler.GeneratedCode("Kiota", "1.16.0")]
    public partial class Aithp24RequestBuilder : BaseRequestBuilder
    {
        /// <summary>The devices property</summary>
        public global::MyUplinkConnector.Client.V2.Internal.Aithp24.Devices.DevicesRequestBuilder Devices
        {
            get => new global::MyUplinkConnector.Client.V2.Internal.Aithp24.Devices.DevicesRequestBuilder(PathParameters, RequestAdapter);
        }
        /// <summary>
        /// Instantiates a new <see cref="global::MyUplinkConnector.Client.V2.Internal.Aithp24.Aithp24RequestBuilder"/> and sets the default values.
        /// </summary>
        /// <param name="pathParameters">Path parameters for the request</param>
        /// <param name="requestAdapter">The request adapter to use to execute the requests.</param>
        public Aithp24RequestBuilder(Dictionary<string, object> pathParameters, IRequestAdapter requestAdapter) : base(requestAdapter, "{+baseurl}/v2/internal/aithp24", pathParameters)
        {
        }
        /// <summary>
        /// Instantiates a new <see cref="global::MyUplinkConnector.Client.V2.Internal.Aithp24.Aithp24RequestBuilder"/> and sets the default values.
        /// </summary>
        /// <param name="rawUrl">The raw URL to use for the request builder.</param>
        /// <param name="requestAdapter">The request adapter to use to execute the requests.</param>
        public Aithp24RequestBuilder(string rawUrl, IRequestAdapter requestAdapter) : base(requestAdapter, "{+baseurl}/v2/internal/aithp24", rawUrl)
        {
        }
    }
}