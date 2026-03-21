using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;
using System.Security.Cryptography;
using System.Text;

namespace DynessConnector;

internal class DynessApiAuthenticationProvider(string appId, string appSecret) : IAuthenticationProvider
{
    public async Task AuthenticateRequestAsync(RequestInformation request, Dictionary<string, object>? additionalAuthenticationContext = null, CancellationToken cancellationToken = default)
    {
        var body = await ReadContentAsync(request.Content);
        var contentMd5 = GetDigest(body);
        var date = GetGMTTime();

        var path = request.UrlTemplate?.Replace("{+baseurl}", "");

        // Constructing the string to sign
        var param = "POST" + "\n" +
              contentMd5 + "\n" +
              "application/json" + "\n" +
              date + "\n" +
              path;

        var sign = HmacSHA1Encrypt(param, appSecret);
        // Setting Headers
        request.Headers.Add("Authorization", "API " + appId + ":" + sign);
        request.Headers.Add("Date", date);
    }

    private static async Task<string> ReadContentAsync(Stream contentStream)
    {
        if (contentStream == null) return string.Empty;

        using var reader = new StreamReader(contentStream, leaveOpen: true);
        var content = await reader.ReadToEndAsync();

        // Reset the position so the HttpClient can read it again
        if (contentStream.CanSeek)
        {
            contentStream.Position = 0;
        }

        return content;
    }

    /// <summary>
    /// Generates HmacSHA1 signature and encodes to Base64
    /// </summary>
    private static string HmacSHA1Encrypt(string encryptText, string keySecret)
    {
        byte[] keyBytes = Encoding.UTF8.GetBytes(keySecret);
        using var hmac = new HMACSHA1(keyBytes);
        byte[] textBytes = Encoding.UTF8.GetBytes(encryptText);
        byte[] hashBytes = hmac.ComputeHash(textBytes);
        return Convert.ToBase64String(hashBytes);
    }

    /// <summary>
    /// Gets current time in GMT format: EEE, d MMM yyyy HH:mm:ss 'GMT'
    /// </summary>
    private static string GetGMTTime()
    {
        return DateTime.UtcNow.ToString("ddd, dd MMM yyyy HH:mm:ss 'GMT'",
            System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Generates MD5 hash of the body and encodes to Base64
    /// </summary>
    private static string GetDigest(string test)
    {
        byte[] inputBytes = Encoding.UTF8.GetBytes(test);
        byte[] hashBytes = MD5.HashData(inputBytes);
        return Convert.ToBase64String(hashBytes);
    }
}
