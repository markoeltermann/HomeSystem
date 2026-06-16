using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Domain;

public sealed class ConfigurationStore<TSettings>(HomeSystemContext context) : IConfigurationStore<TSettings>
    where TSettings : class, new()
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false,
    };

    public async Task<TSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        var settings = new TSettings();
        var properties = typeof(TSettings)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.CanWrite && property.GetIndexParameters().Length == 0)
            .ToArray();

        var keys = properties.Select(property => property.Name).ToArray();
        var existingPoints = await context.ConfigurationPoints
            .AsNoTracking()
            .Where(point => keys.Contains(point.Type))
            .ToDictionaryAsync(point => point.Type, cancellationToken)
            .ConfigureAwait(false);

        foreach (var property in properties)
        {
            if (!existingPoints.TryGetValue(property.Name, out var existing))
            {
                property.SetValue(settings, GetDefaultValue(property.PropertyType));
                continue;
            }

            property.SetValue(settings, ConvertValue(existing.Value, property.PropertyType));
        }

        return settings;
    }

    public async Task SaveAsync(TSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var properties = typeof(TSettings)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.CanRead && property.CanWrite && property.GetIndexParameters().Length == 0)
            .ToArray();

        var keys = properties.Select(property => property.Name).ToArray();
        var existingPoints = await context.ConfigurationPoints
            .Where(point => keys.Contains(point.Type))
            .ToDictionaryAsync(point => point.Type, cancellationToken)
            .ConfigureAwait(false);

        foreach (var property in properties)
        {
            var key = property.Name;
            var value = property.GetValue(settings);
            var serialized = SerializeValue(value);

            if (existingPoints.TryGetValue(key, out var existing))
            {
                existing.Value = serialized;
            }
            else
            {
                context.ConfigurationPoints.Add(new ConfigurationPoint
                {
                    Type = key,
                    Value = serialized,
                });
            }
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static object? GetDefaultValue(Type type)
    {
        if (type == typeof(string))
            return string.Empty;

        if (type.IsValueType)
            return Activator.CreateInstance(type);

        return null;
    }

    private static string SerializeValue(object? value)
    {
        if (value is null)
            return "null";

        return JsonSerializer.Serialize(value, JsonOptions);
    }

    private static object? ConvertValue(string json, Type targetType)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "null")
        {
            return targetType == typeof(string)
                ? string.Empty
                : GetDefaultValue(targetType);
        }

        return JsonSerializer.Deserialize(json, targetType, JsonOptions);
    }
}
