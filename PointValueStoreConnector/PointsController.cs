using Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SharedServices;
using System.Globalization;

namespace PointValueStoreConnector;

[Route("[controller]")]
[ApiController]
public class PointsController(HomeSystemContext context, PointValueStore pointValueStore) : ControllerBase
{
    private readonly static CultureInfo InvariantCulture = CultureInfo.InvariantCulture;

    [HttpGet("{pointId}/values")]
    public async Task<ActionResult<ResponseValueContainerDto>> GetNumericValue(int pointId, DateOnly from, DateOnly upTo)
    {
        var devicePoint = await context.DevicePoints
            .Include(x => x.DataType)
            .Include(x => x.Unit)
            .Include(x => x.EnumMembers)
            .FirstOrDefaultAsync(x => x.Id == pointId);
        if (devicePoint == null)
            return NotFound();

        var enumMembers = devicePoint.DataType.Name == "Enum"
            ? devicePoint.EnumMembers.OrderBy(x => x.Value).ToArray()
            : null;

        return new ResponseValueContainerDto
        {
            Values = pointValueStore.ReadNumericValues(devicePoint.DeviceId, devicePoint.Id, from, upTo).Select(x =>
            {
                var value = x.Item2;
                string? stringValue = null;
                if (devicePoint.DataType.Name == "Enum")
                {
                    (value, stringValue) = GetEnumValue(enumMembers!, value);
                }
                return new NumericValueDto
                {
                    Timestamp = x.Item1,
                    Value = value,
                    StringValue = stringValue,
                };
            }).ToArray(),
            Unit = devicePoint.DataType.Name is "Boolean" ? "bool" : devicePoint.Unit?.Name ?? "unk"
        };
    }

    private static (double?, string?) GetEnumValue(EnumMember[] enumMembers, double? value)
    {
        for (int i = 0; i < enumMembers.Length; i++)
        {
            var enumMember = enumMembers[i];
            if (enumMember.Value == value)
            {
                return (i, enumMember.Name);
            }
        }

        return (null, null);
    }

    [HttpPut("{pointId}/values")]
    public async Task<ActionResult> SetValues(int pointId, ValueContainerDto values)
    {
        if (values == null || values.Values == null)
        {
            return BadRequest("Invalid value container");
        }

        if (values.Values.Length == 0)
        {
            return Ok();
        }

        var devicePoint = await context.DevicePoints
            .Include(x => x.DataType)
            .Include(x => x.Unit)
            .FirstOrDefaultAsync(x => x.Id == pointId);
        if (devicePoint == null)
            return NotFound();
        if (devicePoint.DataType.Name is not "Float" and not "Integer" and not "Boolean")
            return BadRequest("This point is not numeric");

        var processedValues = values.Values.Select(x =>
        {
            string? value = null;
            if (x.StringValue != null)
            {
                value = x.StringValue;
            }
            else if (x.Value != null)
            {
                value = devicePoint.DataType.Name switch
                {
                    "Float" => x.Value.Value.ToString(InvariantCulture),
                    "Integer" => ((int)x.Value.Value).ToString(InvariantCulture),
                    "Boolean" => (x.Value.Value > 0.0).ToString(),
                    _ => throw new Exception(),
                };
            }
            return (x.Timestamp.ToUniversalTime(), value);
        }).ToArray();

        pointValueStore.StoreValuesWithReplace(devicePoint.DeviceId, devicePoint.Id, processedValues);

        return Ok();
    }
}
