using CommonLibrary.Extensions;
using Domain;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace TestApp;
public class YrNoModelConverter
{
    private const string InputPath = @"C:\Users\Admin\Desktop\yrno model.txt";

    public static async Task Run(HomeSystemContext dbContext)
    {
        var device = await dbContext.Devices.FirstOrDefaultAsync(x => x.Type == "yrno_weather_forecast");
        if (device == null)
        {
            device = new Device
            {
                Type = "yrno_weather_forecast",
                IsEnabled = true,
                Name = "YR.no weather forecast"
            };
            dbContext.Devices.Add(device);
        }

        var units = await dbContext.Units.ToArrayAsync();
        var dataTypes = await dbContext.DataTypes.ToArrayAsync();

        var modelRaw = File.ReadAllText(InputPath);
        var models = modelRaw.Split('}', StringSplitOptions.RemoveEmptyEntries);
        var sb = new StringBuilder();

        foreach (var model in models)
        {
            var className = model.Split('{')[0].Trim();
            className = NormaliseClassName(className);

            var propertyModels = model.Split('{')[1].Split(["\n"], StringSplitOptions.RemoveEmptyEntries)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Split(':', '=')[0].Trim(' ', ','))
                .ToArray();

            sb.AppendLine($"public class {className}");
            sb.AppendLine("{");

            foreach (var propertyModel in propertyModels)
            {
                var propertyNameRaw = propertyModel.Split('(')[0].Trim();
                var propertyName = NormaliseClassName(propertyNameRaw);
                var propertyTypeRaw = propertyModel.Split('(')[1].Trim(' ', ')', ',', '\r').Replace(", optional", "");
                var isArray = propertyTypeRaw.StartsWith("Array");
                if (isArray)
                    propertyTypeRaw = propertyTypeRaw.Split('[')[1].Trim(']');
                var propertyType = NormaliseClassName(propertyTypeRaw);
                sb.AppendLine($"\t[JsonPropertyName(\"{propertyNameRaw}\")]");
                sb.AppendLine($"\tpublic {propertyType}{(isArray ? "[]" : null)}? {propertyName} {{ get; set; }}");

                if (className == "ForecastTimeInstant")
                {
                    if (propertyNameRaw.Contains("temperature") || propertyNameRaw.Contains("wind")
                        || propertyNameRaw.Contains("cloud") || propertyNameRaw.Contains("humidity") || propertyNameRaw.Contains("fog"))
                    {
                        Console.WriteLine($"\"{propertyNameRaw}\" => point.{propertyName},");

                        var devicePoint = new DevicePoint
                        {
                            Device = device,
                            Address = propertyNameRaw,
                            DataType = dataTypes.First(x => x.Name == "Float"),
                            Name = propertyNameRaw.Replace("_", " ").Capitalise(),
                        };
                        if (propertyNameRaw.Contains("temperature"))
                            devicePoint.Unit = units.First(x => x.Name == "°C");
                        else if (propertyNameRaw.Contains("cloud") || propertyNameRaw.Contains("humidity") || propertyNameRaw.Contains("fog"))
                            devicePoint.Unit = units.First(x => x.Name == "%");
                        else if (propertyNameRaw.Contains("direction"))
                            devicePoint.Unit = units.First(x => x.Name == "deg");
                        else if (propertyNameRaw.Contains("speed"))
                            devicePoint.Unit = units.First(x => x.Name == "m/s");
                        else
                            throw new Exception();
                        device.DevicePoints.Add(devicePoint);
                    }
                }
            }

            sb.AppendLine("}");
            sb.AppendLine();
        }

        var outputFile = Path.Combine(Path.GetDirectoryName(InputPath)!, "ApiModel.cs");
        using var sw = new StreamWriter(outputFile, false);
        sw.Write(sb.ToString());

        //await dbContext.SaveChangesAsync();
    }

    private static string NormaliseClassName(string className)
    {
        if (className == "number")
            return "decimal";

        if (className == "string")
            return "string";

        return string.Join("", className.Split('_').Select(x => x.Capitalise())).Replace(" ", "");
    }
}
