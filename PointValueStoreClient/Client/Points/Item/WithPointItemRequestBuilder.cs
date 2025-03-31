// <auto-generated/>
using Microsoft.Kiota.Abstractions.Extensions;
using Microsoft.Kiota.Abstractions;
using PointValueStoreClient.Points.Item.Values;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System;
namespace PointValueStoreClient.Points.Item
{
    /// <summary>
    /// Builds and executes requests for operations under \points\{pointId}
    /// </summary>
    [global::System.CodeDom.Compiler.GeneratedCode("Kiota", "1.16.0")]
    public partial class WithPointItemRequestBuilder : BaseRequestBuilder
    {
        /// <summary>The values property</summary>
        public global::PointValueStoreClient.Points.Item.Values.ValuesRequestBuilder Values
        {
            get => new global::PointValueStoreClient.Points.Item.Values.ValuesRequestBuilder(PathParameters, RequestAdapter);
        }
        /// <summary>
        /// Instantiates a new <see cref="global::PointValueStoreClient.Points.Item.WithPointItemRequestBuilder"/> and sets the default values.
        /// </summary>
        /// <param name="pathParameters">Path parameters for the request</param>
        /// <param name="requestAdapter">The request adapter to use to execute the requests.</param>
        public WithPointItemRequestBuilder(Dictionary<string, object> pathParameters, IRequestAdapter requestAdapter) : base(requestAdapter, "{+baseurl}/points/{pointId}", pathParameters)
        {
        }
        /// <summary>
        /// Instantiates a new <see cref="global::PointValueStoreClient.Points.Item.WithPointItemRequestBuilder"/> and sets the default values.
        /// </summary>
        /// <param name="rawUrl">The raw URL to use for the request builder.</param>
        /// <param name="requestAdapter">The request adapter to use to execute the requests.</param>
        public WithPointItemRequestBuilder(string rawUrl, IRequestAdapter requestAdapter) : base(requestAdapter, "{+baseurl}/points/{pointId}", rawUrl)
        {
        }
    }
}
