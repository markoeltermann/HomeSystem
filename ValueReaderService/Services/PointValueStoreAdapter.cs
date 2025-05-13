using PointValueStoreClient;
using PointValueStoreClient.Models;

namespace ValueReaderService.Services;

public class PointValueStoreAdapter
{
    private readonly PointValueStore pointValueStore;

    public PointValueStoreAdapter(ConfigModel configModel, IHttpClientFactory httpClientFactory)
    {
        var pointValueStoreConnectorBaseUrl = configModel.PointValueStoreConnectorUrl();
        if (string.IsNullOrWhiteSpace(pointValueStoreConnectorBaseUrl))
            throw new InvalidOperationException("Point value store connector url has not been set up.");

        var httpClient = httpClientFactory.CreateClient(nameof(PointValueStoreAdapter));
        httpClient.BaseAddress = new Uri(pointValueStoreConnectorBaseUrl);
        pointValueStore = PointValueStoreClientFactory.Create(httpClient);
    }

    public async Task<ResponseValueContainerDto> Get(int pointId, DateOnly date)
    {
        var result = await pointValueStore.Points[pointId].Values.GetAsync(x => { x.QueryParameters.From = date; x.QueryParameters.UpTo = date; });
        if (result?.Values == null || result.Values.Count != 24 * 6 + 1)
            throw new InvalidOperationException("Point value store did not return expected response.");

        return result;
    }

    public static double? GetCurrentValue(DateTime timestampLocal, ResponseValueContainerDto values)
    {
        if (values?.Values == null)
            return null;

        double? value = null;
        for (int i = 0; i < values.Values.Count - 1; i++)
        {
            var numericValue = values.Values[i];
            var nextNumericValue = values.Values[i + 1];
            if (numericValue.Timestamp <= timestampLocal && nextNumericValue.Timestamp > timestampLocal)
            {
                value = numericValue.Value;
                break;
            }
        }

        return value;
    }

    public async Task StoreValuesWithReplace(int devicePointId, IList<PointValue> values, DateTime defaultTimestamp)
    {
        var body = new ValueContainerDto
        {
            Values = [.. values.Select(x => new NumericValueDto
            {
                Timestamp = x.TimeStamp ?? defaultTimestamp,
                StringValue = x.Value,
            })]
        };
        await pointValueStore.Points[devicePointId].Values.PutAsync(body);
    }
}
