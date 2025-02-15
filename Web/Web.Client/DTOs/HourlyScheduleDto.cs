namespace Web.Client.DTOs;

public class HourlyScheduleDto
{
    public int? BatteryLevel { get; set; }
    public bool? IsGridChargeEnabled { get; set; }
    public int Hour { get; set; }
}
