using Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SharedServices;
using Web.Client.DTOs;

namespace Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DevicePointsController(HomeSystemContext context, PointValueStore pointValueStore) : ControllerBase
    {
        [HttpGet("{pointId}/values")]
        public async Task<ActionResult<NumericValueDto[]>> GetNumericValue(int pointId, DateOnly from, DateOnly upTo)
        {
            var devicePoint = await context.DevicePoints.Include(x => x.DataType).FirstOrDefaultAsync(x => x.Id == pointId);
            if (devicePoint == null)
                return NotFound();
            if (devicePoint.DataType.Name is not "Float" and not "Integer")
                return BadRequest("This point is not numeric");

            return pointValueStore.ReadNumericValues(devicePoint.DeviceId, devicePoint.Id, from, upTo).Select(x => new NumericValueDto
            {
                Timestamp = x.Item1,
                Value = x.Item2
            }).ToArray();
        }
    }
}
