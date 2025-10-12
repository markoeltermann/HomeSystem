namespace Web.Client.DTOs;

public class InverterScheduleDto : ScheduleDtoBase
{
    public int Minute { get; set; }
    public int? BatteryLevel { get; set; }
    public int? BatterySellLevel { get; set; }
    public bool? IsGridChargeEnabled { get; set; }
    public bool? IsAdaptiveSellEnabled { get; set; }
}
