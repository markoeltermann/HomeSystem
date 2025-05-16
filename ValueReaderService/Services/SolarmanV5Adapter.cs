using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;
using SolarmanV5Client;

namespace ValueReaderService.Services;
public class SolarmanV5Adapter
{
    public SolarmanV5Adapter(ConfigModel configModel, IHttpClientFactory httpClientFactory)
    {
        var httpClient = httpClientFactory.CreateClient(nameof(SolarmanV5Adapter));
        httpClient.BaseAddress = new Uri(configModel.ModbusConnectorUrl());

        var adapter = new HttpClientRequestAdapter(new AnonymousAuthenticationProvider(), httpClient: httpClient);
        Client = new SolarmanV5(adapter);
    }

    public SolarmanV5 Client { get; }
}
