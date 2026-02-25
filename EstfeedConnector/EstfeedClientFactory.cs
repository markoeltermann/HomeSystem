using EstfeedConnector.Client;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EstfeedConnector;

public static class EstfeedClientFactory
{
    public static async Task<EstfeedClient?> Create(HttpClient httpClient, string clientId, string clientSecret)
    {
        var content = new FormUrlEncodedContent([
            new KeyValuePair<string, string>("client_id", clientId),
            new KeyValuePair<string, string>("client_secret", clientSecret),
            new KeyValuePair<string, string>("grant_type", "client_credentials"),
        ]);

        var response = await httpClient.PostAsync("https://kc.elering.ee/realms/elering-sso/protocol/openid-connect/token", content);

        var responseText = await response.Content.ReadAsStringAsync();

        var tokenInfo = JsonSerializer.Deserialize<AccessTokenInfo>(responseText);

        if (tokenInfo?.AccessToken is null)
            return null;

        //httpClient.BaseAddress = new Uri("https://estfeed.elering.ee/");

        var tokenProvider = new AccessTokenProvider(tokenInfo.AccessToken);
        var authProvider = new BaseBearerTokenAuthenticationProvider(tokenProvider);
        var adapter = new HttpClientRequestAdapter(authProvider, httpClient: httpClient);
        var client = new EstfeedClient(adapter);

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
