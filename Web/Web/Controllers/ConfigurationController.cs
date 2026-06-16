using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using Domain;
using Microsoft.AspNetCore.Mvc;
using Web.Client.DTOs;

namespace Web.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ConfigurationController(IConfigurationStore<ConfigPointModel> configurationStore) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ConfigSettingDto>>> Get()
    {
        var settings = await configurationStore.LoadAsync();
        var rows = GetRows(settings);

        return Ok(rows);
    }

    [HttpPut]
    public async Task<ActionResult> Put([FromBody] IReadOnlyList<ConfigSettingDto> values)
    {
        if (values == null || values.Count == 0)
        {
            return BadRequest("At least one setting is required.");
        }

        var settings = await configurationStore.LoadAsync();

        foreach (var item in values)
        {
            if (string.IsNullOrWhiteSpace(item.Key))
            {
                return BadRequest("Each setting must have a key.");
            }

            var property = typeof(ConfigPointModel).GetProperty(item.Key, BindingFlags.Public | BindingFlags.Instance);
            if (property == null)
            {
                return BadRequest($"Unknown setting '{item.Key}'.");
            }

            if (!TryConvertValue(item.Value, property.PropertyType, out var converted))
            {
                return BadRequest($"Invalid value for '{item.Key}'.");
            }

            property.SetValue(settings, converted);
        }

        await configurationStore.SaveAsync(settings);
        return Ok();
    }

    private static ConfigSettingDto[] GetRows(ConfigPointModel settings)
    {
        var properties = typeof(ConfigPointModel).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        return properties
            .Where(x => x.CanRead)
            .Select(property => new ConfigSettingDto
            {
                Key = property.Name,
                Name = property.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? property.Name,
                Value = Convert.ToString(property.GetValue(settings), CultureInfo.InvariantCulture) ?? string.Empty,
                Kind = GetKind(property.PropertyType),
            })
            .ToArray();
    }

    private static string GetKind(Type type)
    {
        if (type == typeof(bool) || type == typeof(bool?)) return "bool";
        if (type == typeof(int) || type == typeof(int?)) return "int";
        if (type == typeof(decimal) || type == typeof(decimal?)) return "decimal";
        return "string";
    }

    private static bool TryConvertValue(string rawValue, Type targetType, out object? converted)
    {
        converted = null;

        if (targetType == typeof(bool) || targetType == typeof(bool?))
        {
            if (bool.TryParse(rawValue, out var boolValue))
            {
                converted = boolValue;
                return true;
            }

            return false;
        }

        if (targetType == typeof(int) || targetType == typeof(int?))
        {
            if (int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
            {
                converted = intValue;
                return true;
            }

            return false;
        }

        if (targetType == typeof(decimal) || targetType == typeof(decimal?))
        {
            if (decimal.TryParse(rawValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var decimalValue))
            {
                converted = decimalValue;
                return true;
            }

            return false;
        }

        converted = rawValue;
        return true;
    }
}
