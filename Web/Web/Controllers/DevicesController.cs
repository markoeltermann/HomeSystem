using Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Web.Client.DTOs;

namespace Web.Controllers;

[Route("api/[controller]")]
[ApiController]
public class DevicesController(HomeSystemContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<DeviceDto[]>> Get()
    {
        return await context.Devices
            .Select(d => new DeviceDto
            {
                Id = d.Id,
                Name = d.Name,
                IsEnabled = d.IsEnabled,
                Points = d.DevicePoints.Select(dp => new DevicePointDto
                {
                    Id = dp.Id,
                    Name = dp.Name,
                    DataTypeName = dp.DataType.Name,
                    Unit = dp.Unit!.Name,
                    Resolution = dp.Resolution,
                }).ToArray()
            })
            .ToArrayAsync();
    }

    //// GET api/<DeviceController>/5
    //[HttpGet("{id}")]
    //public string Get(int id)
    //{
    //    return "value";
    //}

    //// POST api/<DeviceController>
    //[HttpPost]
    //public void Post([FromBody] string value)
    //{
    //}

    //// PUT api/<DeviceController>/5
    //[HttpPut("{id}")]
    //public void Put(int id, [FromBody] string value)
    //{
    //}

    //// DELETE api/<DeviceController>/5
    //[HttpDelete("{id}")]
    //public void Delete(int id)
    //{
    //}
}