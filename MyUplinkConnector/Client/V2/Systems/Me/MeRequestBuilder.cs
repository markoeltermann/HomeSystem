// <auto-generated/>
using Microsoft.Kiota.Abstractions.Extensions;
using Microsoft.Kiota.Abstractions.Serialization;
using Microsoft.Kiota.Abstractions;
using MyUplinkConnector.Client.Models;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System;
namespace MyUplinkConnector.Client.V2.Systems.Me
{
    /// <summary>
    /// Builds and executes requests for operations under \v2\systems\me
    /// </summary>
    [global::System.CodeDom.Compiler.GeneratedCode("Kiota", "1.16.0")]
    public partial class MeRequestBuilder : BaseRequestBuilder
    {
        /// <summary>
        /// Instantiates a new <see cref="global::MyUplinkConnector.Client.V2.Systems.Me.MeRequestBuilder"/> and sets the default values.
        /// </summary>
        /// <param name="pathParameters">Path parameters for the request</param>
        /// <param name="requestAdapter">The request adapter to use to execute the requests.</param>
        public MeRequestBuilder(Dictionary<string, object> pathParameters, IRequestAdapter requestAdapter) : base(requestAdapter, "{+baseurl}/v2/systems/me{?itemsPerPage*,page*}", pathParameters)
        {
        }
        /// <summary>
        /// Instantiates a new <see cref="global::MyUplinkConnector.Client.V2.Systems.Me.MeRequestBuilder"/> and sets the default values.
        /// </summary>
        /// <param name="rawUrl">The raw URL to use for the request builder.</param>
        /// <param name="requestAdapter">The request adapter to use to execute the requests.</param>
        public MeRequestBuilder(string rawUrl, IRequestAdapter requestAdapter) : base(requestAdapter, "{+baseurl}/v2/systems/me{?itemsPerPage*,page*}", rawUrl)
        {
        }
        /// <summary>
        /// Get user systems.
        /// </summary>
        /// <returns>A <see cref="global::MyUplinkConnector.Client.Models.PagedSystemResult"/></returns>
        /// <param name="cancellationToken">Cancellation token to use when cancelling requests</param>
        /// <param name="requestConfiguration">Configuration for the request such as headers, query parameters, and middleware options.</param>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public async Task<global::MyUplinkConnector.Client.Models.PagedSystemResult?> GetAsync(Action<RequestConfiguration<global::MyUplinkConnector.Client.V2.Systems.Me.MeRequestBuilder.MeRequestBuilderGetQueryParameters>>? requestConfiguration = default, CancellationToken cancellationToken = default)
        {
#nullable restore
#else
        public async Task<global::MyUplinkConnector.Client.Models.PagedSystemResult> GetAsync(Action<RequestConfiguration<global::MyUplinkConnector.Client.V2.Systems.Me.MeRequestBuilder.MeRequestBuilderGetQueryParameters>> requestConfiguration = default, CancellationToken cancellationToken = default)
        {
#endif
            var requestInfo = ToGetRequestInformation(requestConfiguration);
            return await RequestAdapter.SendAsync<global::MyUplinkConnector.Client.Models.PagedSystemResult>(requestInfo, global::MyUplinkConnector.Client.Models.PagedSystemResult.CreateFromDiscriminatorValue, default, cancellationToken).ConfigureAwait(false);
        }
        /// <summary>
        /// Get user systems.
        /// </summary>
        /// <returns>A <see cref="RequestInformation"/></returns>
        /// <param name="requestConfiguration">Configuration for the request such as headers, query parameters, and middleware options.</param>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public RequestInformation ToGetRequestInformation(Action<RequestConfiguration<global::MyUplinkConnector.Client.V2.Systems.Me.MeRequestBuilder.MeRequestBuilderGetQueryParameters>>? requestConfiguration = default)
        {
#nullable restore
#else
        public RequestInformation ToGetRequestInformation(Action<RequestConfiguration<global::MyUplinkConnector.Client.V2.Systems.Me.MeRequestBuilder.MeRequestBuilderGetQueryParameters>> requestConfiguration = default)
        {
#endif
            var requestInfo = new RequestInformation(Method.GET, UrlTemplate, PathParameters);
            requestInfo.Configure(requestConfiguration);
            requestInfo.Headers.TryAdd("Accept", "application/json, text/plain;q=0.9");
            return requestInfo;
        }
        /// <summary>
        /// Returns a request builder with the provided arbitrary URL. Using this method means any other path or query parameters are ignored.
        /// </summary>
        /// <returns>A <see cref="global::MyUplinkConnector.Client.V2.Systems.Me.MeRequestBuilder"/></returns>
        /// <param name="rawUrl">The raw URL to use for the request builder.</param>
        public global::MyUplinkConnector.Client.V2.Systems.Me.MeRequestBuilder WithUrl(string rawUrl)
        {
            return new global::MyUplinkConnector.Client.V2.Systems.Me.MeRequestBuilder(rawUrl, RequestAdapter);
        }
        /// <summary>
        /// Get user systems.
        /// </summary>
        [global::System.CodeDom.Compiler.GeneratedCode("Kiota", "1.16.0")]
        public partial class MeRequestBuilderGetQueryParameters 
        {
            /// <summary>Items per page.</summary>
            [QueryParameter("itemsPerPage")]
            public int? ItemsPerPage { get; set; }
            /// <summary>Page.</summary>
            [QueryParameter("page")]
            public int? Page { get; set; }
        }
        /// <summary>
        /// Configuration for the request such as headers, query parameters, and middleware options.
        /// </summary>
        [Obsolete("This class is deprecated. Please use the generic RequestConfiguration class generated by the generator.")]
        [global::System.CodeDom.Compiler.GeneratedCode("Kiota", "1.16.0")]
        public partial class MeRequestBuilderGetRequestConfiguration : RequestConfiguration<global::MyUplinkConnector.Client.V2.Systems.Me.MeRequestBuilder.MeRequestBuilderGetQueryParameters>
        {
        }
    }
}
