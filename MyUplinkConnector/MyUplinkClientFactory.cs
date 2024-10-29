using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;
using MyUplinkConnector.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MyUplinkConnector;
public static class MyUplinkClientFactory
{
    public static async Task<MyUplinkClient?> Create(HttpClient httpClient, string clientId, string clientSecret)
    {
        var content = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("client_id", clientId),
            new KeyValuePair<string, string>("client_secret", clientSecret),
            new KeyValuePair<string, string>("response_type", "code"),
            new KeyValuePair<string, string>("grant_type", "client_credentials"),
            new KeyValuePair<string, string>("scope", "READSYSTEM WRITESYSTEM"),
            new KeyValuePair<string, string>("redirect_uri", "https://test.com"),
        ]);

        httpClient.BaseAddress = new Uri("https://api.myuplink.com/");
        var response = await httpClient.PostAsync("/oauth/token", content);

        var responseText = await response.Content.ReadAsStringAsync();

        var tokenInfo = JsonSerializer.Deserialize<AccessTokenInfo>(responseText);

        if (tokenInfo?.AccessToken is null)
            return null;

        var tokenProvider = new AccessTokenProvider(tokenInfo.AccessToken);
        var authProvider = new BaseBearerTokenAuthenticationProvider(tokenProvider);
        var adapter = new HttpClientRequestAdapter(authProvider, httpClient: httpClient);
        var client = new MyUplinkClient(adapter);

        return client;
    }

    private class AccessTokenInfo
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("scope")]
        public string? Scope { get; set; }

        [JsonPropertyName("token_type")]
        public string? TokenType { get; set; }
    }

    private class AccessTokenProvider(string token) : IAccessTokenProvider
    {
        public AllowedHostsValidator AllowedHostsValidator => new();

        public Task<string> GetAuthorizationTokenAsync(Uri uri, Dictionary<string, object>? additionalAuthenticationContext = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(token);
        }
    }
}
