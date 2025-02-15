using CommonLibrary.Extensions;
using CommonLibrary.Helpers;
using Microsoft.AspNetCore.Mvc;
using Web.Client.DTOs;

namespace Web.Controllers;

[Route("api/[controller]")]
[ApiController]
public class DevicePointsController(HttpClient httpClient, IConfiguration configuration) : ControllerBase
{
    [HttpGet("{pointId}/values")]
    public async Task<ActionResult<ValueContainerDto?>> GetNumericValue(int pointId, DateOnly from, DateOnly upTo)
    {
        var url = GetPointValueRequestUrl(configuration, pointId, from, upTo);

        return await httpClient.GetFromJsonAsync<ValueContainerDto>(url);
    }

    public static string? GetPointValueRequestUrl(IConfiguration configuration, int pointId, DateOnly from, DateOnly upTo)
    {
        var url = configuration["PointValueStoreConnectorUrl"];
        if (url.IsNullOrEmpty())
        {
            return null;
        }

        url = UrlHelpers.GetUrl(url, $"points/{pointId}/values",
            [KeyValuePair.Create("from", (string?)from.ToString("yyyy-MM-dd")),
            KeyValuePair.Create("upTo", (string?)upTo.ToString("yyyy-MM-dd"))]);

        return url;
    }

    //[HttpPut("{pointId}/values")]
    //public async Task<ActionResult> SetValues(int pointId, ValueContainerDto values)
    //{
    //    if (values == null || values.Values == null)
    //    {
    //        return BadRequest("Invalid value container");
    //    }

    //    if (values.Values.Length == 0)
    //    {
    //        return Ok();
    //    }

    //    var url = configuration["PointValueStoreConnectorUrl"];
    //    if (url.IsNullOrEmpty())
    //    {
    //        return BadRequest("Point value store connector url has not been set up");
    //    }

    //    url = UrlHelpers.GetUrl(url, $"points/{pointId}/values", null);

    //    var response = await httpClient.PutAsJsonAsync(url, values);
    //    if (!response.IsSuccessStatusCode)
    //    {
    //        return BadRequest("The request to point value store failed with code " + response.StatusCode);
    //    }

    //    return Ok();
    //}
}
