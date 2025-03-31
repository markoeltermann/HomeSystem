namespace Web.Client.DTOs;

public class InverterHourlyScheduleDto : HourlyScheduleDtoBase
{
    public int? BatteryLevel { get; set; }
    public bool? IsGridChargeEnabled { get; set; }
    public bool? IsAdaptiveSellEnabled { get; set; }
}
