// <auto-generated/>
using Microsoft.Kiota.Abstractions.Extensions;
using Microsoft.Kiota.Abstractions.Serialization;
using Microsoft.Kiota.Abstractions;
using MyUplinkConnector.Client.Models;
using MyUplinkConnector.Client.V2.Devices.Item.AidMode;
using MyUplinkConnector.Client.V2.Devices.Item.FirmwareInfo;
using MyUplinkConnector.Client.V2.Devices.Item.Points;
using MyUplinkConnector.Client.V2.Devices.Item.ProductMetadata;
using MyUplinkConnector.Client.V2.Devices.Item.SmartHomeCategories;
using MyUplinkConnector.Client.V2.Devices.Item.SmartHomeZones;
using MyUplinkConnector.Client.V2.Devices.Item.Zones;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System;
namespace MyUplinkConnector.Client.V2.Devices.Item
{
    /// <summary>
    /// Builds and executes requests for operations under \v2\devices\{deviceId}
    /// </summary>
    [global::System.CodeDom.Compiler.GeneratedCode("Kiota", "1.16.0")]
    public partial class WithDeviceItemRequestBuilder : BaseRequestBuilder
    {
        /// <summary>The aidMode property</summary>
        public global::MyUplinkConnector.Client.V2.Devices.Item.AidMode.AidModeRequestBuilder AidMode
        {
            get => new global::MyUplinkConnector.Client.V2.Devices.Item.AidMode.AidModeRequestBuilder(PathParameters, RequestAdapter);
        }
        /// <summary>The firmwareInfo property</summary>
        public global::MyUplinkConnector.Client.V2.Devices.Item.FirmwareInfo.FirmwareInfoRequestBuilder FirmwareInfo
        {
            get => new global::MyUplinkConnector.Client.V2.Devices.Item.FirmwareInfo.FirmwareInfoRequestBuilder(PathParameters, RequestAdapter);
        }
        /// <summary>The points property</summary>
        public global::MyUplinkConnector.Client.V2.Devices.Item.Points.PointsRequestBuilder Points
        {
            get => new global::MyUplinkConnector.Client.V2.Devices.Item.Points.PointsRequestBuilder(PathParameters, RequestAdapter);
        }
        /// <summary>The productMetadata property</summary>
        public global::MyUplinkConnector.Client.V2.Devices.Item.ProductMetadata.ProductMetadataRequestBuilder ProductMetadata
        {
            get => new global::MyUplinkConnector.Client.V2.Devices.Item.ProductMetadata.ProductMetadataRequestBuilder(PathParameters, RequestAdapter);
        }
        /// <summary>The smartHomeCategories property</summary>
        public global::MyUplinkConnector.Client.V2.Devices.Item.SmartHomeCategories.SmartHomeCategoriesRequestBuilder SmartHomeCategories
        {
            get => new global::MyUplinkConnector.Client.V2.Devices.Item.SmartHomeCategories.SmartHomeCategoriesRequestBuilder(PathParameters, RequestAdapter);
        }
        /// <summary>The smartHomeZones property</summary>
        public global::MyUplinkConnector.Client.V2.Devices.Item.SmartHomeZones.SmartHomeZonesRequestBuilder SmartHomeZones
        {
            get => new global::MyUplinkConnector.Client.V2.Devices.Item.SmartHomeZones.SmartHomeZonesRequestBuilder(PathParameters, RequestAdapter);
        }
        /// <summary>The zones property</summary>
        public global::MyUplinkConnector.Client.V2.Devices.Item.Zones.ZonesRequestBuilder Zones
        {
            get => new global::MyUplinkConnector.Client.V2.Devices.Item.Zones.ZonesRequestBuilder(PathParameters, RequestAdapter);
        }
        /// <summary>
        /// Instantiates a new <see cref="global::MyUplinkConnector.Client.V2.Devices.Item.WithDeviceItemRequestBuilder"/> and sets the default values.
        /// </summary>
        /// <param name="pathParameters">Path parameters for the request</param>
        /// <param name="requestAdapter">The request adapter to use to execute the requests.</param>
        public WithDeviceItemRequestBuilder(Dictionary<string, object> pathParameters, IRequestAdapter requestAdapter) : base(requestAdapter, "{+baseurl}/v2/devices/{deviceId}", pathParameters)
        {
        }
        /// <summary>
        /// Instantiates a new <see cref="global::MyUplinkConnector.Client.V2.Devices.Item.WithDeviceItemRequestBuilder"/> and sets the default values.
        /// </summary>
        /// <param name="rawUrl">The raw URL to use for the request builder.</param>
        /// <param name="requestAdapter">The request adapter to use to execute the requests.</param>
        public WithDeviceItemRequestBuilder(string rawUrl, IRequestAdapter requestAdapter) : base(requestAdapter, "{+baseurl}/v2/devices/{deviceId}", rawUrl)
        {
        }
        /// <summary>
        /// Device querying endpoint.
        /// </summary>
        /// <returns>A <see cref="global::MyUplinkConnector.Client.Models.DeviceResponseModel"/></returns>
        /// <param name="cancellationToken">Cancellation token to use when cancelling requests</param>
        /// <param name="requestConfiguration">Configuration for the request such as headers, query parameters, and middleware options.</param>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public async Task<global::MyUplinkConnector.Client.Models.DeviceResponseModel?> GetAsync(Action<RequestConfiguration<DefaultQueryParameters>>? requestConfiguration = default, CancellationToken cancellationToken = default)
        {
#nullable restore
#else
        public async Task<global::MyUplinkConnector.Client.Models.DeviceResponseModel> GetAsync(Action<RequestConfiguration<DefaultQueryParameters>> requestConfiguration = default, CancellationToken cancellationToken = default)
        {
#endif
            var requestInfo = ToGetRequestInformation(requestConfiguration);
            return await RequestAdapter.SendAsync<global::MyUplinkConnector.Client.Models.DeviceResponseModel>(requestInfo, global::MyUplinkConnector.Client.Models.DeviceResponseModel.CreateFromDiscriminatorValue, default, cancellationToken).ConfigureAwait(false);
        }
        /// <summary>
        /// Device querying endpoint.
        /// </summary>
        /// <returns>A <see cref="RequestInformation"/></returns>
        /// <param name="requestConfiguration">Configuration for the request such as headers, query parameters, and middleware options.</param>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_1_OR_GREATER
#nullable enable
        public RequestInformation ToGetRequestInformation(Action<RequestConfiguration<DefaultQueryParameters>>? requestConfiguration = default)
        {
#nullable restore
#else
        public RequestInformation ToGetRequestInformation(Action<RequestConfiguration<DefaultQueryParameters>> requestConfiguration = default)
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
        /// <returns>A <see cref="global::MyUplinkConnector.Client.V2.Devices.Item.WithDeviceItemRequestBuilder"/></returns>
        /// <param name="rawUrl">The raw URL to use for the request builder.</param>
        public global::MyUplinkConnector.Client.V2.Devices.Item.WithDeviceItemRequestBuilder WithUrl(string rawUrl)
        {
            return new global::MyUplinkConnector.Client.V2.Devices.Item.WithDeviceItemRequestBuilder(rawUrl, RequestAdapter);
        }
        /// <summary>
        /// Configuration for the request such as headers, query parameters, and middleware options.
        /// </summary>
        [Obsolete("This class is deprecated. Please use the generic RequestConfiguration class generated by the generator.")]
        [global::System.CodeDom.Compiler.GeneratedCode("Kiota", "1.16.0")]
        public partial class WithDeviceItemRequestBuilderGetRequestConfiguration : RequestConfiguration<DefaultQueryParameters>
        {
        }
    }
}
