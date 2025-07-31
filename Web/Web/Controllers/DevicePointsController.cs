using Microsoft.AspNetCore.Mvc;
using Web.Client.DTOs;
using Web.Helpers;

namespace Web.Controllers;

[Route("api/[controller]")]
[ApiController]
public class DevicePointsController(HttpClient httpClient, IConfiguration configuration) : ControllerBase
{
    [HttpGet("{pointId}/values")]
    public async Task<ActionResult<ValueContainerDto?>> GetNumericValue(int pointId, DateOnly from, DateOnly upTo)
    {
        var url = PointValueStoreHelpers.GetPointValueRequestUrl(pointId, from, upTo, configuration["PointValueStoreConnectorUrl"], null);

        return await httpClient.GetFromJsonAsync<ValueContainerDto>(url);
    }
}
