// <auto-generated/>
using Microsoft.Kiota.Abstractions.Extensions;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Serialization.Form;
using Microsoft.Kiota.Serialization.Json;
using Microsoft.Kiota.Serialization.Multipart;
using Microsoft.Kiota.Serialization.Text;
using MyUplinkConnector.Client.Assets;
using MyUplinkConnector.Client.Connect;
using MyUplinkConnector.Client.Devices;
using MyUplinkConnector.Client.Oauth;
using MyUplinkConnector.Client.User;
using MyUplinkConnector.Client.V2;
using MyUplinkConnector.Client.V3;
using MyUplinkConnector.Client.WellKnown;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System;
namespace MyUplinkConnector.Client
{
    /// <summary>
    /// The main entry point of the SDK, exposes the configuration and the fluent API.
    /// </summary>
    [global::System.CodeDom.Compiler.GeneratedCode("Kiota", "1.16.0")]
    public partial class MyUplinkClient : BaseRequestBuilder
    {
        /// <summary>The assets property</summary>
        public global::MyUplinkConnector.Client.Assets.AssetsRequestBuilder Assets
        {
            get => new global::MyUplinkConnector.Client.Assets.AssetsRequestBuilder(PathParameters, RequestAdapter);
        }
        /// <summary>The connect property</summary>
        public global::MyUplinkConnector.Client.Connect.ConnectRequestBuilder Connect
        {
            get => new global::MyUplinkConnector.Client.Connect.ConnectRequestBuilder(PathParameters, RequestAdapter);
        }
        /// <summary>The devices property</summary>
        public global::MyUplinkConnector.Client.Devices.DevicesRequestBuilder Devices
        {
            get => new global::MyUplinkConnector.Client.Devices.DevicesRequestBuilder(PathParameters, RequestAdapter);
        }
        /// <summary>The oauth property</summary>
        public global::MyUplinkConnector.Client.Oauth.OauthRequestBuilder Oauth
        {
            get => new global::MyUplinkConnector.Client.Oauth.OauthRequestBuilder(PathParameters, RequestAdapter);
        }
        /// <summary>The user property</summary>
        public global::MyUplinkConnector.Client.User.UserRequestBuilder User
        {
            get => new global::MyUplinkConnector.Client.User.UserRequestBuilder(PathParameters, RequestAdapter);
        }
        /// <summary>The v2 property</summary>
        public global::MyUplinkConnector.Client.V2.V2RequestBuilder V2
        {
            get => new global::MyUplinkConnector.Client.V2.V2RequestBuilder(PathParameters, RequestAdapter);
        }
        /// <summary>The v3 property</summary>
        public global::MyUplinkConnector.Client.V3.V3RequestBuilder V3
        {
            get => new global::MyUplinkConnector.Client.V3.V3RequestBuilder(PathParameters, RequestAdapter);
        }
        /// <summary>The wellKnown property</summary>
        public global::MyUplinkConnector.Client.WellKnown.WellKnownRequestBuilder WellKnown
        {
            get => new global::MyUplinkConnector.Client.WellKnown.WellKnownRequestBuilder(PathParameters, RequestAdapter);
        }
        /// <summary>
        /// Instantiates a new <see cref="global::MyUplinkConnector.Client.MyUplinkClient"/> and sets the default values.
        /// </summary>
        /// <param name="requestAdapter">The request adapter to use to execute the requests.</param>
        public MyUplinkClient(IRequestAdapter requestAdapter) : base(requestAdapter, "{+baseurl}", new Dictionary<string, object>())
        {
            ApiClientBuilder.RegisterDefaultSerializer<JsonSerializationWriterFactory>();
            ApiClientBuilder.RegisterDefaultSerializer<TextSerializationWriterFactory>();
            ApiClientBuilder.RegisterDefaultSerializer<FormSerializationWriterFactory>();
            ApiClientBuilder.RegisterDefaultSerializer<MultipartSerializationWriterFactory>();
            ApiClientBuilder.RegisterDefaultDeserializer<JsonParseNodeFactory>();
            ApiClientBuilder.RegisterDefaultDeserializer<TextParseNodeFactory>();
            ApiClientBuilder.RegisterDefaultDeserializer<FormParseNodeFactory>();
        }
    }
}
