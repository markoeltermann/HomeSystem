using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;

namespace PointValueStoreClient;

public static class PointValueStoreClientFactory
{
    public static PointValueStore Create(HttpClient httpClient)
    {
        var adapter = new HttpClientRequestAdapter(new AnonymousAuthenticationProvider(), httpClient: httpClient);
        var client = new PointValueStore(adapter);

        return client;
    }
}
