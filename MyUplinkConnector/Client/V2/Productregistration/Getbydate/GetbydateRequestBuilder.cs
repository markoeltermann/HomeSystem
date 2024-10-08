// <auto-generated/>
using Microsoft.Kiota.Abstractions.Extensions;
using Microsoft.Kiota.Abstractions;
using MyUplinkConnector.Client.V2.Productregistration.Getbydate.Item;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System;
namespace MyUplinkConnector.Client.V2.Productregistration.Getbydate
{
    /// <summary>
    /// Builds and executes requests for operations under \v2\productregistration\getbydate
    /// </summary>
    [global::System.CodeDom.Compiler.GeneratedCode("Kiota", "1.16.0")]
    public partial class GetbydateRequestBuilder : BaseRequestBuilder
    {
        /// <summary>Gets an item from the MyUplinkConnector.Client.v2.productregistration.getbydate.item collection</summary>
        /// <param name="position">Unique identifier of the item</param>
        /// <returns>A <see cref="global::MyUplinkConnector.Client.V2.Productregistration.Getbydate.Item.WithDateItemRequestBuilder"/></returns>
        public global::MyUplinkConnector.Client.V2.Productregistration.Getbydate.Item.WithDateItemRequestBuilder this[string position]
        {
            get
            {
                var urlTplParams = new Dictionary<string, object>(PathParameters);
                urlTplParams.Add("date", position);
                return new global::MyUplinkConnector.Client.V2.Productregistration.Getbydate.Item.WithDateItemRequestBuilder(urlTplParams, RequestAdapter);
            }
        }
        /// <summary>
        /// Instantiates a new <see cref="global::MyUplinkConnector.Client.V2.Productregistration.Getbydate.GetbydateRequestBuilder"/> and sets the default values.
        /// </summary>
        /// <param name="pathParameters">Path parameters for the request</param>
        /// <param name="requestAdapter">The request adapter to use to execute the requests.</param>
        public GetbydateRequestBuilder(Dictionary<string, object> pathParameters, IRequestAdapter requestAdapter) : base(requestAdapter, "{+baseurl}/v2/productregistration/getbydate", pathParameters)
        {
        }
        /// <summary>
        /// Instantiates a new <see cref="global::MyUplinkConnector.Client.V2.Productregistration.Getbydate.GetbydateRequestBuilder"/> and sets the default values.
        /// </summary>
        /// <param name="rawUrl">The raw URL to use for the request builder.</param>
        /// <param name="requestAdapter">The request adapter to use to execute the requests.</param>
        public GetbydateRequestBuilder(string rawUrl, IRequestAdapter requestAdapter) : base(requestAdapter, "{+baseurl}/v2/productregistration/getbydate", rawUrl)
        {
        }
    }
}
