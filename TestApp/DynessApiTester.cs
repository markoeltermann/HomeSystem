using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace TestApp;

public class DynessApiTester(IHttpClientFactory httpClientFactory, ILogger<DynessApiTester> logger) : BackgroundService
{
    #region Copilot generated version
    private const string BaseUrl = "http://open-api.dyness.com/openapi/ems-device";

    public async Task<string> CallApiAsync(string endpointPath, string payload, string apiId, string apiSecret, CancellationToken cancellationToken = default)
    {
        var httpClient = httpClientFactory.CreateClient();
        var dateStr = DateTime.UtcNow.ToString("ddd, dd MMM yyyy HH:mm:ss 'GMT'", CultureInfo.InvariantCulture);
        var contentType = "application/json;charset=UTF-8";
        var contentMd5 = CalculateMd5(payload);
        var canonicalResource = "/ems-device" + endpointPath;
        var verb = string.IsNullOrEmpty(payload) ? "GET" : "POST";

        var stringToSign = $"{verb}\n{contentMd5}\n{contentType}\n{dateStr}\n{canonicalResource}";
        var signature = CalculateSignature(apiSecret, stringToSign);

        var request = new HttpRequestMessage(new HttpMethod(verb), BaseUrl + endpointPath);
        request.Headers.TryAddWithoutValidation("Date", dateStr);
        request.Headers.TryAddWithoutValidation("Authorization", $"{apiId}:{signature}");

        if (!string.IsNullOrEmpty(contentMd5))
        {
            request.Headers.TryAddWithoutValidation("Content-MD5", contentMd5);
        }

        if (!string.IsNullOrEmpty(payload))
        {
            request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
            request.Content.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse(contentType);
        }

        var response = await httpClient.SendAsync(request, cancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return $"Error: {response.StatusCode}\n{responseContent}";
        }

        return responseContent;
    }

    private static string CalculateMd5(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(hash);
    }

    private static string CalculateSignature(string secret, string stringToSign)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var dataBytes = Encoding.UTF8.GetBytes(stringToSign);
        var hash = HMACSHA1.HashData(keyBytes, dataBytes);
        return Convert.ToBase64String(hash);
    }
    #endregion

    #region Converted from java example

    public static async Task Main(string[] args)
    {
        // API Credentials
        string key = "";
        string keySecret = "";

        // Request Body
        var map = new Dictionary<string, object>
                {
                    { "pageNo", 1 },
                    { "pageSize", 10 }
                };
        string path = "/v1/device/getLastPowerDataBySn";
        await CallApi2Async(key, keySecret, map, path, null);
    }

    private static async Task<string> CallApi2Async(string key, string keySecret, Dictionary<string, object>? map, string path, string? queryParams)
    {
        string body = map == null ? "{}" : JsonConvert.SerializeObject(map);
        string contentMd5 = GetDigest(body);
        string date = GetGMTTime();

        // Constructing the string to sign
        string param = (map == null ? "GET" : "POST") + "\n" +
                       contentMd5 + "\n" +
                       "application/json" + "\n" +
                       //"null" + "\n" +
                       //"" + "\n" +
                       //"" + "\n" +
                       date + "\n" +
                       path +
                       (string.IsNullOrEmpty(queryParams) ? "" : "\n" + queryParams);

        string sign = HmacSHA1Encrypt(param, keySecret);
        string url = "https://open-api.dyness.com/openapi/ems-device" + path + (string.IsNullOrEmpty(queryParams) ? string.Empty : "?" + queryParams);

        using var client = new HttpClient();
        var request = new HttpRequestMessage(map == null ? HttpMethod.Get : HttpMethod.Post, url);

        // Setting Headers
        request.Headers.Add("Authorization", "API " + key + ":" + sign);
        //request.Headers.TryAddWithoutValidation("Content-MD5", contentMd5);
        request.Headers.TryAddWithoutValidation("Date", date);

        // Setting Content
        //if (map != null)
        {
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            //content.Headers.ContentType = new MediaTypeHeaderValue("application/json")
            //{
            //    CharSet = "UTF-8"
            //};
            request.Content = content;
        }
        //else
        //{
        //    request.Content = new ByteArrayContent(Array.Empty<byte>());
        //    //request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json")
        //    //{
        //    //    CharSet = "UTF-8"
        //    //};
        //}

        var response = await client.SendAsync(request);
        return await response.Content.ReadAsStringAsync();
    }

    /// <summary>
    /// Generates HmacSHA1 signature and encodes to Base64
    /// </summary>
    public static string HmacSHA1Encrypt(string encryptText, string keySecret)
    {
        byte[] keyBytes = Encoding.UTF8.GetBytes(keySecret);
        using (var hmac = new HMACSHA1(keyBytes))
        {
            byte[] textBytes = Encoding.UTF8.GetBytes(encryptText);
            byte[] hashBytes = hmac.ComputeHash(textBytes);
            return Convert.ToBase64String(hashBytes);
        }
    }

    /// <summary>
    /// Gets current time in GMT format: EEE, d MMM yyyy HH:mm:ss 'GMT'
    /// </summary>
    public static string GetGMTTime()
    {
        return DateTime.UtcNow.ToString("ddd, dd MMM yyyy HH:mm:ss 'GMT'",
            System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Generates MD5 hash of the body and encodes to Base64
    /// </summary>
    public static string GetDigest(string test)
    {
        using (var md5 = MD5.Create())
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(test);
            byte[] hashBytes = md5.ComputeHash(inputBytes);
            return Convert.ToBase64String(hashBytes);
        }
    }

    #endregion

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Example: Get bind device SN list
        const string apiId = "133062721551939";
        const string apiSecret = "4a6edca5cb46147f9f75f5c4c3bb78a";
        //const string endpoint = "/v1/device/storage/list";
        //const string endpoint = "/v1​/device​/getBindDeviceSnListByCurrentUserId";
        const string endpoint = "/v1/device/realTime/data";
        //const string endpoint = "/v1/device/bindSn";
        //const string endpoint = "/v1/device/unBindSn";
        //const string endpoint = "/v1/device/read";
        //const string endpoint = "/v1/one/realTime";
        //const string payload = """
        //    {
        //      "deviceSn": "R07E7C46681A0E84-BDU",
        //      "deviceType": null,
        //      "pageNum": 1,
        //      "pageSize": 10
        //    }
        //    """;

        var payload = new Dictionary<string, object>
        {
            ["deviceSn"] = "R07E7C46681A0E84-BDU",
            //["dongleSn"] = "R07E7C46681A0E84",
            //["startAddress"] = "100",
            //["endAddress"] = "110",
            //["checkCode"] = "123456789",
            //["deviceType"] = null,
            //["pageNum"] = 1,
            //["pageSize"] = 10
        };

        //logger.LogInformation("Calling Dyness API: {Endpoint}", endpoint);
        //var response = await CallApiAsync(endpoint, payload, apiId, apiSecret, stoppingToken);
        var response = await CallApi2Async(apiId, apiSecret, payload, endpoint, null);
        //var response = await CallApi2Async(apiId, apiSecret, null, endpoint, null);
        //logger.LogInformation("API Response for {Endpoint}: {Response}", endpoint, response);
    }
}
