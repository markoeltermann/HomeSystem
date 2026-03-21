using DynessConnector.Client;
using Microsoft.Kiota.Http.HttpClientLibrary;

namespace DynessConnector;

public static class DynessClientFactory
{
    public static DynessApiClient Create(HttpClient httpClient, string clientId, string clientSecret)
    {
        var authProvider = new DynessApiAuthenticationProvider(clientId, clientSecret);
        var adapter = new HttpClientRequestAdapter(authProvider, httpClient: httpClient);
        var client = new DynessApiClient(adapter);

        return client;
    }
}
