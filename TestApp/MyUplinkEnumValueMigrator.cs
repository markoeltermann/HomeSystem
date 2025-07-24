using Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System.Text.Json;

namespace TestApp;
public class MyUplinkEnumValueMigrator(HomeSystemContext dbContext) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var jsonFilePath = @"C:\Users\Admin\Desktop\MyUplink output.json";
        var jsonContent = await File.ReadAllTextAsync(jsonFilePath, stoppingToken);
        var points = JsonSerializer.Deserialize<PointDto[]>(jsonContent, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        var devicePoints = await dbContext.DevicePoints
            .Where(dp => dp.Device.Id == 10 && dp.DataTypeId == 4)
            .Include(x => x.EnumMembers)
            .ToArrayAsync(stoppingToken);

        foreach (var point in devicePoints)
        {
            var pointDto = points?.FirstOrDefault(p => p.ParameterId.ToString() == point.Address);
            if (pointDto == null)
                continue;
            //if (point.DataType.Name != "Enum")
            //    continue;
            var enumValues = pointDto.EnumValues;
            if (enumValues == null || enumValues.Length == 0)
                continue;
            var existingEnumMembers = point.EnumMembers;
            foreach (var enumValue in enumValues)
            {
                if (existingEnumMembers.Any(em => em.Value.ToString() == enumValue.Value))
                    continue;
                var newEnumMember = new EnumMember
                {
                    DevicePointId = point.Id,
                    Name = enumValue.Text,
                    Value = int.Parse(enumValue.Value),
                };
                dbContext.EnumMembers.Add(newEnumMember);
            }
            await dbContext.SaveChangesAsync(stoppingToken);
        }

        await this.StopAsync(stoppingToken);
    }

    private class PointDto
    {
        public string ParameterId { get; set; } = null!;
        public decimal Value { get; set; }
        public PointEnumValueDto[] EnumValues { get; set; } = null!;

    }

    private class PointEnumValueDto
    {
        public string Text { get; set; } = null!;
        public string Value { get; set; } = null!;
    }
}
