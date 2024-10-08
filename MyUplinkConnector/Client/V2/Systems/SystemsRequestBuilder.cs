// <auto-generated/>
using Microsoft.Kiota.Abstractions.Extensions;
using Microsoft.Kiota.Abstractions;
using MyUplinkConnector.Client.V2.Systems.Item;
using MyUplinkConnector.Client.V2.Systems.Me;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System;
namespace MyUplinkConnector.Client.V2.Systems
{
    /// <summary>
    /// Builds and executes requests for operations under \v2\systems
    /// </summary>
    [global::System.CodeDom.Compiler.GeneratedCode("Kiota", "1.16.0")]
    public partial class SystemsRequestBuilder : BaseRequestBuilder
    {
        /// <summary>The me property</summary>
        public global::MyUplinkConnector.Client.V2.Systems.Me.MeRequestBuilder Me
        {
            get => new global::MyUplinkConnector.Client.V2.Systems.Me.MeRequestBuilder(PathParameters, RequestAdapter);
        }
        /// <summary>Gets an item from the MyUplinkConnector.Client.v2.systems.item collection</summary>
        /// <param name="position">Unique identifier of the item</param>
        /// <returns>A <see cref="global::MyUplinkConnector.Client.V2.Systems.Item.WithSystemItemRequestBuilder"/></returns>
        public global::MyUplinkConnector.Client.V2.Systems.Item.WithSystemItemRequestBuilder this[string position]
        {
            get
            {
                var urlTplParams = new Dictionary<string, object>(PathParameters);
                urlTplParams.Add("systemId", position);
                return new global::MyUplinkConnector.Client.V2.Systems.Item.WithSystemItemRequestBuilder(urlTplParams, RequestAdapter);
            }
        }
        /// <summary>
        /// Instantiates a new <see cref="global::MyUplinkConnector.Client.V2.Systems.SystemsRequestBuilder"/> and sets the default values.
        /// </summary>
        /// <param name="pathParameters">Path parameters for the request</param>
        /// <param name="requestAdapter">The request adapter to use to execute the requests.</param>
        public SystemsRequestBuilder(Dictionary<string, object> pathParameters, IRequestAdapter requestAdapter) : base(requestAdapter, "{+baseurl}/v2/systems", pathParameters)
        {
        }
        /// <summary>
        /// Instantiates a new <see cref="global::MyUplinkConnector.Client.V2.Systems.SystemsRequestBuilder"/> and sets the default values.
        /// </summary>
        /// <param name="rawUrl">The raw URL to use for the request builder.</param>
        /// <param name="requestAdapter">The request adapter to use to execute the requests.</param>
        public SystemsRequestBuilder(string rawUrl, IRequestAdapter requestAdapter) : base(requestAdapter, "{+baseurl}/v2/systems", rawUrl)
        {
        }
    }
}
