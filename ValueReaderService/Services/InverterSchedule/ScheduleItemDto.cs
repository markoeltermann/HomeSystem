namespace ValueReaderService.Services.InverterSchedule;

public class ScheduleItemDto
{
    public TimeOnly Time { get; set; }

    public bool IsGridChargeEnabled { get; set; }

    public int MaxBatteryPower { get; set; }

    public int BatteryChargeLevel { get; set; }
}
