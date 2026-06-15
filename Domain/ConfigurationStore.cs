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
        var properties = typeof(TSettings).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            if (!property.CanWrite || property.GetIndexParameters().Length != 0)
                continue;

            var key = property.Name;
            var existing = await context.ConfigurationPoints
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Type == key, cancellationToken)
                .ConfigureAwait(false);

            if (existing == null)
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

        var properties = typeof(TSettings).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            if (!property.CanRead || !property.CanWrite || property.GetIndexParameters().Length != 0)
                continue;

            var key = property.Name;
            var value = property.GetValue(settings);
            var serialized = SerializeValue(value);

            var existing = await context.ConfigurationPoints
                .FirstOrDefaultAsync(x => x.Type == key, cancellationToken)
                .ConfigureAwait(false);

            if (existing == null)
            {
                context.ConfigurationPoints.Add(new ConfigurationPoint
                {
                    Type = key,
                    Value = serialized,
                });
            }
            else
            {
                existing.Value = serialized;
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

        if (targetType == typeof(string))
            return JsonSerializer.Deserialize<string>(json, JsonOptions);

        if (targetType == typeof(bool))
            return JsonSerializer.Deserialize<bool>(json, JsonOptions);

        if (targetType == typeof(bool?))
            return JsonSerializer.Deserialize<bool?>(json, JsonOptions);

        if (targetType == typeof(int))
            return JsonSerializer.Deserialize<int>(json, JsonOptions);

        if (targetType == typeof(int?))
            return JsonSerializer.Deserialize<int?>(json, JsonOptions);

        if (targetType == typeof(decimal))
            return JsonSerializer.Deserialize<decimal>(json, JsonOptions);

        if (targetType == typeof(decimal?))
            return JsonSerializer.Deserialize<decimal?>(json, JsonOptions);

        return JsonSerializer.Deserialize(json, targetType, JsonOptions);
    }
}
