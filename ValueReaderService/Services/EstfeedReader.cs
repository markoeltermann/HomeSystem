using Domain;
using EstfeedConnector;
using EstfeedConnector.Client.Api.Public.V1.MeteringData;
using System.Text.Json;

namespace ValueReaderService.Services;

public class EstfeedReader(ILogger<DeviceReader> logger, IHttpClientFactory httpClientFactory) : DeviceReader(logger)
{
    public override bool StorePointsWithReplace => true;

    protected override async Task<IList<PointValue>?> ExecuteAsyncInternal(Device device, DateTime timestamp, ICollection<DevicePoint> devicePoints)
    {
        if (device.Address is null)
            return null;

        var address = JsonSerializer.Deserialize<DeviceAddress>(device.Address);
        if (address is null || address.DeviceId is null || address.ClientId is null || address.ClientSecret is null)
            return null;

        using var httpClient = httpClientFactory.CreateClient(nameof(EstfeedReader));
        var client = await EstfeedClientFactory.Create(httpClient, address.ClientId, address.ClientSecret);
        if (client == null)
            return null;

        var localTime = timestamp.ToLocalTime();

        var response = await client.Api.Public.V1.MeteringData.GetAsync(x =>
        {
            x.QueryParameters.StartDateTime = localTime.Date;
            x.QueryParameters.EndDateTime = localTime.Date.AddDays(1);
            x.QueryParameters.ResolutionAsGetResolutionQueryParameterType = GetResolutionQueryParameterType.Fifteen_minutes;
            x.QueryParameters.MeteringPointEics = [address.DeviceId];
        });

        // TODO generate PointValue list based on response and devicePoints

        return null;
    }
}
