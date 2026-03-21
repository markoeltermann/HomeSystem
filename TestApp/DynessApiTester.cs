using DynessConnector;
using DynessConnector.Client.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Kiota.Http.HttpClientLibrary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace TestApp;

public class DynessApiTester(IHttpClientFactory httpClientFactory, ILogger<DynessApiTester> logger, IConfiguration configuration) : BackgroundService
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

    private static async Task<string> CallApi2Async(string key, string keySecret, Dictionary<string, object>? map, string path, string? queryParams)
    {
        //string body = JsonConvert.SerializeObject(map);
        string body = map == null ? "" : JsonSerializer.Serialize(map);
        //var body = "";
        string contentMd5 = GetDigest(body);
        string date = GetGMTTime();

        string param;
        if (map != null)
        {
            // Constructing the string to sign
            param = (map == null ? "GET" : "POST") + "\n" +
                contentMd5 + "\n" +
                "application/json" + "\n" +
                //"text/plain" + "\n" +
                //"null" + "\n" +
                //"" + "\n" +
                //"" + "\n" +
                date + "\n" +
                path + //"\n" +
                (string.IsNullOrEmpty(queryParams) ? "" : "\n" + queryParams);
        }
        else
        {
            param = "GET" + "\n" +
                //contentMd5 + "\n" +
                //"application/json" + "\n" +
                //"text/plain" + "\n" +
                //"null" + "\n" +
                "" + "\n" +
                "" + "\n" +
                date + "\n" +
                path + //"\n" +
                (string.IsNullOrEmpty(queryParams) ? "" : "?" + queryParams);
            //queryParams;
        }

        string sign = HmacSHA1Encrypt(param, keySecret);
        string url = "https://open-api.dyness.com/openapi/ems-device" + path + (string.IsNullOrEmpty(queryParams) ? string.Empty : "?" + queryParams);

        using var client = new HttpClient();
        var request = new HttpRequestMessage(map == null ? HttpMethod.Get : HttpMethod.Post, url);

        // Setting Headers
        request.Headers.Add("Authorization", "API " + key + ":" + sign);
        //request.Headers.TryAddWithoutValidation("Content-MD5", contentMd5);
        request.Headers.TryAddWithoutValidation("Date", date);

        // Setting Content
        if (map != null)
        {
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            //content.Headers.ContentType = new MediaTypeHeaderValue("application/json")
            //{
            //    CharSet = "UTF-8"
            //};
            //content.Headers.TryAddWithoutValidation("Content-MD5", contentMd5);
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

    //protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    //{
    //    var apiId = configuration["DynessAppId"] ?? throw new InvalidOperationException();
    //    var apiSecret = configuration["DynessAppSecret"] ?? throw new InvalidOperationException();

    //    //const string endpoint = "/v1/device/storage/list";
    //    //const string endpoint = "/v1​/device​/getBindDeviceSnListByCurrentUserId";
    //    const string endpoint = "/v1/device/realTime/data";
    //    //const string endpoint = "/v1/device/bindSn";
    //    //const string endpoint = "/v1/device/unBindSn";
    //    //const string endpoint = "/v1/device/read";
    //    //const string endpoint = "/v1/group/getGroupList";
    //    //const string endpoint = "/v1/device/singleGetChargeDischargeConfig";
    //    //const string endpoint = "/v1/one/realTime";
    //    //const string endpoint = "/v1/device​/getLastPowerDataBySn";
    //    //const string payload = """
    //    //    {
    //    //      "deviceSn": "R07E7C46681A0E84-BDU",
    //    //      "deviceType": null,
    //    //      "pageNum": 1,
    //    //      "pageSize": 10
    //    //    }
    //    //    """;

    //    var payload = new Dictionary<string, object>
    //    {
    //        ["deviceSn"] = "R07E7C46681A0E84-BDU-01",
    //        //["dongleSn"] = "R07E7C46681A0E84",
    //        //["endAddress"] = 853,
    //        //["startAddress"] = 853,
    //        //["checkCode"] = "123456789",
    //        //["deviceType"] = null,
    //        //["pageNum"] = 1,
    //        //["pageSize"] = 10
    //    };

    //    //logger.LogInformation("Calling Dyness API: {Endpoint}", endpoint);
    //    //var response = await CallApiAsync(endpoint, payload, apiId, apiSecret, stoppingToken);
    //    var response = await CallApi2Async(apiId, apiSecret, payload, endpoint, null);
    //    //var response = await CallApi2Async(apiId, apiSecret, null, endpoint, null);
    //    //logger.LogInformation("API Response for {Endpoint}: {Response}", endpoint, response);
    //}

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var apiId = configuration["DynessAppId"] ?? throw new InvalidOperationException();
        var apiSecret = configuration["DynessAppSecret"] ?? throw new InvalidOperationException();

        //var httpClient = new HttpClient();

        var handlers = KiotaClientFactory.CreateDefaultHandlers();
        handlers.Add(new KiotaDebugHandler());

        var httpClient = KiotaClientFactory.Create(handlers);

        var client = DynessClientFactory.Create(httpClient, apiId, apiSecret);

        //var bindResult = await client.V1.Device.BindSn.PostAsync(new RequestDeviceBindRelationDto
        //{
        //    DeviceSn = "R07E7C46681A0E84-BDU-03",
        //    CheckCode = "123456789"
        //}, cancellationToken: stoppingToken);

        var result = await client.V1.Device.RealTime.Data.PostAsync(new RequestOpenApiPointDto
        {
            DeviceSn = "R07E7C46681A0E84-BDU"
            //DeviceSn = "R07E7C46681A0E84"
        },
        cancellationToken: stoppingToken);

        var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });

        File.WriteAllText("battery-2026-03-21-1.json", json);
    }

    public class KiotaDebugHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            //var json = await request.Content.ReadAsStringAsync();

            var response = await base.SendAsync(request, cancellationToken);

            // You can also inspect the response here before it goes back to Kiota
            return response;
        }
    }
}
