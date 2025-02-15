using Microsoft.AspNetCore.WebUtilities;

namespace CommonLibrary.Helpers;
public static class UrlHelpers
{
    public static string GetUrl(string baseUrl, string path, IEnumerable<KeyValuePair<string, string?>>? queryParams)
    {
        var url = baseUrl;
        if (!url.EndsWith('/') && !path.StartsWith('/'))
            url += '/';
        url += path;

        if (queryParams != null)
            url = QueryHelpers.AddQueryString(url, queryParams);

        return url;
    }
}
