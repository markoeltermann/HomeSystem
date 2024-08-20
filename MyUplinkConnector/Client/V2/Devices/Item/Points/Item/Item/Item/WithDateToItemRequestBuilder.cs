// <auto-generated/>
using Microsoft.Kiota.Abstractions.Extensions;
using Microsoft.Kiota.Abstractions;
using MyUplinkConnector.Client.V2.Devices.Item.Points.Item.Item.Item.Item;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System;
namespace MyUplinkConnector.Client.V2.Devices.Item.Points.Item.Item.Item
{
    /// <summary>
    /// Builds and executes requests for operations under \v2\devices\{deviceId}\points\{parameter-id}\{dateFrom}\{dateTo}
    /// </summary>
    [global::System.CodeDom.Compiler.GeneratedCode("Kiota", "1.16.0")]
    public partial class WithDateToItemRequestBuilder : BaseRequestBuilder
    {
        /// <summary>Gets an item from the MyUplinkConnector.Client.v2.devices.item.points.item.item.item.item collection</summary>
        /// <param name="position">Unique identifier of the item</param>
        /// <returns>A <see cref="global::MyUplinkConnector.Client.V2.Devices.Item.Points.Item.Item.Item.Item.WithAggregationMethodItemRequestBuilder"/></returns>
        public global::MyUplinkConnector.Client.V2.Devices.Item.Points.Item.Item.Item.Item.WithAggregationMethodItemRequestBuilder this[string position]
        {
            get
            {
                var urlTplParams = new Dictionary<string, object>(PathParameters);
                urlTplParams.Add("aggregationMethod", position);
                return new global::MyUplinkConnector.Client.V2.Devices.Item.Points.Item.Item.Item.Item.WithAggregationMethodItemRequestBuilder(urlTplParams, RequestAdapter);
            }
        }
        /// <summary>
        /// Instantiates a new <see cref="global::MyUplinkConnector.Client.V2.Devices.Item.Points.Item.Item.Item.WithDateToItemRequestBuilder"/> and sets the default values.
        /// </summary>
        /// <param name="pathParameters">Path parameters for the request</param>
        /// <param name="requestAdapter">The request adapter to use to execute the requests.</param>
        public WithDateToItemRequestBuilder(Dictionary<string, object> pathParameters, IRequestAdapter requestAdapter) : base(requestAdapter, "{+baseurl}/v2/devices/{deviceId}/points/{parameter%2Did}/{dateFrom}/{dateTo}", pathParameters)
        {
        }
        /// <summary>
        /// Instantiates a new <see cref="global::MyUplinkConnector.Client.V2.Devices.Item.Points.Item.Item.Item.WithDateToItemRequestBuilder"/> and sets the default values.
        /// </summary>
        /// <param name="rawUrl">The raw URL to use for the request builder.</param>
        /// <param name="requestAdapter">The request adapter to use to execute the requests.</param>
        public WithDateToItemRequestBuilder(string rawUrl, IRequestAdapter requestAdapter) : base(requestAdapter, "{+baseurl}/v2/devices/{deviceId}/points/{parameter%2Did}/{dateFrom}/{dateTo}", rawUrl)
        {
        }
    }
}