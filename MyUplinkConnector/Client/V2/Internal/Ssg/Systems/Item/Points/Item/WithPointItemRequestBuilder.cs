// <auto-generated/>
using Microsoft.Kiota.Abstractions.Extensions;
using Microsoft.Kiota.Abstractions;
using MyUplinkConnector.Client.V2.Internal.Ssg.Systems.Item.Points.Item.Item;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System;
namespace MyUplinkConnector.Client.V2.Internal.Ssg.Systems.Item.Points.Item
{
    /// <summary>
    /// Builds and executes requests for operations under \v2\internal\ssg\systems\{systemId}\points\{pointId}
    /// </summary>
    [global::System.CodeDom.Compiler.GeneratedCode("Kiota", "1.16.0")]
    public partial class WithPointItemRequestBuilder : BaseRequestBuilder
    {
        /// <summary>Gets an item from the MyUplinkConnector.Client.v2.internal.ssg.systems.item.points.item.item collection</summary>
        /// <param name="position">Unique identifier of the item</param>
        /// <returns>A <see cref="global::MyUplinkConnector.Client.V2.Internal.Ssg.Systems.Item.Points.Item.Item.WithDateFromItemRequestBuilder"/></returns>
        public global::MyUplinkConnector.Client.V2.Internal.Ssg.Systems.Item.Points.Item.Item.WithDateFromItemRequestBuilder this[string position]
        {
            get
            {
                var urlTplParams = new Dictionary<string, object>(PathParameters);
                urlTplParams.Add("dateFrom", position);
                return new global::MyUplinkConnector.Client.V2.Internal.Ssg.Systems.Item.Points.Item.Item.WithDateFromItemRequestBuilder(urlTplParams, RequestAdapter);
            }
        }
        /// <summary>
        /// Instantiates a new <see cref="global::MyUplinkConnector.Client.V2.Internal.Ssg.Systems.Item.Points.Item.WithPointItemRequestBuilder"/> and sets the default values.
        /// </summary>
        /// <param name="pathParameters">Path parameters for the request</param>
        /// <param name="requestAdapter">The request adapter to use to execute the requests.</param>
        public WithPointItemRequestBuilder(Dictionary<string, object> pathParameters, IRequestAdapter requestAdapter) : base(requestAdapter, "{+baseurl}/v2/internal/ssg/systems/{systemId}/points/{pointId}", pathParameters)
        {
        }
        /// <summary>
        /// Instantiates a new <see cref="global::MyUplinkConnector.Client.V2.Internal.Ssg.Systems.Item.Points.Item.WithPointItemRequestBuilder"/> and sets the default values.
        /// </summary>
        /// <param name="rawUrl">The raw URL to use for the request builder.</param>
        /// <param name="requestAdapter">The request adapter to use to execute the requests.</param>
        public WithPointItemRequestBuilder(string rawUrl, IRequestAdapter requestAdapter) : base(requestAdapter, "{+baseurl}/v2/internal/ssg/systems/{systemId}/points/{pointId}", rawUrl)
        {
        }
    }
}
