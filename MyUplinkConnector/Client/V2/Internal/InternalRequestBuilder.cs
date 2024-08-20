// <auto-generated/>
using Microsoft.Kiota.Abstractions.Extensions;
using Microsoft.Kiota.Abstractions;
using MyUplinkConnector.Client.V2.Internal.Aithp24;
using MyUplinkConnector.Client.V2.Internal.Crm;
using MyUplinkConnector.Client.V2.Internal.Ssg;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System;
namespace MyUplinkConnector.Client.V2.Internal
{
    /// <summary>
    /// Builds and executes requests for operations under \v2\internal
    /// </summary>
    [global::System.CodeDom.Compiler.GeneratedCode("Kiota", "1.16.0")]
    public partial class InternalRequestBuilder : BaseRequestBuilder
    {
        /// <summary>The aithp24 property</summary>
        public global::MyUplinkConnector.Client.V2.Internal.Aithp24.Aithp24RequestBuilder Aithp24
        {
            get => new global::MyUplinkConnector.Client.V2.Internal.Aithp24.Aithp24RequestBuilder(PathParameters, RequestAdapter);
        }
        /// <summary>The crm property</summary>
        public global::MyUplinkConnector.Client.V2.Internal.Crm.CrmRequestBuilder Crm
        {
            get => new global::MyUplinkConnector.Client.V2.Internal.Crm.CrmRequestBuilder(PathParameters, RequestAdapter);
        }
        /// <summary>The ssg property</summary>
        public global::MyUplinkConnector.Client.V2.Internal.Ssg.SsgRequestBuilder Ssg
        {
            get => new global::MyUplinkConnector.Client.V2.Internal.Ssg.SsgRequestBuilder(PathParameters, RequestAdapter);
        }
        /// <summary>
        /// Instantiates a new <see cref="global::MyUplinkConnector.Client.V2.Internal.InternalRequestBuilder"/> and sets the default values.
        /// </summary>
        /// <param name="pathParameters">Path parameters for the request</param>
        /// <param name="requestAdapter">The request adapter to use to execute the requests.</param>
        public InternalRequestBuilder(Dictionary<string, object> pathParameters, IRequestAdapter requestAdapter) : base(requestAdapter, "{+baseurl}/v2/internal", pathParameters)
        {
        }
        /// <summary>
        /// Instantiates a new <see cref="global::MyUplinkConnector.Client.V2.Internal.InternalRequestBuilder"/> and sets the default values.
        /// </summary>
        /// <param name="rawUrl">The raw URL to use for the request builder.</param>
        /// <param name="requestAdapter">The request adapter to use to execute the requests.</param>
        public InternalRequestBuilder(string rawUrl, IRequestAdapter requestAdapter) : base(requestAdapter, "{+baseurl}/v2/internal", rawUrl)
        {
        }
    }
}